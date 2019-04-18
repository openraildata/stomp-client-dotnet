using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;

namespace OpenRailMessaging
{
    /*
     * This sample illustrates how to use .Net generally and C# specifically to 
     * receive and process messages from the Darwin Push Port.  Originally written by Chris Bailiss.
     * This sample makes use of the Apache NMS Messaging API - http://activemq.apache.org/nms/
     * This sample was built against v1.7.0 of the API.  
     * The Apache.NMS and Apache.NMS.ActiveMQ assemblies can be downloaded from http://activemq.apache.org/nms/download.html
     */

    public class OpenRailDarwinPushPortReceiver
    {
        private IConnectionFactory moConnectionFactory = null;
        private IConnection moConnection = null;
        private ISession moSession = null;
        private ITopic moTopic = null;
        private IMessageConsumer moConsumer = null;

        private string msConnectUrl = null;
        private string msUser = null;
        private string msPassword = null;
        private string msTopic = null;
        private int miAttemptToConnectForSeconds = 120;

        private ConcurrentQueue<OpenRailMessage> moMessageQueue = null;
        private ConcurrentQueue<OpenRailException> moErrorQueue = null;

        private CancellationTokenSource moCTS = null;
        private Task mtManagement = null;

        private long miSpinSpreadUntilUtc = (new DateTime(2000, 1, 1)).Ticks;
        private long miIsConnected = 0;
        private long miLastMessageReceivedAtUtc = (new DateTime(2000, 1, 1)).Ticks;
        private long miMessageReadCount = 0;
        private long miLastConnectionExceptionAtUtc = (new DateTime(2000, 1, 1)).Ticks;

        public OpenRailDarwinPushPortReceiver(string sConnectUrl, string sUser, string sPassword, string sTopic,
            ConcurrentQueue<OpenRailMessage> oMessageQueue, ConcurrentQueue<OpenRailException> oErrorQueue, 
            int iAttemptToConnectForSeconds = 300)
        {
            msConnectUrl = sConnectUrl;
            msUser = sUser;
            msPassword = sPassword;
            msTopic = sTopic;
            moMessageQueue = oMessageQueue;
            moErrorQueue = oErrorQueue;
            miAttemptToConnectForSeconds = iAttemptToConnectForSeconds;
        }

        public void Start()
        {
            lock (this)
            {
                if (IsRunning) throw new ApplicationException("OpenRailDarwinPushPortReceiver already running!");
                moCTS = new CancellationTokenSource();
                mtManagement = Task.Run((Func<Task>)Run);
                mtManagement.ConfigureAwait(false);
            }
        }

        public bool IsRunning
        {
            get { return mtManagement == null ? false : !(mtManagement.IsCanceled || mtManagement.IsCompleted || mtManagement.IsFaulted); }
        }

        public bool IsConnected { get { return Interlocked.Read(ref miIsConnected) > 0; } }
        public long MessageCount { get { return Interlocked.Read(ref miMessageReadCount); } }
        public Exception FatalException { get { return mtManagement.IsFaulted ? mtManagement.Exception : null; } }

        public void RequestStop()
        {
            if (moCTS != null) moCTS.Cancel();
        }

        private DateTime SpinSpreadUntilUtc
        {
            get { return new DateTime(Interlocked.Read(ref miSpinSpreadUntilUtc)); }
            set { Interlocked.Exchange(ref miSpinSpreadUntilUtc, value.Ticks); }
        }

        public DateTime LastMessageReceivedAtUtc
        {
            get { return new DateTime(Interlocked.Read(ref miLastMessageReceivedAtUtc)); }
            private set { Interlocked.Exchange(ref miLastMessageReceivedAtUtc, value.Ticks); }
        }

        public DateTime LastConnectionExceptionAtUtc
        {
            get { return new DateTime(Interlocked.Read(ref miLastConnectionExceptionAtUtc)); }
            private set { Interlocked.Exchange(ref miLastConnectionExceptionAtUtc, value.Ticks); }
        }

        private async Task Run()
        {
            CancellationToken oCT = moCTS.Token;
            try
            {
                Interlocked.Exchange(ref miMessageReadCount, 0);
                await Connect();

                bool bRefreshRequired = false;
                while (!oCT.IsCancellationRequested)
                {
                    await Task.Delay(50);
                    int iMessageGapToleranceSeconds = DateTime.UtcNow < LastConnectionExceptionAtUtc.AddSeconds(60) ? 30 : 120;
                    bRefreshRequired = (LastMessageReceivedAtUtc.AddSeconds(iMessageGapToleranceSeconds) < DateTime.UtcNow);
                    if (bRefreshRequired)
                    {
                        Disconnect();
                        await Connect();
                        LastMessageReceivedAtUtc = DateTime.UtcNow;
                        bRefreshRequired = false;
                    }
                }
            }
            catch (Exception oException)
            {
                if (!oCT.IsCancellationRequested)
                {
                    moErrorQueue.Enqueue(new OpenRailFatalException("OpenRailDarwinPushPortReceiver FAILED due to " +
                        OpenRailException.GetShortErrorInfo(oException), oException));

                    // rethrow the exception:
                    // this sets the Exception property of the Task object  
                    // i.e. makes this exception visible in the FatalException property of this class
                    throw oException;
                }
            }
            finally
            {
                Disconnect();
            }
        }

        private async Task Connect()
        {
            DateTime dtAttemptToConnectUntilUtc = DateTime.UtcNow.AddSeconds(miAttemptToConnectForSeconds);
            DateTime dtNextConnectAtUtc = new DateTime(2000, 1, 1);
            int iDelayDurationMilliSeconds = 250;
            Exception oLastException = null;
            CancellationToken oCT = moCTS.Token;

            while (DateTime.UtcNow < dtAttemptToConnectUntilUtc)
            {
                if (dtNextConnectAtUtc < DateTime.UtcNow)
                {
                    if (TryConnect()) return;
                    // connect retry time doubles between each attempt (up to 1 minute) otherwise Open Data Service is overwhelmed during recovery
                    iDelayDurationMilliSeconds = Math.Min(iDelayDurationMilliSeconds * 2, 60000);
                    dtNextConnectAtUtc = DateTime.UtcNow.AddMilliseconds(iDelayDurationMilliSeconds);
                }
                await Task.Delay(500);
                if (oCT.IsCancellationRequested)
                    throw new OperationCanceledException("The connection attempt was cancelled due to OpenRailDarwinPushPortReceiver.RequestStop() being called.");
            }

            if (oLastException == null)
                throw new OpenRailConnectTimeoutException("Timeout trying to connect to the message feed.");
            else
                throw new OpenRailConnectTimeoutException("Timeout trying to connect to the message feed.  The last connection error was: " +
                    oLastException.GetType().FullName + ": " + oLastException.Message, oLastException);
        }

        private bool TryConnect()
        {
            try
            {
                moConnectionFactory = new NMSConnectionFactory(new Uri(msConnectUrl));
                moConnection = moConnectionFactory.CreateConnection(msUser, msPassword);
                moConnection.ClientId = msUser;
                moConnection.ExceptionListener += new ExceptionListener(OnConnectionException);
                moSession = moConnection.CreateSession();
                moTopic = moSession.GetTopic(msTopic);
                moConsumer = moSession.CreateConsumer(moTopic);
                moConsumer.Listener += new MessageListener(OnMessageReceived);

                LastMessageReceivedAtUtc = DateTime.UtcNow;
                SpinSpreadUntilUtc = DateTime.UtcNow.AddSeconds(30);

                moConnection.Start();
                Interlocked.Exchange(ref miIsConnected, 1);
                return true;
            }
            catch (Exception oException)
            {
                moErrorQueue.Enqueue(new OpenRailConnectException("Connection attempt failed: " +
                    OpenRailException.GetShortErrorInfo(oException), oException));
                Disconnect();
                return false;
            }
        }

        private void OnConnectionException(Exception exception)
        {
            try
            {
                OpenRailConnectionException oConnectionException = new OpenRailConnectionException(
                    OpenRailException.GetShortErrorInfo(exception), exception);
                moErrorQueue.Enqueue(oConnectionException);
                LastConnectionExceptionAtUtc = DateTime.UtcNow;
            }
            catch { }
        }

        private void OnMessageReceived(IMessage message)
        {
            try
            {
                OpenRailMessage oMessage = null;

                // when the Apache code starts receiving messages, a number of worker threads are fired up (inside the Apache assembly)
                // these threads are all started up at close to the exact same time
                // this can lead to contention within the apache code, i.e.  blocking and slow throughput until the threads spread out
                // so, for the first half minute, inject some spin waits in the different worker threads to spread their activities out
                if (DateTime.UtcNow < SpinSpreadUntilUtc)
                {
                    long iSeed = Thread.CurrentThread.ManagedThreadId + (DateTime.Now.Ticks % Int32.MaxValue);
                    Thread.SpinWait(new Random((int)(iSeed % Int32.MaxValue)).Next(1, 1000000));
                }

                // text message
                ITextMessage msgText = message as ITextMessage;
                if (msgText != null) oMessage = new OpenRailTextMessage(msgText.NMSTimestamp, msgText.Text);

                // bytes message
                IBytesMessage msgBytes = message as IBytesMessage;
                if (msgBytes != null) oMessage = new OpenRailBytesMessage(message.NMSTimestamp, msgBytes.Content);

                // everything else
                if (oMessage == null) oMessage = new OpenRailUnsupportedMessage(message.NMSTimestamp, message.GetType().FullName);

                Interlocked.Increment(ref miMessageReadCount);
                LastMessageReceivedAtUtc = DateTime.UtcNow;
                moMessageQueue.Enqueue(oMessage);
            }
            catch (Exception oException)
            {
                moErrorQueue.Enqueue(new OpenRailMessageException("Message receive failed: " +
                    OpenRailException.GetShortErrorInfo(oException), oException));
            }
        }

        private void Disconnect()
        {
            try
            {
                if (moConnection != null)
                {
                    try { if (moConnection != null) moConnection.Stop(); }
                    catch { }
                    try { if (moConsumer != null) moConsumer.Close(); }
                    catch { }
                    try { if (moSession != null) moSession.Close(); }
                    catch { }
                    try { if (moConnection != null) moConnection.Close(); }
                    catch { }
                }
            }
            finally
            {
                moConnection = null;
                moConnectionFactory = null;
                moSession = null;
                moTopic = null;
                moConsumer = null;
                Interlocked.Exchange(ref miIsConnected, 0);
            }
        }
    }
}

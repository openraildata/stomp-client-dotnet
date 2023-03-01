using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OpenRailMessaging;

namespace ExampleNetworkRailOpenDataClient
{
    /*
     * This sample illustrates how to use .Net generally and C# specifically to 
     * receive and process messages from the Network Rail Open Data Platform.  Originally written by Chris Bailiss.
     * This sample makes use of the Apache NMS Messaging API - http://activemq.apache.org/nms/
     * This sample was built against v1.5.1 of the API.  
     * The Apache.NMS and Apache.NMS.Stomp assemblies can be downloaded from http://activemq.apache.org/nms/download.html
     */

    class Program
    {
        static void Main(string[] args)
        {
            // CONNECTION SETTINGS:  In your code, move these into some form of configuration file / table
            // *** change the following lines to your user, password and feeds of interest - get this from the Network Rail Data Feeds portal *** 
            string sConnectUrl = "stomp:tcp://datafeeds.networkrail.co.uk:61618";
            string sUser = "InsertYourUserIdHere";
            string sPassword = "InsertYourPasswordHere";
            string sTopic1 = "TRAIN_MVT_ALL_TOC";
            string sTopic2 = "VSTP_ALL";
            bool bUseDurableSubscription = true;

            if ((sUser == "InsertYourUserIdHere") || (sPassword == "InsertYourPasswordHere"))
            {
                Console.WriteLine("NETWORK RAIL OPEN DATA RECEIVER SAMPLE: ");
                Console.WriteLine();
                Console.WriteLine("ERROR:  Please update the source code (in the Program.cs file) to use your Network Rail Open Data credentials!");
                Console.ReadLine();
                return;
            }

            // create the shared queues (into which the receiver will enqueue messages/errors)
            ConcurrentQueue<OpenRailMessage> oMessageQueue1 = new ConcurrentQueue<OpenRailMessage>();
            ConcurrentQueue<OpenRailMessage> oMessageQueue2 = new ConcurrentQueue<OpenRailMessage>();
            ConcurrentQueue<OpenRailException> oErrorQueue = new ConcurrentQueue<OpenRailException>();

            // create the receiver
            OpenRailNRODReceiver oNRODReceiver = new OpenRailNRODReceiver(
                sConnectUrl, sUser, sPassword, sTopic1, sTopic2, oMessageQueue1, oMessageQueue2, oErrorQueue, bUseDurableSubscription, 100);

            // Start the receiver
            oNRODReceiver.Start();

            // Running: process the output from the receiver (in the queues) and display progress
            DateTime dtRunUntilUtc = DateTime.UtcNow.AddSeconds(120);
            DateTime dtNextUiUpdateTime = DateTime.UtcNow;
            int iTextMessageCount1 = 0;
            int iBytesMessageCount1 = 0;
            int iUnsupportedMessageCount1 = 0;
            string msLastTextMessage1 = null;
            int iTextMessageCount2 = 0;
            int iBytesMessageCount2 = 0;
            int iUnsupportedMessageCount2 = 0;
            string msLastTextMessage2 = null;
            int iErrorCount = 0;
            string msLastErrorInfo = null;
            while (DateTime.UtcNow < dtRunUntilUtc)
            {
                // attempt to dequeue and process any errors that occurred in the receiver
                while ((oErrorQueue.Count > 0) && (DateTime.UtcNow < dtNextUiUpdateTime))
                {
                    OpenRailException oOpenRailException = null;
                    if (oErrorQueue.TryDequeue(out oOpenRailException))
                    {
                        // the code here simply counts the errors, and captures the details of the last 
                        // error - your code may log details of errors to a database or log file
                        iErrorCount++;
                        msLastErrorInfo = OpenRailException.GetShortErrorInfo(oOpenRailException);
                    }
                }

                // attempt to dequeue and process some messages
                while ((oMessageQueue1.Count > 0) && (DateTime.UtcNow < dtNextUiUpdateTime))
                {
                    OpenRailMessage oMessage = null;
                    if (oMessageQueue1.TryDequeue(out oMessage))
                    {
                        // All Network Rail Open Data Messages should be text
                        OpenRailTextMessage oTextMessage = oMessage as OpenRailTextMessage;
                        if (oTextMessage != null)
                        {
                            iTextMessageCount1++;
                            msLastTextMessage1 = oTextMessage.Text;
                        }

                        // Network Rail Open Data Messages should not be bytes messages (code is here just in case)
                        OpenRailBytesMessage oBytesMessage = oMessage as OpenRailBytesMessage;
                        if (oBytesMessage != null) iBytesMessageCount1++;

                        // All Network Rail Open Data Messages should be text (code is here just in case)
                        OpenRailUnsupportedMessage oUnsupportedMessage = oMessage as OpenRailUnsupportedMessage;
                        if (oUnsupportedMessage != null) iUnsupportedMessageCount1++;
                    }
                }
                while ((oMessageQueue2.Count > 0) && (DateTime.UtcNow < dtNextUiUpdateTime))
                {
                    OpenRailMessage oMessage = null;
                    if (oMessageQueue2.TryDequeue(out oMessage))
                    {
                        // All Network Rail Open Data Messages should be text
                        OpenRailTextMessage oTextMessage = oMessage as OpenRailTextMessage;
                        if (oTextMessage != null)
                        {
                            iTextMessageCount2++;
                            msLastTextMessage2 = oTextMessage.Text;
                        }

                        // Network Rail Open Data Messages should not be bytes messages (code is here just in case)
                        OpenRailBytesMessage oBytesMessage = oMessage as OpenRailBytesMessage;
                        if (oBytesMessage != null) iBytesMessageCount2++;

                        // All Network Rail Open Data Messages should be text (code is here just in case)
                        OpenRailUnsupportedMessage oUnsupportedMessage = oMessage as OpenRailUnsupportedMessage;
                        if (oUnsupportedMessage != null) iUnsupportedMessageCount2++;
                    }
                }

                if (dtNextUiUpdateTime < DateTime.UtcNow)
                {
                    Console.Clear();
                    Console.WriteLine("NETWORK RAIL OPEN DATA RECEIVER SAMPLE: ");
                    Console.WriteLine();
                    Console.WriteLine("Remaining Run Time = " + dtRunUntilUtc.Subtract(DateTime.UtcNow).TotalSeconds.ToString("###0.0") + " seconds");
                    Console.WriteLine();
                    Console.WriteLine("Receiver Status:");
                    Console.WriteLine("  Running = " + oNRODReceiver.IsRunning.ToString() + ", Connected To Data Feed = " + oNRODReceiver.IsConnected.ToString());
                    Console.WriteLine("  Size of local In-Memory Queue 1 (" + sTopic1 + ") = " + oMessageQueue1.Count.ToString()); // i.e. messages received from the feed but not yet processed locally
                    Console.WriteLine("  Size of local In-Memory Queue 2 (" + sTopic2 + ") = " + oMessageQueue2.Count.ToString()); // i.e. messages received from the feed but not yet processed locally
                    Console.WriteLine("  Last Message Received At = " + oNRODReceiver.LastMessageReceivedAtUtc.ToLocalTime().ToString("HH:mm:ss.fff ddd dd MMM yyyy"));
                    Console.WriteLine("  Msg Counts:  (1: {0}) = {1}, (2: {2}) = {3}", sTopic1, oNRODReceiver.MessageCount1, sTopic2, oNRODReceiver.MessageCount2);
                    Console.WriteLine();
                    Console.WriteLine("Processing Status 1 (" + sTopic1 + "):");
                    Console.WriteLine("  Msg Counts: Text = {0}, Bytes = {1}, Unsupported = {2}", iTextMessageCount1, iBytesMessageCount1, iUnsupportedMessageCount1);
                    Console.WriteLine("  Last JSON = " + (msLastTextMessage1 == null ? "" : (msLastTextMessage1.Length > 40 ? msLastTextMessage1.Substring(0, 40) + "..." : msLastTextMessage1)));
                    Console.WriteLine();
                    Console.WriteLine("Processing Status 2 (" + sTopic2 + "):");
                    Console.WriteLine("  Msg Counts: Text = {0}, Bytes = {1}, Unsupported = {2}", iTextMessageCount2, iBytesMessageCount2, iUnsupportedMessageCount2);
                    Console.WriteLine("  Last JSON = " + (msLastTextMessage2 == null ? "" : (msLastTextMessage2.Length > 40 ? msLastTextMessage2.Substring(0, 40) + "..." : msLastTextMessage2)));
                    Console.WriteLine();
                    Console.WriteLine("Errors:  Total Errors = " + iErrorCount.ToString());
                    Console.WriteLine("  Last Error = " + (msLastErrorInfo == null ? "" : msLastErrorInfo));
                    Console.WriteLine();
                    dtNextUiUpdateTime = DateTime.UtcNow.AddMilliseconds(500);
                }

                if ((oMessageQueue1.Count < 10) && (oMessageQueue2.Count < 10)) Thread.Sleep(50);
            }

            Console.WriteLine("Stopping Receiver...");

            oNRODReceiver.RequestStop();

            while (oNRODReceiver.IsRunning)
            {
                Thread.Sleep(50);
            }

            Console.WriteLine("Receiver stopped.");
            Console.WriteLine("Finished.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}

Darwin and Network Rail Open Data .NET Examples
===============================================

Version History
---------------

| Version | Date       | Author     | Changes |
|---------|------------|------------|---------|    
| 1.00    | 2015-04-08 | CBailiss   | Initial Version | 
| 1.01    | 2015-04-09 | CBailiss   | Messaging clients hooked up to Apache NMS Connection Exception Listener. | 
| 1.02    | 2019-04-18 | CBailiss   | Added two Darwin Push Port v16 examples:  (1) MinimalDarwinPushPortClientV16 and (2) ExampleDarwinPushPortClientV16 | 
| 1.03    | 2020-02-05 | PeterHicks | Minor code and README cleanup | 
| 1.04    | 2023-01-20 | Legolash2o | Updated Apached Libraries to ActiveMQ and v1.8.0. Updated default connection to new publicfeeds. Updated .NET version to 4.8 |
| 1.05    | 2023-03-01 | CBailiss   | Reverted previous OpenWire changes (inline with repo name).  Added warning that this repo should be considered historical. |

Warning
-------

The code in this repo should not be used as the basis of ongoing/future projects.  Development of the Apache NMS Stomp assemblies appears to have been abandoned many years ago and they do not work well with the current version of the Open Data Feeds.

Please see the updated code in the openwire-client-dotnet repo for an updated version of this code that uses OpenWire instead of Stomp.

Overview
--------

This solution contains three Visual Studio Projects

 * `ExampleDarwinPushPortClientV16` demonstrates how to receive information from the RDG Darwin System (V16).
 * `MinimalDarwinPushPortClientV16` demonstrates the bare bones of receiving messages from the RDG Darwin System (V16).
 * `ExampleNetworkRailOpenDataClient` demonstrates how to receive information from the Network Rail Open Data Platform.

The `MinimalDarwinPushPortClientV16` example was added because the other example clients are more involved.  They have more features (described below) but are not so easy to understand.

For a comparison between the Darwin Feed and the Network Rail Feeds, see https://wiki.openraildata.com/index.php/Rail_Data_FAQ

These projects are intended to simplify the path to using the Open Rail Data Feeds and demonstrate good practice. The code has been structured to support easy re-use in your projects.  The `OpenRail*.cs` files can be easily copied into other solutions.

Both projects are simple Console applications.  Each runs for 120 seconds and then exits. They receive messages from the relevant feed(s) and display a running status summary on-screen. Before running the code, some basic user/connection information needs entering into the source code and the projects then need to be rebuilt.  This is described in more detail below.  Such information should of course not be hard coded in any production code, however these values have been left in the source code in these samples here for clarity.

Common Features
---------------

These projects (with the exception of the `MinimalDarwinPushPortClientV16` project) demonstrate the following:

 * Use of long running connections to stream messages (instead of repeated connects and disconnects which place excessive load on the messaging servers)
 * Use of multiple threads to receive messages to increase throughput and to separate the work of receiving messages from the work of processing messages
 * Automatic queuing of received messages in a local thread-safe in-memory queue (multiple processing threads can then pick up messages from this queue)
 * Capture of exceptions also in an in memory queue for logging (e.g. to log file or database)
 * Automatic connection retries for up to a specified total time interval (to simply your code from this logic)
 * When connecting or reconnecting, use of an exponential backoff (to prevent a recovering server from being overwhelmed)
 * Automatic disconnect and reconnect if no messages are received for 30 seconds following an exception on the connection to the messaging server.
 * Automatic disconnect and reconnect if no messages are received for 120 seconds (to handle the scenario of a quietly dropped connection)
 * Use of a durable subscription (applies to the Network Rail feeds only) to retain up to five minutes of message history when a client is disconnected
 * Use of a single connection to receive messages from multiple topics (applies to the Network Rail feeds only)

ExampleDarwinPushPortClient
---------------------------

The OpenRail*.cs files contain the core classes to receive messages.  These files can easily be copied into your application.

The Program.cs file contains the console application code.  This code illustrates how to use the core OpenRail* classes.

Before running the code, the lines in the Program.cs file must be updated to contain your personal details.  These can be found in the https://opendata.nationalrail.co.uk/feeds portal.

The core of the code to receive messages is as follows:

    // create the shared queues (into which the receiver will enqueue messages/errors)
    ConcurrentQueue<OpenRailMessage> oMessageQueue = new ConcurrentQueue<OpenRailMessage>();
    ConcurrentQueue<OpenRailException> oErrorQueue = new ConcurrentQueue<OpenRailException>();

    // create the receiver
    OpenRailDarwinPushPortReceiver oDarwinReceiver = new OpenRailDarwinPushPortReceiver(
        sConnectUrl, sUser, sPassword, sQueue, oMessageQueue, oErrorQueue, 100);

    // Start the receiver
    oDarwinReceiver.Start();

After these lines have been executed, the oMessageQueue object will start to be populated with messages from the feed.  These messages can be retrieved using a loop as follows:

    // attempt to dequeue and process some messages
    while (oMessageQueue.Count > 0)
    {
        OpenRailMessage oMessage = null;
        if (oMessageQueue.TryDequeue(out oMessage))
        {
            // All Darwin push port messages should be byte messages
            OpenRailBytesMessage oBytesMessage = oMessage as OpenRailBytesMessage;
            if (oBytesMessage != null)
            {
                iBytesMessageCount++;

                // the processing here simply deserializes the message to objects
                // Your code here could then write to a database, files, etc.
                Pport oPPort = DarwinMessageHelper.GetMessageAsObjects(oBytesMessage.Bytes);
                // insert your processing code here
            }
        }
    }

The oMessageQueue object is thread-safe.  This means the "while" loop above can be executed concurrently in multiple processing threads to provide greater processing throughput.

Note that because oMessageQueue is a ConcurrentQueue, occasionally (if another thread is accessing the queue at that very specific moment) TryDequeue() will return false, which means it wasn't able at that specific moment to retrieve a message.  This is normal - simply try again as shown in the code above.

This code was build against v1.7.0 of the Apache NMS Messaging API (http://activemq.apache.org/nms/).  The Apache.NMS and Apache.NMS.ActiveMQ assemblies can be downloaded from http://activemq.apache.org/nms/download.html.  The example uses OpenWire to connect to the service.

ExampleNetworkRailOpenDataClient
--------------------------------

The OpenRail*.cs files contain the core classes to receive messages.  These files can easily be copied into your application.

The Program.cs file contains the console application code.  This code illustrates how to use the core OpenRail* classes. 

Before running the code, the following lines in the Program.cs file must be updated to your Network Rail Data Feeds login:

    string sUser = "InsertYourUserIdHere";
    string sPassword = "InsertYourPasswordHere";

The example code supports receiving messages from up to two topics (though more could be supported by copying the code pattern).

By default the code reads from the TRUST (i.e. Train Movements) and Very Short Term Planning (VSTP) topics.

You may switch to different feeds by modifying the following lines:

    string sTopic1 = "TRAIN_MVT_ALL_TOC";
    string sTopic2 = "VSTP_ALL";

Whichever feeds you use, you must subscribe to these first in the Network Rail Data Feeds website.

The core of the code to receive messages is as follows:

    // create the shared queues (into which the receiver will enqueue messages/errors)
    ConcurrentQueue<OpenRailMessage> oMessageQueue1 = new ConcurrentQueue<OpenRailMessage>();
    ConcurrentQueue<OpenRailMessage> oMessageQueue2 = new ConcurrentQueue<OpenRailMessage>();
    ConcurrentQueue<OpenRailException> oErrorQueue = new ConcurrentQueue<OpenRailException>();

    // create the receiver
    OpenRailNRODReceiver oNRODReceiver = new OpenRailNRODReceiver(
        sConnectUrl, sUser, sPassword, sTopic1, sTopic2, oMessageQueue1, oMessageQueue2, oErrorQueue, bUseDurableSubscription, 100);

    // Start the receiver
    oNRODReceiver.Start();

After these lines have been executed, the oMessageQueue object will start to be populated with messages from the feed.  These messages can be retrieved using a loop as follows:

    // attempt to dequeue and process some messages
    while (oMessageQueue1.Count > 0)
    {
        OpenRailMessage oMessage = null;
        if (oMessageQueue1.TryDequeue(out oMessage))
        {
            // All Network Rail Open Data Messages should be text
            OpenRailTextMessage oTextMessage = oMessage as OpenRailTextMessage;
            if (oTextMessage != null)
            {
                iTextMessageCount1++;
                // insert your processing code here
            }
        }
    }

Note that because oMessageQueue is a ConcurrentQueue, occasionally (if another thread is accessing the queue at that very specific moment) TryDequeue() will return false, which means it wasn't able at that specific moment to retrieve a message.  This is normal - simply try again as shown in the code above.

This code was build against v1.5.1 of the Apache NMS Messaging API (http://activemq.apache.org/nms/).  The Apache.NMS and Apache.NMS.ActiveMQ assemblies can be downloaded from http://activemq.apache.org/nms/download.html.  The example uses Stomp to connect to the service.

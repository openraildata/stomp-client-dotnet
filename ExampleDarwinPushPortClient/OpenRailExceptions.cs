using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRailMessaging
{
    public class OpenRailException : ApplicationException
    {
        public OpenRailException(string sMessage)
            : base(sMessage)
        { }

        public OpenRailException(string sMessage, Exception oInnerException)
            : base(sMessage, oInnerException)
        { }

        public static string GetShortErrorInfo(Exception oException)
        {
            if (oException == null) return "(unknown)";
            else return oException.GetType().FullName + ": " + oException.Message;
        }
    }

    // occurs when an exception is encountered whilst attempting to connect to the open data service
    public class OpenRailConnectException : OpenRailException
    {
        public OpenRailConnectException(string sMessage, Exception oInnerException)
            : base(sMessage, oInnerException)
        { }
    }

    // occurs when time limit is reached for attempting to connect to the open data service
    public class OpenRailConnectTimeoutException : OpenRailException
    {
        public OpenRailConnectTimeoutException(string sMessage)
            : base(sMessage)
        { }

        public OpenRailConnectTimeoutException(string sMessage, Exception oInnerException)
            : base(sMessage, oInnerException)
        { }
    }

    // occurs when an exception is encountered receiving a message
    public class OpenRailMessageException : OpenRailException
    {
        public OpenRailMessageException(string sMessage, Exception oInnerException)
            : base(sMessage, oInnerException)
        { }
    }

    // occurs when the message receiver main worker thread fails
    public class OpenRailFatalException : OpenRailException
    {
        public OpenRailFatalException(string sMessage, Exception oInnerException)
            : base(sMessage, oInnerException)
        { }
    }
}

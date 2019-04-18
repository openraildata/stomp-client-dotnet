using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRailMessaging
{
    public class OpenRailMessage
    {
        private DateTime mdtNmsTimestamp = DateTime.MinValue;

        public OpenRailMessage(DateTime dtNmsTimestamp)
        {
            mdtNmsTimestamp = dtNmsTimestamp;
        }

        public DateTime NmsTimestamp { get { return mdtNmsTimestamp; } }
    }

    public class OpenRailTextMessage : OpenRailMessage
    {
        private string msText = null;

        public OpenRailTextMessage(DateTime dtNmsTimestamp, string sText) :
            base(dtNmsTimestamp)
        {
            msText = sText;
        }

        public string Text { get { return msText; } }
    }

    public class OpenRailBytesMessage : OpenRailMessage
    {
        private byte[] mbBytes = null;

        public OpenRailBytesMessage(DateTime dtNmsTimestamp, byte[] bBytes) :
            base(dtNmsTimestamp)
        {
            mbBytes = bBytes;
        }

        public byte[] Bytes { get { return mbBytes; } }
    }

    public class OpenRailUnsupportedMessage : OpenRailMessage
    {
        private string msMessageType = null;

        public OpenRailUnsupportedMessage(DateTime dtNmsTimestamp, string sMessageType) :
            base(dtNmsTimestamp)
        {
            msMessageType = sMessageType;
        }

        public string UnsupportedMessageType { get { return msMessageType; } }
    }
}

namespace MinimalDarwinPushPortClientV16
{
    public class OpenRailMessage
    {
        protected OpenRailMessage()
        {
        }
    }

    public class OpenRailBytesMessage : OpenRailMessage
    {
        public OpenRailBytesMessage(byte[] bBytes)
        {
            Bytes = bBytes;
        }

        public byte[] Bytes { get; }
    }
}

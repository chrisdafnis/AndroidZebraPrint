namespace AndroidZebraPrint
{
    class ZebraPrinter : IZebraPrinter
    {
        protected string macAddress = "";
        protected string friendlyName = "";

        public string MACAddress {  get { return macAddress; } set { macAddress = value; } }
        public string FriendlyName { get { return friendlyName; } set { friendlyName = value; } }

        public ZebraPrinter(string address, string name)
        {
            this.MACAddress = address;
            this.FriendlyName = name;
        }

        public override string ToString()
        {
            return FriendlyName;
        }
    }
}
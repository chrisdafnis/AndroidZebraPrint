namespace DakotaIntegratedSolutions
{
    class ZebraPrinter : IZebraPrinter
    {
        protected string macAddress = "", friendlyName = "";

        public string MACAddress { get { return macAddress; } set { macAddress = value; } }
        public string FriendlyName { get { return friendlyName; } set { friendlyName = value; } }

        public ZebraPrinter(string address, string name)
        {
            MACAddress = address;
            FriendlyName = name;
        }

        public override string ToString() => FriendlyName;
    }
}
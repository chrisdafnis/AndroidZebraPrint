namespace DakotaIntegratedSolutions
{
    public interface IZebraPrinter
    {
        string MACAddress { get; set; }
        string FriendlyName { get; set; }
        string ToString();
    }
}

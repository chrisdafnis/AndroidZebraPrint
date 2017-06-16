using LinkOS.Plugin.Abstractions;

namespace DakotaIntegratedSolutions
{
    public interface IPrinterDiscovery
    {
        void FindBluetoothPrinters(IDiscoveryHandler handler);
        void FindUSBPrinters(IDiscoveryHandler handler);
        void RequestUSBPermission(IDiscoveredPrinterUsb printer);
        void CancelDiscovery();
    }
}

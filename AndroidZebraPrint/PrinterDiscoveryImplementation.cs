using LinkOS.Plugin;
using LinkOS.Plugin.Abstractions;
using Android.Bluetooth;

namespace AndroidZebraPrint
{
    public enum ConnectionType
    {
        Bluetooth,
        USB,
        Network
    }

    public class PrinterDiscoveryImplementation : IPrinterDiscovery
    {
        public PrinterDiscoveryImplementation() { }

        public void CancelDiscovery()
        {
            if (BluetoothAdapter.DefaultAdapter.IsDiscovering)
            {
                BluetoothAdapter.DefaultAdapter.CancelDiscovery();
                System.Diagnostics.Debug.WriteLine("Cancelling Discovery");
            }
        }

        public void FindBluetoothPrinters(IDiscoveryHandler handler)
        {
            BluetoothDiscoverer.Current.FindPrinters(Android.App.Application.Context, handler);
        }

        public void FindUSBPrinters(IDiscoveryHandler handler)
        {
            UsbDiscoverer.Current.FindPrinters(Android.App.Application.Context, handler);
        }

        public void RequestUSBPermission(IDiscoveredPrinterUsb printer)
        {
            if (!printer.HasPermissionToCommunicate)
            {
                printer.RequestPermission(Android.App.Application.Context);
            }
        }
    }
}
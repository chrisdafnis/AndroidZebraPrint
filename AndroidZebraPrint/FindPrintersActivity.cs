using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System.Collections.ObjectModel;
using LinkOS.Plugin;
using LinkOS.Plugin.Abstractions;
using System.Reflection;

namespace AndroidZebraPrint
{
    [Activity(Label = "@+string/SearchPrinters", Theme = "@style/dialog_light")]
    class FindPrintersActivity : Activity
    {
        ListView printerListView;
        ObservableCollection<IZebraPrinter> printerList = new ObservableCollection<IZebraPrinter>();
        IZebraPrinter zebraPrinter;
        IFileUtil fileUtility;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.FindPrinters);

            printerListView = FindViewById<ListView>(Resource.Id.printerListView);
            printerListView.ItemClick += PrinterListView_ItemClick; ;
            // set up file utility for saving/loading settings
            fileUtility = new FileUtilImplementation();
            SearchForPrinters();
        }

        private void PrinterListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            try
            {
                IZebraPrinter bluetoothPrinter = printerList[e.Position];
                RemoveHandlers();
                if (bluetoothPrinter is IZebraPrinter)
                {
                    SetPrinter(bluetoothPrinter);
                    fileUtility.SaveXMLSettings(bluetoothPrinter);
                    zebraPrinter = (IZebraPrinter)fileUtility.LoadXMLSettings();
                }
            }
            catch (Exception ex)
            {
                
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }

            Intent returnIntent = new Intent();
            returnIntent.PutExtra("result", "found");
            SetResult(Result.Ok, returnIntent);
            Finish();
        }

        private void SetPrinter(IZebraPrinter bluetoothPrinter)
        {
            zebraPrinter = bluetoothPrinter;
        }

        private void SearchForPrinters()
        {
            SetUpHandlers();
            printerList = new ObservableCollection<IZebraPrinter>();
            LinkOS.Plugin.Abstractions.IDiscoveryHandler bthandler = DiscoveryHandler.Current;
            System.Diagnostics.Debug.WriteLine("Starting Bluetooth Discovery");
            IPrinterDiscovery discover = new PrinterDiscoveryImplementation();
            discover.FindBluetoothPrinters(bthandler);
        }

        private void SetUpHandlers()
        {
            DiscoveryHandler.OnDiscoveryError += DiscoveryHandler_OnDiscoveryError;
            DiscoveryHandler.OnDiscoveryFinished += DiscoveryHandler_OnDiscoveryFinished;
            DiscoveryHandler.OnFoundPrinter += DiscoveryHandler_OnFoundPrinter;
        }

        private void RemoveHandlers()
        {
            DiscoveryHandler.OnDiscoveryError -= DiscoveryHandler_OnDiscoveryError;
            DiscoveryHandler.OnDiscoveryFinished -= DiscoveryHandler_OnDiscoveryFinished;
            DiscoveryHandler.OnFoundPrinter -= DiscoveryHandler_OnFoundPrinter;
        }

        private void DiscoveryHandler_OnFoundPrinter(object sender, IDiscoveredPrinter discoveredPrinter)
        {
            System.Diagnostics.Debug.WriteLine("Found Printer:" + discoveredPrinter.ToString());
            IZebraPrinter bluetoothPrinter = new ZebraPrinter(discoveredPrinter.Address, ((IDiscoveredPrinterBluetooth)discoveredPrinter).FriendlyName);

            if (!printerList.Contains(bluetoothPrinter))
            {
                if (!String.IsNullOrEmpty(bluetoothPrinter.FriendlyName))
                    printerList.Add(bluetoothPrinter);
            }
        }

        private void DiscoveryHandler_OnDiscoveryFinished(object sender)
        {
            IZebraPrinter[] printers = new IZebraPrinter[printerList.Count];
            for (int i = 0; i < printerList.Count; i++)
            {
                printers[i] = printerList[i];
            }
            try
            {
                printerListView.Adapter = new AlternateRowAdapter(Android.App.Application.Context, Android.Resource.Layout.SimpleListItem1, printers);
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }
            RemoveHandlers();
        }

        private void DiscoveryHandler_OnDiscoveryError(object sender, string message)
        {
            //System.Diagnostics.Debug.WriteLine("On Discovery Error: " + connetionType.ToString());
            //OnError(message);
            RemoveHandlers();
        }
    }
}
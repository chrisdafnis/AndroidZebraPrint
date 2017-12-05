using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidZebraPrint2;
using LinkOS.Plugin;
using LinkOS.Plugin.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace DakotaIntegratedSolutions
{
    [Activity(Label = "@+string/SearchPrinters", Theme = "@style/dialog_light")]
    class FindPrintersActivity : Activity
    {
        ListView printerListView;
        ObservableCollection<IZebraPrinter> printerList = new ObservableCollection<IZebraPrinter>();
        IZebraPrinter zebraPrinter;
        IFileUtil fileUtility;
        IDiscoveryEventHandler discoveryEventHandler = DiscoveryHandlerFactory.Current.GetInstance();

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

        void PrinterListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            try
            {
                var bluetoothPrinter = printerList[e.Position];
                var bthandler = DiscoveryHandlerFactory.Current.GetInstance();
                RemoveHandlers(bthandler);
                if (bluetoothPrinter is IZebraPrinter)
                {
                    SetPrinter(bluetoothPrinter);
                    fileUtility.SaveXMLSettings(bluetoothPrinter);
                    zebraPrinter = (IZebraPrinter)fileUtility.LoadXMLSettings();
                }
            }
            catch (Exception ex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }

            var returnIntent = new Intent();
            returnIntent.PutExtra("result", "found");
            SetResult(Result.Ok, returnIntent);
            Finish();
        }

        void SetPrinter(IZebraPrinter bluetoothPrinter) => zebraPrinter = bluetoothPrinter;

        void SearchForPrinters()
        {
            printerList = new ObservableCollection<IZebraPrinter>();
            var bthandler = DiscoveryHandlerFactory.Current.GetInstance();
            SetUpHandlers(bthandler);
            System.Diagnostics.Debug.WriteLine("Starting Bluetooth Discovery");
            IPrinterDiscovery discover = new PrinterDiscoveryImplementation();
            discover.FindBluetoothPrinters(bthandler);
        }

        void SetUpHandlers(IDiscoveryEventHandler bthandler)
        {
            bthandler.OnDiscoveryError += DiscoveryHandler_OnDiscoveryError;
            bthandler.OnDiscoveryFinished += DiscoveryHandler_OnDiscoveryFinished;
            bthandler.OnFoundPrinter += DiscoveryHandler_OnFoundPrinter;
        }

        void RemoveHandlers(IDiscoveryEventHandler bthandler)
        {
            bthandler.OnDiscoveryError -= DiscoveryHandler_OnDiscoveryError;
            bthandler.OnDiscoveryFinished -= DiscoveryHandler_OnDiscoveryFinished;
            bthandler.OnFoundPrinter -= DiscoveryHandler_OnFoundPrinter;
        }

        void DiscoveryHandler_OnFoundPrinter(object sender, IDiscoveredPrinter discoveredPrinter)
        {
            System.Diagnostics.Debug.WriteLine("Found Printer:" + discoveredPrinter.ToString());
            IZebraPrinter bluetoothPrinter = new ZebraPrinter(discoveredPrinter.Address, ((IDiscoveredPrinterBluetooth)discoveredPrinter).FriendlyName);

            if (!printerList.Contains(bluetoothPrinter))
            {
                if (!string.IsNullOrEmpty(bluetoothPrinter.FriendlyName))
                    printerList.Add(bluetoothPrinter);
            }
        }

        void DiscoveryHandler_OnDiscoveryFinished(object sender)
        {
            IZebraPrinter[] printers = new IZebraPrinter[printerList.Count];
            for (int i = 0; i < printerList.Count; i++)
                printers[i] = printerList[i];

            try
            {
                printerListView.Adapter = new AlternateRowAdapter(Android.App.Application.Context, Android.Resource.Layout.SimpleListItem1, printers);
            }
            catch (Exception ex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }

            var bthandler = DiscoveryHandlerFactory.Current.GetInstance();
            RemoveHandlers(bthandler);
        }

        void DiscoveryHandler_OnDiscoveryError(object sender, string message)
        {
            var bthandler = DiscoveryHandlerFactory.Current.GetInstance();
            RemoveHandlers(bthandler);
        }
    }
}
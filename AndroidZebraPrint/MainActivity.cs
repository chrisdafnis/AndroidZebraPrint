using Android.App;
using Android.Widget;
using Android.OS;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Text;
using Android.Views;
using Android.Content;
using System.Xml.Linq;
using System.Collections.Generic;
using static Android.Widget.AdapterView;
using System.Reflection;

namespace AndroidZebraPrint
{
    [Activity(Label = "@+string/AppName", MainLauncher = true, Icon = "@drawable/dakota_healthcare_icon")]
    public class MainActivity : Activity
    {
        IZebraPrinter zebraPrinter;
        IFileUtil fileUtility;
        ListView locationsView;
        ObservableCollection<IGLNLocation> locationList;
        int printQuantity = 1;
        string locationsFile;
        int currentSelected = 0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button printButton = FindViewById<Button>(Resource.Id.PrintButton);
            printButton.Click += PrintButton_Click;
            // disable print button until we have a printer connected and something selected to print
            printButton.Enabled = false;
            TextView selectedPrinter = (TextView)FindViewById<TextView>(Resource.Id.selectedPrinterTxt);
            selectedPrinter.Text = "";

            locationsView = ((ListView)FindViewById<ListView>(Resource.Id.locationsView));

            // set up file utility for saving/loading settings
            fileUtility = new FileUtilImplementation();
            zebraPrinter = (IZebraPrinter)fileUtility.LoadXMLSettings();
            try
            {
                ((TextView)FindViewById<TextView>(Resource.Id.selectedPrinterTxt)).Text = zebraPrinter.FriendlyName;
                printButton.Enabled = true;
            }
            catch (Exception ex)
            {
                ((TextView)FindViewById<TextView>(Resource.Id.selectedPrinterTxt)).SetText(Resource.String.NoPrinter);
            
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }

            if (!AntiPiracyCheck())
            {
                AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

                dialogBuilder.SetTitle("Error");
                dialogBuilder.SetMessage("This software is not permitted for use on this device.\n\rPlease contact your IT department.");
                dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                dialogBuilder.SetPositiveButton(Android.Resource.String.Ok, delegate
                    {
                        Finish();
                    });
                dialogBuilder.Show();
            }
            else
            {
                // load list of locations files
                var findFilesPage = new Android.Content.Intent(this, typeof(FindFilesActivity));
                StartActivityForResult(findFilesPage, 3);
            }
        }

        private bool AntiPiracyCheck()
        {
            IList<string> validDeviceSerialNumbers = new string[] { "FHP5CM00227", "FHP5CM00269", "FHP5CM00232", "FHP5CM00144", "FHP5CM00013", "FHP4AM00107", "FHP52M00438" };
            bool isValid = false;

            if (validDeviceSerialNumbers.Contains(Build.Serial))
                isValid = true;

            return isValid;
        }

        public override void OnBackPressed()
        {
            AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

            dialogBuilder.SetTitle(Resource.String.QuitApplication);
            dialogBuilder.SetMessage(Resource.String.QuitPrompt);
            dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
            dialogBuilder.SetNegativeButton(Android.Resource.String.No, delegate { });
            dialogBuilder.SetPositiveButton(Android.Resource.String.Yes, delegate
            {
                this.Finish();
            });
            dialogBuilder.Show();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.popup_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.FindPrinters:
                    var findPrintersPage = new Android.Content.Intent(this, typeof(FindPrintersActivity));
                    StartActivityForResult(findPrintersPage, 0);
                    return true;
                case Resource.Id.LoadDataFile:
                    var findFilesPage = new Android.Content.Intent(this, typeof(FindFilesActivity));
                    StartActivityForResult(findFilesPage, 3);
                    return true;
                case Resource.Id.RowInfo:
                    var rowInfoPage = new Android.Content.Intent(this, typeof(LocationInfoActivity));
                    IGLNLocation location = locationList[currentSelected];
                    String info = String.Format("  Region: {0}\n\r" +
                                                " Site: {1}\n\r" + 
                                                " Building: {2}\n\r" + 
                                                " Floor: {3}\n\r" + 
                                                " Room: {4}\n\r" + 
                                                " Code: {5}\n\r" + 
                                                " GLN: {6}\n\r" + 
                                                " Date: {7}",
                        location.Region,
                        location.Site,
                        location.Building,
                        location.Floor,
                        location.Room,
                        location.Code,
                        location.GLN,
                        location.Date.ToString());
                    rowInfoPage.PutExtra("location", info);
                    StartActivityForResult(rowInfoPage, 5);
                    return true;
                case Resource.Id.About:
                    var aboutPage = new Android.Content.Intent(this, typeof(AboutButtonActivity));
                    StartActivityForResult(aboutPage, 4);
                    return true;
                case Resource.Id.SearchLocation:
                    var searchLocationPage = new Android.Content.Intent(this, typeof(FindLocationActivity));
                    StartActivityForResult(searchLocationPage, 6);
                    return true;
                case Resource.Id.Quit:
                    AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

                    dialogBuilder.SetTitle(Resource.String.QuitApplication);
                    dialogBuilder.SetMessage(Resource.String.QuitPrompt);
                    dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                    dialogBuilder.SetNegativeButton(Android.Resource.String.No, delegate { });
                    dialogBuilder.SetPositiveButton(Android.Resource.String.Yes, delegate
                    {
                        this.Finish();
                    });
                    dialogBuilder.Show();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == 0)   // result from FindPrinters
            {
                if (resultCode == Result.Ok)
                {
                    String result = data.GetStringExtra("result");
                    LoadXMLSettings();
                }
                if (resultCode == Result.Canceled)
                {
                    //Write your code if there's no result
                }
            }
            else if (requestCode == 1)  // result from LoadData
            {
                if (resultCode == Result.Ok)
                {
                    
                }
                if (resultCode == Result.Canceled)
                {
                    //Write your code if there's no result
                }
            }
            else if (requestCode == 2)   // result from PrintQuantity
            {
                if (resultCode == Result.Ok)
                {
                    printQuantity = data.GetIntExtra("quantity", 1);
                    new Task(new Action(() => {
                        SendZplOverBluetooth(); 
                    })).Start();
                    try
                    {
                        currentSelected = ((CustomArrayAdapter)locationsView.Adapter).GetSelectedIndex();
                        View selected = locationsView.GetChildAt(currentSelected);
                        ((CustomArrayAdapter)locationsView.Adapter).SetPrintedIndex(currentSelected);
                    }
                    catch (Exception ex)
                    {
                        //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                        fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
                    }
                }
                if (resultCode == Result.Canceled)
                {
                    //Write your code if there's no result
                }
            }
            else if (requestCode == 3)   // result from PrintQuantity
            {
                if (resultCode == Result.Ok)
                {
                    locationsFile = data.GetStringExtra("filename");
                    LoadLocations(locationsFile);
                }
                if (resultCode == Result.Canceled)
                {
                    //Write your code if there's no result
                }
            }
            else if (requestCode == 5)   // result from LocationInfo
            {
                if (resultCode == Result.Ok)
                {
                }
                if (resultCode == Result.Canceled)
                {
                    //Write your code if there's no result
                }
            }
            else if (requestCode == 6)   // result from Location Search
            {
                if (resultCode == Result.Ok)
                {
                    string searchLocation = data.GetStringExtra("location");
                    CustomArrayAdapter adapter = (CustomArrayAdapter)locationsView.Adapter;
                    int i = 0;
                    object item = null;
                    foreach (IGLNLocation location in locationList)
                    {
                        if (location.Code == searchLocation)
                        {
                            item = adapter.GetItem(i);
                            adapter.SetSelectedIndex(i);
                            locationsView.SmoothScrollToPosition(i);
                            break;
                        }
                        i++;
                    }
                    if (item == null)
                    {
                        AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

                        dialogBuilder.SetTitle("Code Not Found");
                        dialogBuilder.SetMessage("The room code '" + searchLocation + "' does not exist in the current database");
                        dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                        dialogBuilder.SetPositiveButton(Android.Resource.String.Ok, delegate { });
                        dialogBuilder.Show();
                    }
                }
                if (resultCode == Result.Canceled)
                {
                    //Write your code if there's no result
                }
            }
        }

        private void LoadLocations(string filename)
        {
            XDocument xDoc = (XDocument)fileUtility.LoadXMLGLNFile(filename);
            if (xDoc != null)
            {
                locationList = new ObservableCollection<IGLNLocation>();
                XName xName = XName.Get("GLNLocation");

                foreach (XElement xElem in xDoc.Descendants("GLNLocation"))
                {
                    IGLNLocation location = new GLNLocation();
                    location.Region = xElem.Element("Region").Value;
                    location.Site = xElem.Element("Site").Value;
                    location.Building = xElem.Element("Building").Value;
                    location.Floor = xElem.Element("Floor").Value;
                    location.Room = xElem.Element("Room").Value;
                    location.Code = xElem.Element("Code").Value;
                    location.GLN = xElem.Element("GLN").Value;
                    location.Date = Convert.ToDateTime(xElem.Element("GLNCreationDate").Value);

                    if (!locationList.Contains(location))
                    {
                        locationList.Add(location);
                    }
                }
                IGLNLocation[] locations = new IGLNLocation[locationList.Count];

                for (int i = 0; i < locationList.Count; i++)
                {
                    locations[i] = locationList[i];
                }
                try
                {
                    locationsView.Adapter = new CustomArrayAdapter(Android.App.Application.Context, Android.Resource.Layout.SimpleListItem1, locations);
                    locationsView.ItemClick += (object sender, ItemClickEventArgs e) =>
                        {
                            ((CustomArrayAdapter)((ListView)sender).Adapter).SetSelectedIndex(e.Position);
                        };
                }
                catch (Exception ex)
                {
                    //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                    fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
                }
            }
            else
            {
                AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

                dialogBuilder.SetTitle("File Error");
                dialogBuilder.SetMessage("There was a problem loading this file. Please choose another file, or correct the error and try again.");
                dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                dialogBuilder.SetPositiveButton(Android.Resource.String.Ok, delegate { });
                dialogBuilder.Show();
            }
        }

        private void LoadXMLSettings()
        {
            zebraPrinter = (IZebraPrinter)fileUtility.LoadXMLSettings();
            try
            {
                ((TextView)FindViewById<TextView>(Resource.Id.selectedPrinterTxt)).Text = zebraPrinter.FriendlyName;
                ((Button)FindViewById<Button>(Resource.Id.PrintButton)).Enabled = true;
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }
        }

        private void PrintButton_Click(object sender, EventArgs e)
        {
            if (CheckPrinter())
            {
                var printQuantityPage = new Android.Content.Intent(this, typeof(PrintQuantityActivity));
                StartActivityForResult(printQuantityPage, 2);
                locationsView = ((ListView)FindViewById<ListView>(Resource.Id.locationsView));
            }
        }

        private bool SendZplOverBluetooth()
        {
            bool success = false;
            try
            {
                //IConnection connection = ConnectionBuilder.Current.Build("BT:" + zebraPrinter.MACAddress);
                Zebra.Sdk.Comm.BluetoothConnectionInsecure connection = new Zebra.Sdk.Comm.BluetoothConnectionInsecure(zebraPrinter.MACAddress);
                // Open the connection - physical connection is established here.
                connection.Open();
                //thePrinterConn.Open();

                // Actual Label
                string zplData = GetZplGLNLabel(locationList[currentSelected]);

                // Send the data to printer as a byte array.
                //connection.Write(GetBytes(zplData));
                byte[] response = connection.SendAndWaitForResponse(GetBytes(zplData), 3000, 1000, "\"");

                //thePrinterConn.Close();
                connection.Close();
                success = true;
            }
            catch (Zebra.Sdk.Comm.ConnectionException ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }
            return success;
        }

        private byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            bytes = Encoding.UTF8.GetBytes(str);
            return bytes;
        }

        private string GetZplGLNLabel(IGLNLocation location)
        {
            string zpl =
                @"^XA" + "\r\n" +
                @"^MMT" + "\r\n" +
                @"^PW601" + "\r\n" +
                @"^LL0406" + "\r\n" +
                @"^LS0" + "\r\n";
            if (location == null)
            {
                zpl +=
                    @"^BY3,3,230^FT508,109^BCI,,N,N^FD>;>8414" + "1234567890123" + "^FS" + "\r\n" +
                    @"^FT441,71^A0I,34,33^FB276,1,0,C^FH\^FD(414)" + "1234567890123" + "\r\n";
            }
            else
            {

                zpl +=
                    @"^BY3,3,230^FT508,109^BCI,,N,N^FD>;>8414" + locationList[currentSelected].GLN + "^FS" + "\r\n" +
                    @"^FT441,71^A0I,34,33^FB276,1,0,C^FH\^FD(414)" + locationList[currentSelected].GLN + "\r\n";
            }
            zpl += @"^PQ" + printQuantity + ",0,1,Y^XZ" + "\r\n";
            return zpl;
        }

        private bool CheckPrinter()
        {
            if (null == zebraPrinter)
                return false;
            else
            return true;
        }
    }
}


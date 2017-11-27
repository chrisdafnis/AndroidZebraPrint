﻿using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using static Android.Widget.AdapterView;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Zebra.Sdk.Comm;
using AndroidZebraPrint2;
using AndroidHUD;
using System.Linq;
using static DakotaIntegratedSolutions.FileUtilImplementation;

//using Com.Mitac.Cell.Device;

namespace DakotaIntegratedSolutions
{
    [Activity(Label = "@+string/AppName", MainLauncher = true, Icon = "@drawable/dakota_healthcare_icon", Theme = "@android:style/Theme.Holo.Light", ConfigurationChanges = (Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize))]
    public class MainActivity : Activity
    {
        IZebraPrinter zebraPrinter;
        IFileUtil fileUtility;
        ListView locationsView;
        ObservableCollection<IGLNLocation> locationList;
        int printQuantity = 1;
        string locationsFile;
        int currentSelected = 0;
        public enum ActivityCode { FindPrinters = 0, PrintQuantity, LoadLocations, About/*, LocationInfo, LocationSearch*/ };

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(AndroidZebraPrint2.Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button printButton = FindViewById<Button>(AndroidZebraPrint2.Resource.Id.PrintButton);
            printButton.Click += PrintButton_Click;
            // disable print button until we have a printer connected and something selected to print
            printButton.Enabled = false;
            TextView selectedPrinter = (TextView)FindViewById<TextView>(AndroidZebraPrint2.Resource.Id.selectedPrinterTxt);
            selectedPrinter.Text = "";

            locationsView = ((ListView)FindViewById<ListView>(AndroidZebraPrint2.Resource.Id.locationsView));

            // set up file utility for saving/loading settings
            fileUtility = new FileUtilImplementation();
            zebraPrinter = (IZebraPrinter)fileUtility.LoadXMLSettings();
            try
            {
                ((TextView)FindViewById<TextView>(AndroidZebraPrint2.Resource.Id.selectedPrinterTxt)).Text = zebraPrinter.FriendlyName;
                printButton.Enabled = true;
            }
            catch (Exception ex)
            {
                ((TextView)FindViewById<TextView>(AndroidZebraPrint2.Resource.Id.selectedPrinterTxt)).SetText(AndroidZebraPrint2.Resource.String.NoPrinter);
            
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }

#if DEBUG
#else
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
                StartActivityForResult(findFilesPage, (int)ActivityCode.LoadLocations);
            }
#endif
        }

        private bool AntiPiracyCheck()
        {
            IList<string> validDeviceSerialNumbers = Resources.GetStringArray(AndroidZebraPrint2.Resource.Array.valid_devices);
                //new string[] { "FHP5CM00227", "FHP5CM00269", "FHP5CM00232", "FHP5CM00144", "FHP5CM00013", "FHP4AM00107", "FHP52M00438", "FHP52M00075", "FHP52M00242" };
            bool isValid = false;

            if (validDeviceSerialNumbers.Contains(Build.Serial))
                isValid = true;

            return isValid;
        }

        public override void OnBackPressed()
        {
            AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

            dialogBuilder.SetTitle(AndroidZebraPrint2.Resource.String.QuitApplication);
            dialogBuilder.SetMessage(AndroidZebraPrint2.Resource.String.QuitPrompt);
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
            MenuInflater.Inflate(AndroidZebraPrint2.Resource.Menu.popup_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case AndroidZebraPrint2.Resource.Id.FindPrinters:
                    var findPrintersPage = new Android.Content.Intent(this, typeof(FindPrintersActivity));
                    StartActivityForResult(findPrintersPage, (int)ActivityCode.FindPrinters);
                    return true;
                case AndroidZebraPrint2.Resource.Id.LoadDataFile:
                    var findFilesPage = new Android.Content.Intent(this, typeof(FindFilesActivity));
                    StartActivityForResult(findFilesPage, (int)ActivityCode.LoadLocations);
                    return true;
                //case AndroidZebraPrint2.Resource.Id.RowInfo:
                //    var rowInfoPage = new Android.Content.Intent(this, typeof(LocationInfoActivity));
                //    IGLNLocation location = locationList[currentSelected];
                //    String info = String.Format("  Region: {0}\n\r" +
                //                                " Site: {1}\n\r" + 
                //                                " Building: {2}\n\r" + 
                //                                " Floor: {3}\n\r" + 
                //                                " Room: {4}\n\r" + 
                //                                " Code: {5}\n\r" + 
                //                                " GLN: {6}\n\r" + 
                //                                " Date: {7}",
                //        location.Region,
                //        location.Site,
                //        location.Building,
                //        location.Floor,
                //        location.Room,
                //        location.Code,
                //        location.GLN,
                //        location.Date.ToString());
                //    rowInfoPage.PutExtra("location", info);
                //    StartActivityForResult(rowInfoPage, (int)ActivityCode.LocationInfo);
                //    return true;
                case AndroidZebraPrint2.Resource.Id.About:
                    var aboutPage = new Android.Content.Intent(this, typeof(AboutButtonActivity));
                    StartActivityForResult(aboutPage, (int)ActivityCode.About);
                    return true;
                //case AndroidZebraPrint2.Resource.Id.SearchLocation:
                //    var searchLocationPage = new Android.Content.Intent(this, typeof(FindLocationActivity));
                //    StartActivityForResult(searchLocationPage, (int)ActivityCode.LocationSearch);
                //    return true;
                case AndroidZebraPrint2.Resource.Id.Quit:
                    AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

                    dialogBuilder.SetTitle(AndroidZebraPrint2.Resource.String.QuitApplication);
                    dialogBuilder.SetMessage(AndroidZebraPrint2.Resource.String.QuitPrompt);
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
            switch ((ActivityCode)requestCode)
            {
                case ActivityCode.FindPrinters:
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
                    break;
                case ActivityCode.PrintQuantity:
                    {
                        if (resultCode == Result.Ok)
                        {
                            printQuantity = data.GetIntExtra("quantity", 1);
                            new Task(new Action(() =>
                            {
                                SendZplOverBluetooth();
                            })).Start();
                            try
                            {
                                currentSelected = ((CustomArrayAdapter)locationsView.Adapter).GetSelectedIndex();
                                View selected = locationsView.GetChildAt(currentSelected);
                                ((CustomArrayAdapter)locationsView.Adapter).SetPrintedIndex(currentSelected);
                                SaveFile(locationsFile, locationList[currentSelected]);
                                //fileUtility.SaveLocation(locationsFile, locationList[currentSelected]);
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
                    break;
                case ActivityCode.LoadLocations:
                    {
                        if (resultCode == Result.Ok)
                        {
                            locationsFile = data.GetStringExtra("filename");
                            LoadFile(locationsFile);
                        }
                        //if (resultCode == Result.Canceled)
                        //{
                        //    AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

                        //    dialogBuilder.SetTitle("File Not Found");
                        //    dialogBuilder.SetMessage("No GLN Locations files found on SD card. Application will now exit.");
                        //    dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                        //    dialogBuilder.SetPositiveButton(Android.Resource.String.Ok, delegate
                        //    {
                        //        this.Finish();
                        //    });
                        //    dialogBuilder.Show();
                        //}
                    }
                    break;
                    //case ActivityCode.LocationInfo:
                    //    {
                    //        if (resultCode == Result.Ok)
                    //        {
                    //        }
                    //        if (resultCode == Result.Canceled)
                    //        {
                    //            //Write your code if there's no result
                    //        }
                    //    }
                    //    break;
                    //    case ActivityCode.LocationSearch:
                    //        {
                    //            if (resultCode == Result.Ok)
                    //            {
                    //                string searchLocation = data.GetStringExtra("location");
                    //                CustomArrayAdapter adapter = (CustomArrayAdapter)locationsView.Adapter;
                    //                int i = 0;
                    //                bool found = false;
                    //                foreach (IGLNLocation location in locationList)
                    //                {
                    //                    if (location.Code == searchLocation)
                    //                    {
                    //                        found = true;
                    //                        adapter.SetSelectedIndex(i);
                    //                        currentSelected = ((CustomArrayAdapter)locationsView.Adapter).GetSelectedIndex();
                    //                        locationsView.SmoothScrollToPosition(i);
                    //                        break;
                    //                    }
                    //                    i++;
                    //                }
                    //                if (!found)
                    //                {
                    //                    AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);

                    //                    dialogBuilder.SetTitle("Code Not Found");
                    //                    dialogBuilder.SetMessage("The room code '" + searchLocation + "' does not exist in the current database");
                    //                    dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                    //                    dialogBuilder.SetPositiveButton(Android.Resource.String.Ok, delegate { });
                    //                    dialogBuilder.Show();
                    //                }
                    //            }
                    //            if (resultCode == Result.Canceled)
                    //            {
                    //                //Write your code if there's no result
                    //            }
                    //        }
                    //        break;
            }
        }

        public async void SaveFile(string filename, IGLNLocation location)
        {
            try
            {
                AndHUD.Shared.Show(this, "Updating...", -1, MaskType.Black);
                await Task.Factory.StartNew(() => fileUtility.SaveLocation(filename, location));
                AndHUD.Shared.Dismiss(this);
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
        }
        public async void LoadFile(string filename)
        {
            string xmlString = String.Empty;
            object res = null;
            try
            {
                AndHUD.Shared.Show(this, "Loading...", -1, MaskType.Black);
                string xml = LoadLocations(filename);
                if (xml.Length > 0)
                {
                    Func<string> function = new Func<string>(() => xml);
                    res = await Task.Factory.StartNew<string>(function);
                }
                else
                {
                    AndHUD.Shared.Dismiss(this);
                    AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);
                    dialogBuilder.SetTitle("File Error");
                    dialogBuilder.SetMessage("There was a problem loading this file, it is not in the required format.");
                    dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                    dialogBuilder.SetPositiveButton(Android.Resource.String.Ok, delegate { });
                    dialogBuilder.Show();
                    throw new Exception();
                }
                AndHUD.Shared.Dismiss(this);
            }
            catch (Exception ex)
            {//call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
                return;
            }

            XDocument xDoc = XDocument.Parse((string)res);
            if (xDoc != null)
            {
                CSVFileFormat fileType = fileUtility.FileFormat;
                locationList = new System.Collections.ObjectModel.ObservableCollection<IGLNLocation>();
                XName xName = XName.Get("GLNLocation");
                if (fileType == CSVFileFormat.PLYMOUTH)
                {
                    foreach (XElement xElem in xDoc.Descendants("GLNLocation"))
                    {
                        IGLNLocation location = new GLNLocation()
                        {
                            Region = xElem.Element("Region").Value,
                            Site = xElem.Element("Site").Value,
                            Building = xElem.Element("Building").Value,
                            Floor = xElem.Element("Floor").Value,
                            Room = xElem.Element("Room").Value,
                            Code = xElem.Element("Code").Value,
                            GLN = xElem.Element("GLN").Value,
                            Date = Convert.ToDateTime(xElem.Element("GLNCreationDate").Value),
                            VariableText = xElem.Element("FreeText").Value,
                            Printed = Convert.ToBoolean(xElem.Element("Printed").Value)
                        };
                        if (!locationList.Contains(location))
                        {
                            locationList.Add(location);
                        }
                    }
                }
                else
                {
                    foreach (XElement xElem in xDoc.Descendants("GLNLocation"))
                    {
                        IGLNLocation location = new GLNLocation()
                        {
                            Region = xElem.Element("Region").Value,
                            Site = xElem.Element("Site").Value,
                            Building = xElem.Element("Building").Value,
                            Floor = xElem.Element("Floor").Value,
                            Room = xElem.Element("Room").Value,
                            Code = xElem.Element("Code").Value,
                            GLN = xElem.Element("GLN").Value,
                            Date = Convert.ToDateTime(xElem.Element("GLNCreationDate").Value),
                            Printed = Convert.ToBoolean(xElem.Element("Printed").Value)
                        };
                        if (!locationList.Contains(location))
                        {
                            locationList.Add(location);
                        }
                    }
                }

                IGLNLocation[] locations = new IGLNLocation[locationList.Count];
                try
                {
                    locationsView.Adapter = new CustomArrayAdapter(Android.App.Application.Context, Android.Resource.Layout.SimpleListItem1, locations);

                    for (int i = 0; i < locationList.Count; i++)
                    {
                        locations[i] = locationList[i];
                        if (locations[i].Printed)
                            ((CustomArrayAdapter)locationsView.Adapter).SetPrintedIndex(i);
                    }

                    locationsView.ItemClick += (object sender, ItemClickEventArgs e) =>
                    {
                        ((CustomArrayAdapter)((ListView)sender).Adapter).SetSelectedIndex(e.Position);
                        currentSelected = ((CustomArrayAdapter)locationsView.Adapter).GetSelectedIndex();
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

        private string LoadLocations(string filename)
        {
            XDocument xDoc = (XDocument)fileUtility.LoadGLNFile(filename);
            string returnValue = string.Empty;
            if (xDoc != null)
            {
                returnValue = String.Concat(@"<?xml version=""1.0"" encoding=""utf-8"" ?>", xDoc.ToString());
            }
            return returnValue;
        }

        private void SaveLocations(string filename, IGLNLocation location)
        {
            fileUtility.SaveLocation(filename, location);
        }

        private void LoadXMLSettings()
        {
            zebraPrinter = (IZebraPrinter)fileUtility.LoadXMLSettings();
            try
            {
                ((TextView)FindViewById<TextView>(AndroidZebraPrint2.Resource.Id.selectedPrinterTxt)).Text = zebraPrinter.FriendlyName;
                ((Button)FindViewById<Button>(AndroidZebraPrint2.Resource.Id.PrintButton)).Enabled = true;
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
                StartActivityForResult(printQuantityPage, (int)ActivityCode.PrintQuantity);
                locationsView = ((ListView)FindViewById<ListView>(AndroidZebraPrint2.Resource.Id.locationsView));
            }
        }

        private bool SendZplOverBluetooth()
        {
            bool success = false;
            try
            {
                IConnection connection = ConnectionBuilder.Build("BT:" + zebraPrinter.MACAddress);
                //Zebra.Sdk.Comm.BluetoothConnectionInsecure connection = new Zebra.Sdk.Comm.BluetoothConnectionInsecure(zebraPrinter.MACAddress);
                // Open the connection - physical connection is established here.
                connection.Open();
                //thePrinterConn.Open();

                // Actual Label
                string zplData = GetZplGLNLabel(locationList[currentSelected]);
                fileUtility.LogFile("ZPL Output Debug", zplData, "MainActivity", 440, "SendZplOverBluetooth");

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
                if (locationList[currentSelected].Region == "ROYAL CORNWALL HOSPITALS NHS TRUST")
                {
                    // Royal Cornwall want the room code above the barcode
                    zpl +=
                        @"^FT591,340^A0I,54,52^FD" + "Room Number:" + locationList[currentSelected].Code + "^FS" + "\r\n" +
                        @"^BY3,3,230^FT508,89^BCI,,N,N^FD>;>8414" + locationList[currentSelected].GLN + "^FS" + "\r\n" +
                        @"^FT441,41^A0I,34,33^FB276,1,0,C^FH\^FD(414)" + locationList[currentSelected].GLN + "\r\n";
                    //@"^FT591,360^A0I,40,39^FD" + "Room Number:" + locationList[currentSelected].Code + "^FS" + "\r\n" +
                    //@"^BY3,3,230^FT508,109^BCI,,N,N^FD>;>8414" + locationList[currentSelected].GLN + "^FS" + "\r\n" +
                    //@"^FT441,71^A0I,34,33^FB276,1,0,C^FH\^FD(414)" + locationList[currentSelected].GLN + "\r\n";
                }
                else if (locationList[currentSelected].Region == "Plymouth Hospitals NHS Trust")
                {
                    locationList[currentSelected].VariableText = locationList[currentSelected].VariableText.Substring(0, Math.Min(28, locationList[currentSelected].VariableText.Length));
                    while (locationList[currentSelected].VariableText.Length < 28)
                    {
                        locationList[currentSelected].VariableText = " " + locationList[currentSelected].VariableText + " ";
                    }
                    // Plymouth want variable text above the barcode
                    zpl +=
                        @"^FT581,340^A0I,54,52^FD" + locationList[currentSelected].VariableText + "^FS" + "\r\n" +
                        @"^BY3,3,230^FT508,89^BCI,,N,N^FD>;>8414" + locationList[currentSelected].GLN + "^FS" + "\r\n" +
                        @"^FT441,41^A0I,34,33^FB276,1,0,C^FH\^FD(414)" + locationList[currentSelected].GLN + "\r\n";
                    //@"^FT591,360^A0I,40,39^FD" + "Room Number:" + locationList[currentSelected].Code + "^FS" + "\r\n" +
                    //@"^BY3,3,230^FT508,109^BCI,,N,N^FD>;>8414" + locationList[currentSelected].GLN + "^FS" + "\r\n" +
                    //@"^FT441,71^A0I,34,33^FB276,1,0,C^FH\^FD(414)" + locationList[currentSelected].GLN + "\r\n";
                }
                else
                {
                    zpl +=
                        @"^BY3,3,230^FT508,109^BCI,,N,N^FD>;>8414" + locationList[currentSelected].GLN + "^FS" + "\r\n" +
                        @"^FT441,71^A0I,34,33^FB276,1,0,C^FH\^FD(414)" + locationList[currentSelected].GLN + "\r\n";
                }
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

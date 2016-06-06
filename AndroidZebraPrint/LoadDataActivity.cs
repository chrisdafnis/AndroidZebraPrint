using System;

using Android.App;
using Android.Content;
using Android.OS;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace AndroidZebraPrint
{
    [Activity(Label = "@+string/LoadLocations", Theme = "@android:style/Theme.Dialog")]
    class LoadDataActivity : Activity
    {
        const int fields = 8;
        ObservableCollection<IGLNLocation> locationList = new ObservableCollection<IGLNLocation>();
        protected IGLNLocation currentLocation;
        IFileUtil fileUtility;
        XDocument xDoc;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.LoadData);

            fileUtility = new FileUtilImplementation();
            LoadGLNLocations();

            Intent returnIntent = new Intent();
            returnIntent.PutExtra("glnLocations", xDoc.ToString());
            SetResult(Result.Ok, returnIntent);
            Finish();
        }

        private void LoadGLNLocations()
        {
            xDoc = (XDocument)fileUtility.LoadGLNFile(@"GLNStoImport.csv");
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
        }
    }
}
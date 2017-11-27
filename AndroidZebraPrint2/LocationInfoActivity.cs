using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;

namespace DakotaIntegratedSolutions
{
    [Activity(Label = "@+string/RowInfo", Theme = "@style/dialog_light")]
    public class LocationInfoActivity : Activity
    {
        TextView locationInfo;

        public IGLNLocation location;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(AndroidZebraPrint2.Resource.Layout.LocationInfo);

            Button btnOK = ((Button)FindViewById<Button>(AndroidZebraPrint2.Resource.Id.buttonOK));
            btnOK.Click += BtnOK_Click;

            locationInfo = FindViewById<TextView>(AndroidZebraPrint2.Resource.Id.locationInfoText);

            locationInfo.Text = Intent.GetStringExtra("location");
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Intent returnIntent = new Intent();
            SetResult(Result.Ok, returnIntent);
            Finish();
        }
    }
}
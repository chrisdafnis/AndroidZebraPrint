using Android.App;
using Android.OS;
using Android.Widget;
using AndroidZebraPrint2;
using System;

namespace DakotaIntegratedSolutions
{
    [Activity(Label = "@+string/AboutBox", Theme = "@style/dialog_light")]
    public class AboutButtonActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.AboutBox);
            ((TextView)FindViewById<TextView>(Resource.Id.aboutAppname)).SetText(Resource.String.AppName);
            ((TextView)FindViewById<TextView>(Resource.Id.aboutVersion)).SetText(Resource.String.Version);
            ((TextView)FindViewById<TextView>(Resource.Id.aboutText)).SetText(Resource.String.AboutBoxText);
            ((Button)FindViewById<Button>(Resource.Id.buttonOK)).Click += AboutButtonActivity_Click;
        }

        void AboutButtonActivity_Click(object sender, EventArgs e) => Finish();
    }
}
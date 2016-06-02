using System;
using Android.App;
using Android.OS;
using Android.Widget;

namespace AndroidZebraPrint
{
    [Activity(Label = "@+string/AboutBox", Theme = "@android:style/Theme.Dialog")]
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

        private void AboutButtonActivity_Click(object sender, EventArgs e)
        {
            Finish();
        }
    }
}
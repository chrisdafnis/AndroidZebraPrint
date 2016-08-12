using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AndroidZebraPrint
{
    [Activity(Label = "ProgressDialogActivity")]
    public class ProgressDialogActivity : Activity
    {
        private static ProgressDialog progress;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            progress = new ProgressDialog(this);
            progress.Indeterminate = true;
            progress.SetProgressStyle(ProgressDialogStyle.Spinner);

            progress.SetMessage("Contacting server. Please wait...");
            progress.SetCancelable(true);
            
        }

        public void Show()
        {
            progress.Show();
        }

        public void Hide()
        {
            progress.Hide();
        }
    }
}
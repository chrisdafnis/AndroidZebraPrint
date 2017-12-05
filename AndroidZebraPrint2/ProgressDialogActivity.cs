using Android.App;
using Android.OS;

namespace DakotaIntegratedSolutions
{
    [Activity(Label = "ProgressDialogActivity")]
    public class ProgressDialogActivity : Activity
    {
        static ProgressDialog progress;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            progress = new ProgressDialog(this);
            progress.Indeterminate = true;
            progress.SetProgressStyle(ProgressDialogStyle.Spinner);

            progress.SetMessage("Contacting server. Please wait...");
            progress.SetCancelable(true);
        }

        public void Show() => progress.Show();

        public void Hide() => progress.Hide();
    }
}
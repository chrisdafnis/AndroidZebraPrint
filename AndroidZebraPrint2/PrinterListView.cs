using Android.App;
using Android.OS;

namespace DakotaIntegratedSolutions
{
    public class PrinterListView : ListActivity
    {
        public void onCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(AndroidZebraPrint2.Resource.Layout.Main);
        }
    }
}
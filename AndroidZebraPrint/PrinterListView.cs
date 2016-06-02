
using Android.App;
using Android.OS;

namespace AndroidZebraPrint
{
    public class PrinterListView : ListActivity
    {

        public void onCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);
        }
    }
}
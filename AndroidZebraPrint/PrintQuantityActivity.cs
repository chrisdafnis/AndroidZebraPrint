using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace AndroidZebraPrint
{
    [Activity(Label = "@+string/PrintQuantity", Theme = "@android:style/Theme.Dialog")]
    class PrintQuantityActivity : Activity
    {
        Spinner spinQty;
        int quantity = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.PrintQuantity);
            Button btnOK = ((Button)FindViewById<Button>(Resource.Id.buttonOK));
            spinQty = FindViewById<Spinner>(Resource.Id.spinnerQty); ;
            spinQty.ItemSelected += SpinQty_ItemSelected;
            var adapter = ArrayAdapter.CreateFromResource(
                this, Resource.Array.quantity_array, Android.Resource.Layout.SimpleSpinnerItem);

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinQty.Adapter = adapter;
            btnOK.Click += BtnOK_Click;
        }

        private void SpinQty_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            quantity = Convert.ToInt32(spinQty.GetItemAtPosition(e.Position).ToString());
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Intent returnIntent = new Intent();
            returnIntent.PutExtra("quantity", quantity);
            SetResult(Result.Ok, returnIntent);
            Finish();
        }
    }
}
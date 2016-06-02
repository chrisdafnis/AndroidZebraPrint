using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AndroidZebraPrint
{
    [Activity(Label = "@+string/FindFiles", Theme = "@android:style/Theme.Dialog")]
    public class FindFilesActivity : Activity
    {
        ListView fileListView;
        IEnumerable<string> fileList;
        string selectedFile;
        IFileUtil fileUtility;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.FindFiles);

            fileListView = FindViewById<ListView>(Resource.Id.fileListView);
            fileListView.ItemClick += FileListView_ItemClick; ;
            // set up file utility for saving/loading settings
            fileUtility = new FileUtilImplementation();
            SearchForFiles();
        }

        private void SearchForFiles()
        {
            fileList = fileUtility.GetFileList();
            if (fileList.Count<string>() > 0)
            {
                string[] files = new string[fileList.Count<string>()];
                for (int i = 0; i < fileList.Count<string>(); i++)
                {
                    files[i] = fileList.ElementAt<string>(i);
                }
                try
                {
                    fileListView.Adapter = new ArrayAdapter(Android.App.Application.Context, Android.Resource.Layout.SimpleListItem1, files);
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

                dialogBuilder.SetTitle("File Not Found");
                dialogBuilder.SetMessage("No GLN Locations files found on SD card. Application will now exit.");
                dialogBuilder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                dialogBuilder.SetPositiveButton(Android.Resource.String.Ok, delegate
                {
                    this.Finish();
                });
                dialogBuilder.Show();
            }
        }

        private void FileListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            try
            {
                selectedFile = fileList.ElementAt<string>(e.Position);
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }

            Intent returnIntent = new Intent();
            returnIntent.PutExtra("filename", selectedFile);
            SetResult(Result.Ok, returnIntent);
            Finish();
        }
    }
}
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DakotaIntegratedSolutions
{
    [Activity(Label = "@+string/FindFiles", Theme = "@style/dialog_light")]
    public class FindFilesActivity : Activity
    {
        ListView fileListView;
        IEnumerable<string> fileList;
        string selectedFile;
        IFileUtil fileUtility;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(AndroidZebraPrint2.Resource.Layout.FindFiles);

            fileListView = FindViewById<ListView>(AndroidZebraPrint2.Resource.Id.fileListView);
            fileListView.ItemClick += FileListView_ItemClick; ;
            // set up file utility for saving/loading settings
            fileUtility = new FileUtilImplementation();
            // fileUtility.
            LogFile("log", "Searching for files", MethodBase.GetCurrentMethod().Name, 0, Class.SimpleName);
            SearchForFiles();
        }

        void SearchForFiles()
        {
            try
            {
                LogFile("log", "Searching for files", MethodBase.GetCurrentMethod().Name, 1, Class.SimpleName);
                fileList = fileUtility.GetFileList();
                if (fileList.Count<string>() > 0)
                {
                    string[] files = new string[fileList.Count<string>()];
                    for (int i = 0; i < fileList.Count<string>(); i++)
                        files[i] = fileList.ElementAt<string>(i);

                    try
                    {
                        fileListView.Adapter = new AlternateRowAdapter(Android.App.Application.Context, Android.Resource.Layout.SimpleListItem1, files);
                    }
                    catch (Exception ex1)
                    {
                        // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                        LogFile(ex1.Message, ex1.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex1), Class.SimpleName);
                    }
                }
                else
                {
                    LogFile("log", "No files found", MethodBase.GetCurrentMethod().Name, 2, Class.SimpleName);

                    var returnIntent = new Intent();
                    returnIntent.PutExtra("filename", "");
                    SetResult(Result.Canceled, returnIntent);
                    Finish();
                }
            }
            catch (Exception ex2)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex2.Message, ex2.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex2), Class.SimpleName);
            }
        }

        void FileListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            try
            {
                selectedFile = fileList.ElementAt<string>(e.Position);
            }
            catch (Exception ex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }

            var returnIntent = new Intent();
            returnIntent.PutExtra("filename", selectedFile);
            SetResult(Result.Ok, returnIntent);
            Finish();
        }

        public void LogFile(string sExceptionName, string sEventName, string sControlName, int nErrorLineNo, string sFormName)
        {
            StreamWriter log;
            var today = DateTime.Now;
            var filename = string.Format("{0}/{1:ddMMyyyy}.log",
                "/mnt/ext_sdcard", today);

            if (!File.Exists(filename))
            {
                log = new StreamWriter(filename);
            }
            else
            {
                log = File.AppendText(filename);
            }

            // Write to the file:
            log.WriteLine(string.Format("{0}{1}", "-----------------------------------------------", System.Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Log Time: ", DateTime.Now, System.Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Exception Name: ", sExceptionName, System.Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Event Name: ", sEventName, System.Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Control Name: ", sControlName, System.Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Error Line No.: ", nErrorLineNo, System.Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Form Name: ", sFormName, System.Environment.NewLine));

            // Close the stream:
            log.Close();
        }
    }
}
using Android.App;
using System.Collections.Generic;

namespace DakotaIntegratedSolutions
{
    public interface IFileUtil
    {
        void SaveXMLSettings(object printer);
        object LoadXMLSettings();
        object LoadGLNFile(string filename);
        IEnumerable<string> GetFileList();
        void LogFile(string sExceptionName, string sEventName, string sControlName, int nErrorLineNo, string sFormName);
        void SaveLocation(string filename, IGLNLocation iGLNLocation);
    }
}

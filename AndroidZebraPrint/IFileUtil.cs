using System.Collections.Generic;

namespace AndroidZebraPrint
{
    public interface IFileUtil
    {
        void SaveXMLSettings(object printer);
        object LoadXMLSettings();
        object LoadXMLGLNFile(string filename);
        IEnumerable<string> GetFileList();
        void LogFile(string sExceptionName, string sEventName, string sControlName, int nErrorLineNo, string sFormName);
    }
}

using System.Collections.Generic;
using static DakotaIntegratedSolutions.FileUtilImplementation;

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
        CSVFileFormat FileFormat { get; set; }
    }
}

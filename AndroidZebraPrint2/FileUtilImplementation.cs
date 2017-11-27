using System.IO;
using System.Xml;
using System.Xml.Linq;
using System;
using Zebra.Sdk.Printer.Discovery;
using System.Xml.Schema;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Net;
using System.Text;
using Excel;
using System.Data;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using System.Diagnostics;
using Android.App;

namespace DakotaIntegratedSolutions
{
    public class FileUtilImplementation : IFileUtil
    {
        protected DiscoveredPrinterBluetooth savedPrinter = null;
        public enum FILE_EXTENSIONS { csv = 0, xlsx };
        public DiscoveredPrinterBluetooth SavedPrinter { get { return savedPrinter; } set { savedPrinter = value; } }
        public enum CSVFileFormat { PLYMOUTH=0, CORNWALL, NORTHTEES, UNKNOWN };
        public CSVFileFormat FileFormat { get; set; }

        public void SaveXMLSettings(object printer)
        {
            try
            {
                string localFilename = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "config.xml");
                IZebraPrinter discoveredPrinter = (IZebraPrinter)printer;

                XDocument xDoc = XDocument.Parse("<AppConfig><SavedPrinterConfig><FriendlyName></FriendlyName><MACAddress></MACAddress></SavedPrinterConfig></AppConfig>");

                XElement xRoot = xDoc.Root;
                foreach (XElement xElem in xRoot.Elements())
                {
                    foreach (XElement xChild in xElem.Elements())
                    {
                        if (xChild.Name == "FriendlyName")
                            xChild.Value = discoveredPrinter.FriendlyName;
                        if (xChild.Name == "MACAddress")
                            xChild.Value = discoveredPrinter.MACAddress;
                    }
                }
                xDoc.Save(localFilename);
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
        }

        public object LoadXMLSettings()
        {
            try
            {
                string localFilename = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "config.xml");
                if (!File.Exists(localFilename))
                {
                    LogFile("First run", "First run", MethodBase.GetCurrentMethod().Name, 0, GetType().Name);
                    IZebraPrinter dummyprinter = new ZebraPrinter("", "");
                    SaveXMLSettings(dummyprinter);
                }

                XDocument xDoc = XDocument.Load(localFilename);
                XElement xRoot = xDoc.Root;
                string friendlyName = "";
                string macAddress = "";
                foreach (XElement xElem in xRoot.Elements())
                {
                    foreach (XElement xChild in xElem.Elements())
                    {
                        if (xChild.Name == "FriendlyName")
                            friendlyName = xChild.Value;
                        if (xChild.Name == "MACAddress")
                            macAddress = xChild.Value;
                    }
                }
                IZebraPrinter printer = new ZebraPrinter(macAddress, friendlyName);
                return printer;
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
                return null;
            }
        }

        public static string Directorypath
        {
            get
            {
                string documentsPath = "/mnt/ext_sdcard";
#if DEBUG
                //documentsPath = "/mnt/sdcard";
#else
                documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
#endif
                return documentsPath;
            }
        }

        private XDocument TransformCSVToXML(string filename)
        {
            XDocument xDoc = null;
            try
            {
                FileFormat = CSVFileFormat.UNKNOWN;
                string[] csv = File.ReadAllLines(filename);
                // extract blank rows (if any)
                csv = csv.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                // remove header (if present)
                var header = new Regex("GLN Creation Date");
                csv = csv.Where(x => !header.IsMatch(x)).ToArray();
                int fieldCount = Regex.Split(csv[0], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").Count<string>();

                if (csv[0].Contains("Plymouth Hospitals NHS Trust"))
                {
                    if (fieldCount != 10)
                    {
                        throw new ApplicationException("File error: the file is not in the required format");
                    }
                    else
                    {
                        FileFormat = CSVFileFormat.PLYMOUTH;
                    }
                }
                else if (csv[0].Contains("ROYAL CORNWALL HOSPITALS NHS TRUST"))
                {
                    if (fieldCount != 9)
                    {
                        throw new ApplicationException("File error: the file is not in the required format");
                    }
                    else
                    {
                        FileFormat = CSVFileFormat.CORNWALL;
                    }
                }
                else if (csv[0].Contains("Nth Tees & Hartlepool NHSTrust"))
                {
                    if (fieldCount != 9)
                    {
                        throw new ApplicationException("File error: the file is not in the required format");
                    }
                    else
                    {
                        FileFormat = CSVFileFormat.NORTHTEES;
                    }
                }
                else
                {
                    FileFormat = CSVFileFormat.UNKNOWN;
                }

                StringBuilder builder = new StringBuilder();
                foreach (string row in csv)
                {
                    string[] fields = Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    string temp = String.Empty;

                    // clean up the fields (remove " and leading spaces)
                    for (int i = 0; i < fields.Length; i++)
                    {
                        fields[i] = fields[i].TrimStart(' ', '"');
                        fields[i] = fields[i].TrimEnd('"');
                        if (fields[i].Contains(","))
                            fields[i] = fields[i].Replace(","," ");
                        temp += fields[i] + ",";
                    }

                    // remove unnecessary spaces from the end of the string
                    while (temp.EndsWith(" "))
                    {
                        temp = temp.Remove(temp.LastIndexOf(' '), 1);
                    }

                    // remove unnecessary commas from the end of the string
                    while (temp.EndsWith(","))
                    {
                        temp = temp.Remove(temp.LastIndexOf(','), 1);
                    }

                    // add the printed true/false flag to each now
                    if (!temp.EndsWith("True", StringComparison.CurrentCultureIgnoreCase) &&
                        !temp.EndsWith("False", StringComparison.CurrentCultureIgnoreCase))
                    {
                        temp = temp + ",False\r\n";
                    }
                    builder.AppendLine(temp);
                }
                
                StreamWriter writer = new StreamWriter(filename);
                writer.Write(builder);
                writer.Close();

                switch (FileFormat)
                {
                    case CSVFileFormat.CORNWALL:
                        {

                            XElement location = new XElement("Root",
                                from str in builder.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                let fields = str.Split(',')

                                select new XElement("GLNLocation",
                                    new XElement("Region", fields[0]),
                                    new XElement("Site", fields[1]),
                                    new XElement("Building", fields[2]),
                                    new XElement("Floor", fields[3]),
                                    new XElement("Room", fields[4]),
                                    new XElement("Code", fields[5]),
                                    new XElement("GLN", fields[6]),
                                    new XElement("GLNCreationDate", fields[7]),
                                    new XElement("Printed", fields[8])));

                            XmlSchemaSet schemaSet = AddXMLSchema();
                            xDoc = XDocument.Parse(location.ToString());
                            if (xDoc == null | xDoc.Root == null)
                            {
                                throw new ApplicationException("xml error: the referenced stream is not xml.");
                            }

                            xDoc.Validate(schemaSet, (o, e) =>
                            {
                                throw new ApplicationException("xsd validation error: xml file has structural problems");
                            });
                        }
                        break;
                    case CSVFileFormat.NORTHTEES:
                        {
                            XElement location = new XElement("Root",
                                from str in builder.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                let fields = str.Split(',')

                                // Region, Site, Building, Floor, Room, Code, GLN
                                select new XElement("GLNLocation",
                                    new XElement("Region", fields[0]),
                                    new XElement("Site", fields[1]),
                                    new XElement("Building", fields[2]),
                                    new XElement("Floor", fields[3]),
                                    new XElement("Room", fields[4]),
                                    new XElement("Code", fields[5]),
                                    new XElement("GLN", fields[6]),
                                    new XElement("GLNCreationDate", DateTime.Now),
                                    new XElement("Printed", fields[7])));

                            XmlSchemaSet schemaSet = AddXMLSchema();
                            xDoc = XDocument.Parse(location.ToString());
                            if (xDoc == null | xDoc.Root == null)
                            {
                                throw new ApplicationException("xml error: the referenced stream is not xml.");
                            }

                            xDoc.Validate(schemaSet, (o, e) =>
                            {
                                throw new ApplicationException("xsd validation error: xml file has structural problems");
                            });
                        }
                        break;
                    case CSVFileFormat.PLYMOUTH:
                        {
                            XElement location = new XElement("Root",
                                from str in builder.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                let fields = str.Split(',')

                                // Region, Site, Building, Floor, Room, Code, GLN
                                select new XElement("GLNLocation",
                                    new XElement("Region", fields[0]),
                                    new XElement("Site", fields[1]),
                                    new XElement("Building", fields[2]),
                                    new XElement("Floor", fields[3]),
                                    new XElement("Room", fields[4]),
                                    new XElement("Code", fields[5]),
                                    new XElement("GLN", fields[6]),
                                    new XElement("GLNCreationDate", DateTime.Now),
                                    new XElement("FreeText", fields[8]),
                                    new XElement("Printed", fields[9])));

                            XmlSchemaSet schemaSet = AddNewXMLSchema();
                            xDoc = XDocument.Parse(location.ToString());
                            if (xDoc == null | xDoc.Root == null)
                            {
                                throw new ApplicationException("xml error: the referenced stream is not xml.");
                            }

                            xDoc.Validate(schemaSet, (o, e) =>
                            {
                                throw new ApplicationException("xsd validation error: xml file has structural problems");
                            });
                        }
                        break;
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
            catch (ApplicationException ex)
            {
                xDoc = null;
            }
            return xDoc;
        }

        private XDocument TransformXLSToXML(string filename)
        {
            XDocument xDoc = null;
            try
            {
                FileStream stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
                IExcelDataReader excelReader;

                excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                //excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                stream.Close();

                excelReader.IsFirstRowAsColumnNames = true;
                DataSet dataset = excelReader.AsDataSet();
                StringBuilder builder = new StringBuilder();

                List<object> tableCollection = new List<object>();
                foreach (DataTable dt in dataset.Tables)
                {
                    tableCollection.Add(dt);
                }

                DataTable table = dataset.Tables[0];
                bool hasPrintCol = true;
                //if (table.Columns.Count == 13)
                if (table.Columns.Count == 8)
                {
                    table.Columns.Add("Printed", typeof(string), "False");
                    hasPrintCol = false;
                }

                foreach (DataRow row in dataset.Tables[0].Rows)
                {
                    string temp = String.Empty;

                    if (row.ItemArray[1].ToString() != "Region")
                    {
                        GLNLocation glnlocation = new GLNLocation()
                        {
                            Region = row.ItemArray[0].ToString(),
                            Site = row.ItemArray[1].ToString(),
                            Building = row.ItemArray[2].ToString(),
                            Floor = row.ItemArray[3].ToString(),
                            Room = row.ItemArray[4].ToString(),
                            Code = row.ItemArray[5].ToString(),
                            GLN = row.ItemArray[6].ToString(),
                            Date = Convert.ToDateTime(row.ItemArray[7])
                        };
                        if (hasPrintCol)
                            glnlocation.Printed = Convert.ToBoolean(row.ItemArray[8]);

                        //glnlocation.Region = row.ItemArray[1].ToString();
                        //glnlocation.Site = row.ItemArray[3].ToString();
                        //glnlocation.Building = row.ItemArray[5].ToString();
                        //glnlocation.Floor = row.ItemArray[7].ToString();
                        //glnlocation.Room = row.ItemArray[9].ToString();
                        //glnlocation.Code = row.ItemArray[10].ToString();
                        //glnlocation.GLN = row.ItemArray[11].ToString();
                        //glnlocation.Date = Convert.ToDateTime(row.ItemArray[12]);
                        //if (hasPrintCol)
                        //    glnlocation.Printed = Convert.ToBoolean(row.ItemArray[13]);

                        // clean up site for commas etc
                        glnlocation.Site = glnlocation.Site.TrimStart(' ', '"');
                        glnlocation.Site = glnlocation.Site.TrimEnd('"');
                        glnlocation.Site = glnlocation.Site.Replace(",", " ");

                        temp = glnlocation.Value();

                        // remove unnecessary spaces from the end of the string
                        while (temp.EndsWith(" "))
                        {
                            temp = temp.Remove(temp.LastIndexOf(' '), 1);
                        }

                        // remove unnecessary commas from the end of the string
                        while (temp.EndsWith(","))
                        {
                            temp = temp.Remove(temp.LastIndexOf(','), 1);
                        }

                        // add the printed true/false flag to each now
                        if (!temp.EndsWith("True", StringComparison.CurrentCultureIgnoreCase) &&
                            !temp.EndsWith("False", StringComparison.CurrentCultureIgnoreCase))
                        {
                            temp = temp + ",False\r\n";
                        }
                        builder.AppendLine(temp);
                    }
                    else
                    {
                        try
                        {
                            row["Printed"] = "Printed";
                        }
                        catch (Exception ex)
                        {
                            //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                            LogFile(ex.Message+" :1", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
                        }
                    }
                }
                dataset.Tables.Clear();
                dataset.Tables.Add(table);
                tableCollection.Remove(table);
                foreach (DataTable dt in tableCollection)
                {
                    dataset.Tables.Add(dt);
                }

                XElement location = new XElement("Root",
                    from str in builder.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                    let fields = str.Split(',')

                    select new XElement("GLNLocation",
                        new XElement("Region", fields[0]),
                        new XElement("Site", fields[1]),
                        new XElement("Building", fields[2]),
                        new XElement("Floor", fields[3]),
                        new XElement("Room", fields[4]),
                        new XElement("Code", fields[5]),
                        new XElement("GLN", fields[6]),
                        new XElement("GLNCreationDate", fields[7]),
                        new XElement("Printed", fields[8])));

                XmlSchemaSet schemaSet = AddXMLSchema();
                xDoc = XDocument.Parse(location.ToString());
                if (xDoc == null | xDoc.Root == null)
                {
                    throw new ApplicationException("xml error: the referenced stream is not xml.");
                }

                xDoc.Validate(schemaSet, (o, e) =>
                {
                    throw new ApplicationException("xsd validation error: xml file has structural problems");
                });

                try
                {
                    if (File.Exists(filename))
                        File.Delete(filename);

                    stream = File.Open(filename, FileMode.CreateNew, FileAccess.ReadWrite);
                    using (SpreadsheetDocument document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                    {
                        ExcelFunctions.WriteExcelFile(dataset, document);
                    }
                    stream.Close();
                }
                catch (Exception ex)
                {
                    //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                    LogFile(ex.Message + " :2", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message + " :3", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
            return xDoc;
        }

        private string TransformXMLToCSV(XDocument document)
        {
            return "";
        }

        public IEnumerable<string> GetFileList()
        {
            var directory = new DirectoryInfo(Directorypath);
            var masks = new[] { "*.csv"/*, "*.xlsx"*/ };
            var files = masks.SelectMany(directory.EnumerateFiles);
            List<string> fileList = new List<string>();
            foreach (FileInfo file in files)
            {
                fileList.Add(file.DirectoryName + "/" + file.Name);
            }
            return fileList;
        }

        public object LoadGLNFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    if (filename.Contains(".csv"))
                    {
                        XDocument doc = new XDocument();
                        doc = TransformCSVToXML(filename);
                        return doc;
                    }
                    else if (filename.Contains(".xlsx"))
                    {
                        XDocument doc = new XDocument();
                        doc = TransformXLSToXML(filename);
                        return doc;
                    }
                }
            }
            catch (IndexOutOfRangeException oorex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(oorex.Message + " :4", oorex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(oorex), GetType().Name);
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message + " :5", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
            return null;
        }

        public void SaveLocation(string filename, IGLNLocation iGLNLocation)
        {
            if (filename.Contains(".csv"))
            {
                SaveCSVLocation(filename, iGLNLocation);
            }
            else if (filename.Contains(".xlsx"))
            {
                SaveXLSLocation(filename, iGLNLocation);
            }
        }

        private void SaveXLSLocation(string filename, IGLNLocation iGLNLocation)
        {
            FileStream stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
            IExcelDataReader excelReader;

            if (filename.Contains(".xlsx"))
            {
                excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            }
            else
            {
                excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
            }
            stream.Close();

            excelReader.IsFirstRowAsColumnNames = true;
            DataSet dataset = excelReader.AsDataSet();

            DataRowCollection rowCollection = dataset.Tables[0].Rows;

            foreach (DataRow row in rowCollection)
            {
                if (row[11].ToString().Contains(iGLNLocation.GLN))
                {
                    string printed = (string)row[13];
                    printed = Regex.Replace(printed, "False", "True", RegexOptions.IgnoreCase);
                    row[13] = printed;

                    break;
                }
            }

            try
            {
                if (File.Exists(filename))
                    File.Delete(filename);

                stream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite);
                using (SpreadsheetDocument document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    ExcelFunctions.WriteExcelFile(dataset, document);
                }
                stream.Close();
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message + " :6", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
        }

        private void SaveCSVLocation(string filename, IGLNLocation iGLNLocation)
        { 
            StreamReader reader = new StreamReader(filename);
            string content = reader.ReadToEnd();
            reader.Close();
            char[] splitParams = new char[] { '\r', '\n' };
            string[] rows = content.Split(splitParams, StringSplitOptions.RemoveEmptyEntries);

            String newRow = String.Empty;
            String oldRow = String.Empty;
            foreach (String row in rows)
            {
                if (row.Contains(iGLNLocation.GLN))
                {
                    oldRow = row;
                    newRow = Regex.Replace(row, "False", "True", RegexOptions.IgnoreCase);
                    break;
                }
            }

            if (newRow != String.Empty && oldRow != String.Empty)
            {
                content = Regex.Replace(content, oldRow, newRow);
            }
            
            StreamWriter writer = new StreamWriter(filename);
            writer.Write(content);
            writer.Close();
        }

        public object SaveGLNFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    XDocument xDoc = TransformCSVToXML(filename);
                    return xDoc;
                }
            }
            catch (IndexOutOfRangeException oorex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(oorex.Message + " :7", oorex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(oorex), GetType().Name);
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message + " :8", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
            return null;
        }

        private XmlSchemaSet AddXMLSchema()
        {
            string xsdMarkup =
                @"<?xml version='1.0' encoding='utf-8'?>
                    <xsd:schema attributeFormDefault='unqualified' elementFormDefault='qualified' targetNamespace='http://www.contoso.com/books' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                        <xsd:element nillable='true' name='GLNData'>
		                    <xsd:complexType>
			                    <xsd:sequence minOccurs='0'>
				                    <xsd:element minOccurs='0' maxOccurs='unbounded' nillable='true' name='GLNLocation' form='unqualified'>
					                    <xsd:complexType>
						                    <xsd:sequence minOccurs='0'>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Region' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Site' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Building' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Floor' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Room' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Code' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='GLN' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:date' name='GLNCreationDate' form='unqualified'></xsd:element>
						                    </xsd:sequence>
					                    </xsd:complexType>
				                    </xsd:element>
			                    </xsd:sequence>
		                    </xsd:complexType>
	                    </xsd:element>
                    </xsd:schema>";
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add("http://www.contoso.com/books", XmlReader.Create(new StringReader(xsdMarkup)));
            return schemas;
        }

        private XmlSchemaSet AddNewXMLSchema()
        {
            string xsdMarkup =
                @"<?xml version='1.0' encoding='utf-8'?>
                    <xsd:schema attributeFormDefault='unqualified' elementFormDefault='qualified' targetNamespace='http://www.contoso.com/books' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                        <xsd:element nillable='true' name='GLNData'>
		                    <xsd:complexType>
			                    <xsd:sequence minOccurs='0'>
				                    <xsd:element minOccurs='0' maxOccurs='unbounded' nillable='true' name='GLNLocation' form='unqualified'>
					                    <xsd:complexType>
						                    <xsd:sequence minOccurs='0'>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Region' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Site' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Building' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Floor' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Room' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='Code' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='GLN' form='unqualified'></xsd:element>
                                                <xsd:element minOccurs='0' nillable='true' type='xsd:string' name='FreeText' form='unqualified'></xsd:element>
							                    <xsd:element minOccurs='0' nillable='true' type='xsd:date' name='GLNCreationDate' form='unqualified'></xsd:element>
						                    </xsd:sequence>
					                    </xsd:complexType>
				                    </xsd:element>
			                    </xsd:sequence>
		                    </xsd:complexType>
	                    </xsd:element>
                    </xsd:schema>";
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add("http://www.contoso.com/books", XmlReader.Create(new StringReader(xsdMarkup)));
            return schemas;
        }

        public void LogFile(string sExceptionName, string sEventName, string sControlName, int nErrorLineNo, string sFormName)
        {
            StreamWriter log;
            DateTime today = DateTime.Now;
            String filename = String.Format("{0}/{1:ddMMyyyy}.log", 
                Directorypath, today);

            if (!File.Exists(filename))
            {
                log = new StreamWriter(filename);
            }
            else
            {
                log = File.AppendText(filename);
            }

            // Write to the file:
            log.WriteLine(String.Format("{0}{1}", "-----------------------------------------------", Environment.NewLine));
            log.WriteLine(String.Format("{0}{1}{2}", "Log Time: ", DateTime.Now, Environment.NewLine));
            log.WriteLine(String.Format("{0}{1}{2}", "Exception Name: ", sExceptionName, Environment.NewLine));
            log.WriteLine(String.Format("{0}{1}{2}", "Event Name: ", sEventName, Environment.NewLine));
            log.WriteLine(String.Format("{0}{1}{2}", "Control Name: ", sControlName, Environment.NewLine));
            log.WriteLine(String.Format("{0}{1}{2}", "Error Line No.: ", nErrorLineNo, Environment.NewLine));
            log.WriteLine(String.Format("{0}{1}{2}", "Form Name: ", sFormName, Environment.NewLine));

            // Close the stream:
            log.Close();
        }

        //Read A File From Server
        public void ReadFileFromFTP(String FTP, String Local)
        {
            //Create A 2MB Cache Buffer
            byte[] Buffer = new byte[2048];
            int FileLenght = 0;

            //Create An FTP Client Request
            FtpWebRequest Request = (FtpWebRequest)WebRequest.Create(FTP);
            Request.Method = WebRequestMethods.Ftp.DownloadFile;

            //Use A Login Credential
            Request.Credentials = new NetworkCredential("USER", "PASS");

            //Receive An Answer From Server
            FtpWebResponse Response = (FtpWebResponse)Request.GetResponse();
            Stream ResponseStream = Response.GetResponseStream();

            //Write The File To The SDCARD
            StreamWriter Output = new StreamWriter(Local, false);

            //Store On Buffer And Write TO SDCARD
            while ((FileLenght = ResponseStream.Read(Buffer, 0, Buffer.Length)) > 0)
            {
                for (int i = 0; i < FileLenght; i++)
                {
                    Output.Write((char)Buffer[i]);
                }
            }

            //Close File Stream Writer
            Output.Close();

            //Close Connection Request
            Response.Close();
        }
    }
}
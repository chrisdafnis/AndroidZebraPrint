using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Zebra.Sdk.Printer.Discovery;

namespace DakotaIntegratedSolutions
{
    public class FileUtilImplementation : IFileUtil
    {
        protected DiscoveredPrinterBluetooth savedPrinter;
        public enum FILE_EXTENSIONS { csv = 0, xlsx };
        public DiscoveredPrinterBluetooth SavedPrinter { get { return savedPrinter; } set { savedPrinter = value; } }
        public enum CSVFileFormat { PLYMOUTH = 0, CORNWALL, NORTHTEES, UNKNOWN };
        public CSVFileFormat FileFormat { get; set; }

        public void SaveXMLSettings(object printer)
        {
            try
            {
                var localFilename = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "config.xml");
                var discoveredPrinter = (IZebraPrinter)printer;

                var xDoc = XDocument.Parse("<AppConfig><SavedPrinterConfig><FriendlyName></FriendlyName><MACAddress></MACAddress></SavedPrinterConfig></AppConfig>");

                var xRoot = xDoc.Root;
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
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
        }

        public object LoadXMLSettings()
        {
            try
            {
                var localFilename = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "config.xml");
                if (!File.Exists(localFilename))
                {
                    LogFile("First run", "First run", MethodBase.GetCurrentMethod().Name, 0, GetType().Name);
                    IZebraPrinter dummyprinter = new ZebraPrinter("", "");
                    SaveXMLSettings(dummyprinter);
                }

                var xDoc = XDocument.Load(localFilename);
                var xRoot = xDoc.Root;
                var friendlyName = "";
                var macAddress = "";
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
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
                return null;
            }
        }

        public static string Directorypath
        {
            get
            {
                var documentsPath = "/mnt/ext_sdcard";
#if DEBUG
                //documentsPath = "/mnt/sdcard";
#else
                // documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
#endif
                return documentsPath;
            }
        }

        XDocument TransformCSVToXML(string filename)
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
                var fieldCount = Regex.Split(csv[0], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").Count<string>();

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

                var builder = new StringBuilder();
                foreach (string row in csv)
                {
                    string[] fields = Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    var temp = string.Empty;

                    // clean up the fields (remove " and leading spaces)
                    for (int i = 0; i < fields.Length; i++)
                    {
                        fields[i] = fields[i].TrimStart(' ', '"');
                        fields[i] = fields[i].TrimEnd('"');
                        if (fields[i].Contains(","))
                            fields[i] = fields[i].Replace(",", " ");
                        temp += fields[i] + ",";
                    }

                    // remove unnecessary spaces from the end of the string
                    while (temp.EndsWith(" "))
                        temp = temp.Remove(temp.LastIndexOf(' '), 1);

                    // remove unnecessary commas from the end of the string
                    while (temp.EndsWith(","))
                        temp = temp.Remove(temp.LastIndexOf(','), 1);

                    // add the printed true/false flag to each now
                    if (!temp.EndsWith("True", StringComparison.CurrentCultureIgnoreCase) &&
                        !temp.EndsWith("False", StringComparison.CurrentCultureIgnoreCase))
                    {
                        temp = temp + ",False\r\n";
                    }

                    builder.AppendLine(temp);
                }

                var writer = new StreamWriter(filename);
                writer.Write(builder);
                writer.Close();

                switch (FileFormat)
                {
                    case CSVFileFormat.CORNWALL:
                        {
                            var location = new XElement("Root",
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

                            var schemaSet = AddXMLSchema();
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
                            var location = new XElement("Root",
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

                            var schemaSet = AddXMLSchema();
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
                            var location = new XElement("Root",
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

                            var schemaSet = AddNewXMLSchema();
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
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
            catch (ApplicationException ex)
            {
                xDoc = null;
            }

            return xDoc;
        }

        XDocument TransformXLSToXML(string filename)
        {
            XDocument xDoc = null;
            try
            {
                var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
                IExcelDataReader excelReader;

                excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                // excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                stream.Close();

                excelReader.IsFirstRowAsColumnNames = true;
                var dataset = excelReader.AsDataSet();
                var builder = new StringBuilder();

                List<object> tableCollection = new List<object>();
                foreach (DataTable dt in dataset.Tables)
                    tableCollection.Add(dt);

                var table = dataset.Tables[0];
                var hasPrintCol = true;
                // if (table.Columns.Count == 13)
                if (table.Columns.Count == 8)
                {
                    table.Columns.Add("Printed", typeof(string), "False");
                    hasPrintCol = false;
                }

                foreach (DataRow row in dataset.Tables[0].Rows)
                {
                    var temp = string.Empty;

                    if (row.ItemArray[1].ToString() != "Region")
                    {
                        var glnlocation = new GLNLocation()
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

                        // glnlocation.Region = row.ItemArray[1].ToString();
                        // glnlocation.Site = row.ItemArray[3].ToString();
                        // glnlocation.Building = row.ItemArray[5].ToString();
                        // glnlocation.Floor = row.ItemArray[7].ToString();
                        // glnlocation.Room = row.ItemArray[9].ToString();
                        // glnlocation.Code = row.ItemArray[10].ToString();
                        // glnlocation.GLN = row.ItemArray[11].ToString();
                        // glnlocation.Date = Convert.ToDateTime(row.ItemArray[12]);
                        // if (hasPrintCol)
                        //    glnlocation.Printed = Convert.ToBoolean(row.ItemArray[13]);

                        // clean up site for commas etc
                        glnlocation.Site = glnlocation.Site.TrimStart(' ', '"');
                        glnlocation.Site = glnlocation.Site.TrimEnd('"');
                        glnlocation.Site = glnlocation.Site.Replace(",", " ");

                        temp = glnlocation.Value();

                        // remove unnecessary spaces from the end of the string
                        while (temp.EndsWith(" "))
                            temp = temp.Remove(temp.LastIndexOf(' '), 1);

                        // remove unnecessary commas from the end of the string
                        while (temp.EndsWith(","))
                            temp = temp.Remove(temp.LastIndexOf(','), 1);

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
                            // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                            LogFile(ex.Message + " :1", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
                        }
                    }
                }

                dataset.Tables.Clear();
                dataset.Tables.Add(table);
                tableCollection.Remove(table);
                foreach (DataTable dt in tableCollection)
                    dataset.Tables.Add(dt);

                var location = new XElement("Root",
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

                var schemaSet = AddXMLSchema();
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
                        ExcelFunctions.WriteExcelFile(dataset, document);

                    stream.Close();
                }
                catch (Exception ex)
                {
                    // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                    LogFile(ex.Message + " :2", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message + " :3", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }

            return xDoc;
        }

        string TransformXMLToCSV(XDocument document) => "";

        public IEnumerable<string> GetFileList()
        {
            LogFile("Log", "Building File List", MethodBase.GetCurrentMethod().Name, 0, GetType().Name);
            var directory = new DirectoryInfo(Directorypath);
            var masks = new[] { "*.csv"/*, "*.xlsx"*/ };
            var files = masks.SelectMany(directory.EnumerateFiles);
            List<string> fileList = new List<string>();
            foreach (FileInfo file in files)
                fileList.Add(file.DirectoryName + "/" + file.Name);

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
                        var doc = new XDocument();
                        doc = TransformCSVToXML(filename);
                        return doc;
                    }
                    else if (filename.Contains(".xlsx"))
                    {
                        var doc = new XDocument();
                        doc = TransformXLSToXML(filename);
                        return doc;
                    }
                }
            }
            catch (IndexOutOfRangeException oorex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(oorex.Message + " :4", oorex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(oorex), GetType().Name);
            }
            catch (Exception ex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
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

        void SaveXLSLocation(string filename, IGLNLocation iGLNLocation)
        {
            var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
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
            var dataset = excelReader.AsDataSet();

            var rowCollection = dataset.Tables[0].Rows;

            foreach (DataRow row in rowCollection)
            {
                if (row[11].ToString().Contains(iGLNLocation.GLN))
                {
                    var printed = (string)row[13];
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
                    ExcelFunctions.WriteExcelFile(dataset, document);

                stream.Close();
            }
            catch (Exception ex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message + " :6", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
        }

        void SaveCSVLocation(string filename, IGLNLocation iGLNLocation)
        {
            var reader = new StreamReader(filename);
            var content = reader.ReadToEnd();
            reader.Close();
            char[] splitParams = new char[] { '\r', '\n' };
            string[] rows = content.Split(splitParams, StringSplitOptions.RemoveEmptyEntries);

            var newRow = string.Empty;
            var oldRow = string.Empty;
            foreach (string row in rows)
            {
                if (row.Contains(iGLNLocation.GLN))
                {
                    oldRow = row;
                    newRow = Regex.Replace(row, "False", "True", RegexOptions.IgnoreCase);
                    break;
                }
            }

            if (newRow != string.Empty && oldRow != string.Empty)
            {
                content = Regex.Replace(content, oldRow, newRow);
            }

            var writer = new StreamWriter(filename);
            writer.Write(content);
            writer.Close();
        }

        public object SaveGLNFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    var xDoc = TransformCSVToXML(filename);
                    return xDoc;
                }
            }
            catch (IndexOutOfRangeException oorex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(oorex.Message + " :7", oorex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(oorex), GetType().Name);
            }
            catch (Exception ex)
            {
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message + " :8", ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }

            return null;
        }

        XmlSchemaSet AddXMLSchema()
        {
            var xsdMarkup =
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
            var schemas = new XmlSchemaSet();
            schemas.Add("http://www.contoso.com/books", XmlReader.Create(new StringReader(xsdMarkup)));
            return schemas;
        }

        XmlSchemaSet AddNewXMLSchema()
        {
            var xsdMarkup =
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
            var schemas = new XmlSchemaSet();
            schemas.Add("http://www.contoso.com/books", XmlReader.Create(new StringReader(xsdMarkup)));
            return schemas;
        }

        public void LogFile(string sExceptionName, string sEventName, string sControlName, int nErrorLineNo, string sFormName)
        {
            StreamWriter log;
            var today = DateTime.Now;
            var filename = string.Format("{0}/{1:ddMMyyyy}.log",
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
            log.WriteLine(string.Format("{0}{1}", "-----------------------------------------------", Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Log Time: ", DateTime.Now, Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Exception Name: ", sExceptionName, Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Event Name: ", sEventName, Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Control Name: ", sControlName, Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Error Line No.: ", nErrorLineNo, Environment.NewLine));
            log.WriteLine(string.Format("{0}{1}{2}", "Form Name: ", sFormName, Environment.NewLine));

            // Close the stream:
            log.Close();
        }

        // Read A File From Server
        public void ReadFileFromFTP(string FTP, string Local)
        {
            // Create A 2MB Cache Buffer
            byte[] Buffer = new byte[2048];
            var FileLenght = 0;

            // Create An FTP Client Request
            var Request = (FtpWebRequest)WebRequest.Create(FTP);
            Request.Method = WebRequestMethods.Ftp.DownloadFile;

            // Use A Login Credential
            Request.Credentials = new NetworkCredential("USER", "PASS");

            // Receive An Answer From Server
            var Response = (FtpWebResponse)Request.GetResponse();
            var ResponseStream = Response.GetResponseStream();

            // Write The File To The SDCARD
            var Output = new StreamWriter(Local, false);

            // Store On Buffer And Write TO SDCARD
            while ((FileLenght = ResponseStream.Read(Buffer, 0, Buffer.Length)) > 0)
            {
                for (int i = 0; i < FileLenght; i++)
                    Output.Write((char)Buffer[i]);
            }

            // Close File Stream Writer
            Output.Close();

            // Close Connection Request
            Response.Close();
        }
    }
}
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

namespace AndroidZebraPrint
{
    public class FileUtilImplementation : IFileUtil
    {
        protected DiscoveredPrinterBluetooth savedPrinter = null;
        const string FILE_EXTENSION = "*.csv";
        public DiscoveredPrinterBluetooth SavedPrinter { get { return savedPrinter; } set { savedPrinter = value; } }

        public void SaveXMLSettings(object printer)
        {
            string localFilename = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "config.xml");
            IZebraPrinter discoveredPrinter = (IZebraPrinter)printer;

            XDocument xDoc = XDocument.Parse("<AppConfig><SavedPrinterConfig><FriendlyName></FriendlyName><MACAddress>AC:3F:A4:13:38:CF</MACAddress></SavedPrinterConfig></AppConfig>");
            try
            {
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
            string localFilename = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "config.xml");
            try
            {
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
                return "/mnt/ext_sdcard";
            }
        }

        private XDocument TransformCSVToXml(string filename)
        {
            XDocument xDoc = null;
            try
            {
                string[] csv = File.ReadAllLines(filename);
                // extract blank rows (if any)
                csv = csv.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                // remove header (if present)
                var header = new Regex("GLN Creation Date");
                csv = csv.Where(x => !header.IsMatch(x)).ToArray();

                XElement location = new XElement("Root",
                    from str in csv
                    let fields = str.Split(',')
                    select new XElement("GLNLocation",
                        new XElement("Region", fields[0]),
                        new XElement("Site", fields[1]),
                        new XElement("Building", fields[2]),
                        new XElement("Floor", fields[3]),
                        new XElement("Room", fields[4]),
                        new XElement("Code", fields[5]),
                        new XElement("GLN", fields[6]),
                        new XElement("GLNCreationDate", fields[7])));

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
            catch (IndexOutOfRangeException ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
            }
            return xDoc;
        }

        public IEnumerable<string> GetFileList()
        {
            return Directory.EnumerateFiles(Directorypath, FILE_EXTENSION, SearchOption.TopDirectoryOnly);
        }

        public object LoadXMLGLNFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    XDocument xDoc = TransformCSVToXml(filename);
                    return xDoc;
                }
            }
            catch (IndexOutOfRangeException oorex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(oorex.Message, oorex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(oorex), GetType().Name);
            }
            catch (Exception ex)
            {
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), GetType().Name);
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
    }
}
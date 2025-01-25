using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Extensions;
using System;
using System.IO;
using System.Xml;

namespace Redbox.HAL.Configuration
{
    internal sealed class RedboxConfigurationFile : IConfigurationFile
    {
        public SystemConfigurations Type => SystemConfigurations.Redbox;

        public string Path => "c:\\program files\\Redbox\\Halservice\\bin";

        public string FileName => "hal.xml";

        public string FullSourcePath { get; private set; }

        public void ImportFrom(IConfigurationFile config, ErrorList errors)
        {
            throw new NotImplementedException();
        }

        public ConversionResult ConvertTo(KioskConfiguration newconfig, ErrorList errors)
        {
            if (!File.Exists(this.FullSourcePath))
                return ConversionResult.InvalidFile;
            XmlDocument xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.Load(this.FullSourcePath);
            }
            catch (Exception ex)
            {
                errors.Add(Error.NewError("C001", "Invalid XML file", ex.Message));
                return ConversionResult.InvalidFile;
            }
            XmlNode xmlNode1 = xmlDocument.DocumentElement.SelectSingleNode("Controller/Decks");
            XmlNode lastChild = xmlNode1.LastChild;
            if (lastChild.GetAttributeValue<bool>("IsQlm"))
            {
                XmlNode xmlNode2 = (XmlNode)null;
                foreach (XmlNode childNode in xmlNode1.ChildNodes)
                {
                    if (7 == childNode.GetAttributeValue<int>("Number"))
                    {
                        xmlNode2 = childNode;
                        break;
                    }
                }
                XmlNode xmlNode3 = xmlNode2.Clone();
                xmlNode3.SetAttributeValue<int>("Number", 8);
                xmlNode3.SetAttributeValue<int>("Offset", lastChild.GetAttributeValue<int>("Offset"));
                xmlNode1.ReplaceChild(xmlNode3, lastChild);
                xmlDocument.Save(this.FullSourcePath);
            }
            return ConversionResult.Success;
        }

        internal RedboxConfigurationFile()
        {
            this.FullSourcePath = System.IO.Path.Combine(this.Path, this.FileName);
        }
    }
}

using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace Redbox.HAL.Configuration
{
    public sealed class ConfigurationService : IConfigurationService
    {
        internal const string IndexerGroup = "indexer";
        internal const string TextIndexerRegex = "\\[\"(?<indexer>.*?)\"\\]";
        internal const string NumericIndexerRegex = "\\[(?<indexer>[0-9]*?)\\]";
        private readonly Dictionary<string, IAttributeXmlConfiguration> Configurations = new Dictionary<string, IAttributeXmlConfiguration>();
        private readonly string ConfigurationFile;
        private readonly char[] PropertyPathSeparators = new char[1]
        {
      '.'
        };
        private readonly BindingFlags GetObjectFromPathBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.GetField | BindingFlags.GetProperty;

        public static ConfigurationService Make(string configFile)
        {
            return new ConfigurationService(configFile);
        }

        public string FormatAsXml(string friendly)
        {
            friendly = friendly.ToLower();
            if (!this.Configurations.ContainsKey(friendly))
                return (string)null;
            IAttributeXmlConfiguration configuration = this.Configurations[friendly];
            return PropertyHelper.FormatObjectAsXml((object)configuration, configuration.RootName);
        }

        public void UpdateFromXml(string key, string xmlData, ErrorList errors)
        {
            LogHelper.Instance.Log("[Configuration Service] Configuration change.");
            string lower = key.ToLower();
            if (!this.Configurations.ContainsKey(lower))
            {
                LogHelper.Instance.Log("[Configuration Service UpdateFromXml] The configuration {0} doesn't exist.", (object)key);
                errors.Add(Error.NewError("S001", string.Format("The requested configuration name '{0}' is not known.", (object)key), "Specify one of the valid configuration names."));
            }
            else
            {
                IAttributeXmlConfiguration configuration = this.Configurations[lower];
                this.BroadcastChangeStart(configuration);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xmlData);
                PropertyHelper.UpdateObjectFromXml((object)configuration, (XmlNode)xmlDocument.DocumentElement);
                this.Save(errors, configuration);
                this.BroadcastChangeEnd(configuration);
            }
        }

        public void Load(ErrorList errors)
        {
            if (!File.Exists(this.ConfigurationFile))
            {
                errors.Add(Error.NewError("P001", string.Format("The configuration file {0} does not exist.", (object)this.ConfigurationFile), "Specify a valid configuration file path."));
            }
            else
            {
                XmlDocument document = new XmlDocument();
                document.Load(this.ConfigurationFile);
                if (document.DocumentElement == null)
                    return;
                foreach (string key in this.Configurations.Keys)
                    this.Configurations[key].LoadProperties(document, errors);
                LogHelper.Instance.Log("[Configuration Service] Broadcast configuration load.");
                foreach (string key in this.Configurations.Keys)
                    this.Configurations[key].NotifyConfigurationLoaded();
            }
        }

        public void Save(ErrorList errors)
        {
            if (!File.Exists(this.ConfigurationFile))
            {
                errors.Add(Error.NewError("P001", string.Format("The configuration file {0} does not exist.", (object)this.ConfigurationFile), "Specify a valid configuration file path."));
            }
            else
            {
                XmlDocument document = new XmlDocument();
                document.Load(this.ConfigurationFile);
                if (document.DocumentElement == null)
                    return;
                foreach (string key in this.Configurations.Keys)
                    this.Configurations[key].StoreProperties(document, errors);
                document.Save(this.ConfigurationFile);
            }
        }

        public object GetPropertyByName(string key, string name)
        {
            if (name == null)
                return (object)null;
            IAttributeXmlConfiguration rootObject = this.FromKey(key);
            return rootObject != null ? this.GetValueForPath(name, (object)rootObject) : (object)null;
        }

        public void SetPropertyByName(string key, string name, object[] value)
        {
            LogHelper.Instance.Log("[Configuration Service] Property update.");
            if (name == null || value == null)
                return;
            IAttributeXmlConfiguration xmlConfiguration = this.FromKey(key);
            if (xmlConfiguration == null)
                return;
            this.BroadcastChangeStart(xmlConfiguration);
            this.SetValueForPath(name, (object)xmlConfiguration, 1 == value.Length ? value[0] : (object)value);
            this.Save(new ErrorList(), xmlConfiguration);
            this.BroadcastChangeEnd(xmlConfiguration);
        }

        public void RegisterConfiguration(string key, IAttributeXmlConfiguration me)
        {
            string lower = key.ToLower();
            if (this.Configurations.ContainsKey(lower))
                return;
            this.Configurations[lower] = me;
        }

        public void LoadAndImport(ErrorList errors)
        {
            XmlDocument document = new XmlDocument();
            document.Load(this.ConfigurationFile);
            if (document.DocumentElement == null)
                return;
            this.Load(errors);
            foreach (string key in this.Configurations.Keys)
            {
                IAttributeXmlConfiguration configuration = this.Configurations[key];
                configuration.Import(errors);
                configuration.StoreProperties(document, errors);
            }
            document.Save(this.ConfigurationFile);
        }

        public void LoadAndUpgrade(ErrorList errors)
        {
            XmlDocument document = new XmlDocument();
            document.Load(this.ConfigurationFile);
            if (document.DocumentElement == null)
                return;
            foreach (string key in this.Configurations.Keys)
                this.Configurations[key].Upgrade(document, errors);
            document.Save(this.ConfigurationFile);
        }

        public IAttributeXmlConfiguration FindConfiguration(string name) => this.FromKey(name);

        public IAttributeXmlConfiguration FindConfiguration(Redbox.HAL.Component.Model.Configurations configuration)
        {
            return this.FindConfiguration(configuration.ToString());
        }

        private void Save(ErrorList errors, IAttributeXmlConfiguration configuration)
        {
            if (!File.Exists(this.ConfigurationFile))
            {
                errors.Add(Error.NewError("P001", string.Format("The configuration file {0} does not exist.", (object)this.ConfigurationFile), "Specify a valid configuration file path."));
            }
            else
            {
                XmlDocument document = new XmlDocument();
                document.Load(this.ConfigurationFile);
                if (document.DocumentElement == null)
                    return;
                configuration.StoreProperties(document, errors);
                document.Save(this.ConfigurationFile);
            }
        }

        private void BroadcastChangeEnd(IAttributeXmlConfiguration config)
        {
            LogHelper.Instance.Log("[ConfigurationService] Broadcast config change end.");
            config.NotifyConfigurationChangeEnd();
        }

        private void BroadcastChangeStart(IAttributeXmlConfiguration config)
        {
            LogHelper.Instance.Log("[ConfigurationService] Broadcast config change start.");
            config.NotifyConfigurationChangeStart();
        }

        private object GetValueForPath(string path, object rootObject)
        {
            return this.GetObjectFromPath(path, rootObject, new int?());
        }

        private IAttributeXmlConfiguration FromKey(string key)
        {
            key = key.ToLower();
            return this.Configurations.ContainsKey(key) ? this.Configurations[key] : (IAttributeXmlConfiguration)null;
        }

        private object ExtractIndexInternal(string part)
        {
            if (part == null)
                return (object)null;
            Match match1 = Regex.Match(part, "\\[\"(?<indexer>.*?)\"\\]", RegexOptions.Singleline);
            Match match2 = Regex.Match(part, "\\[(?<indexer>[0-9]*?)\\]", RegexOptions.Singleline);
            object indexInternal = (object)null;
            if (match1.Success)
            {
                indexInternal = (object)match1.Groups["indexer"].Captures[0].Value;
            }
            else
            {
                int result;
                if (match2.Success && int.TryParse(match2.Groups["indexer"].Captures[0].Value, out result))
                    indexInternal = (object)result;
            }
            return indexInternal;
        }

        private object GetObjectFromPath(string path, object rootObject, int? depthAdjust)
        {
            if (path == null)
                return (object)null;
            string[] strArray = path.Split(this.PropertyPathSeparators);
            if (strArray.Length == 0 || rootObject == null)
                return (object)null;
            int length = strArray.Length;
            if (depthAdjust.HasValue)
                length -= depthAdjust.Value;
            object target = rootObject;
            for (int index = 0; index < length; ++index)
            {
                object[] args = (object[])null;
                string str = strArray[index];
                object indexInternal = this.ExtractIndexInternal(str);
                if (indexInternal != null)
                {
                    str = str.Substring(0, str.IndexOf("["));
                    args = new object[1] { indexInternal };
                }
                try
                {
                    target = target.GetType().InvokeMember(str, this.GetObjectFromPathBindingFlags, (Binder)null, target, args);
                    if (target == null)
                        break;
                }
                catch (Exception ex)
                {
                    return (object)null;
                }
            }
            return target;
        }

        private void SetValueForPath(string path, object rootObject, object value)
        {
            object objectFromPath = this.GetObjectFromPath(path, rootObject, new int?(1));
            if (objectFromPath == null)
                return;
            string[] strArray = path.Split(this.PropertyPathSeparators);
            PropertyInfo property = objectFromPath.GetType().GetProperty(strArray[strArray.Length - 1], BindingFlags.Instance | BindingFlags.Public);
            property?.SetValue(objectFromPath, ConversionHelper.ChangeType(value, property.PropertyType), (object[])null);
        }

        private ConfigurationService(string file)
        {
            if (string.IsNullOrEmpty(file))
                return;
            this.ConfigurationFile = file;
        }
    }
}

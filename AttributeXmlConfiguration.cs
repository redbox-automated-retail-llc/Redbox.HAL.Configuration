using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Attributes;
using Redbox.HAL.Component.Model.Extensions;
using Redbox.HAL.Component.Model.Timers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml;

namespace Redbox.HAL.Configuration
{
    public abstract class AttributeXmlConfiguration :
      IAttributeXmlConfiguration,
      IConfigurationObserver
    {
        private readonly Type ThisType;
        private readonly List<IConfigurationObserver> Observers = new List<IConfigurationObserver>();
        private const BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.Public;

        public void NotifyConfigurationLoaded()
        {
            foreach (IConfigurationObserver observer in this.Observers)
                observer.NotifyConfigurationLoaded();
        }

        public void NotifyConfigurationChangeStart()
        {
            foreach (IConfigurationObserver observer in this.Observers)
                observer.NotifyConfigurationChangeStart();
        }

        public void NotifyConfigurationChangeEnd()
        {
            foreach (IConfigurationObserver observer in this.Observers)
                observer.NotifyConfigurationChangeEnd();
        }

        public void AddObserver(IConfigurationObserver observer) => this.Observers.Add(observer);

        public void RemoveObserver(IConfigurationObserver observer) => this.Observers.Remove(observer);

        public void LoadProperties(XmlDocument document, ErrorList errors)
        {
            using (ExecutionTimer executionTimer = new ExecutionTimer())
            {
                foreach (XmlNode childNode in document.DocumentElement.SelectSingleNode(this.FileNodeName).ChildNodes)
                {
                    string name = childNode.Name;
                    try
                    {
                        PropertyInfo property = this.ThisType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                        if (property == null)
                        {
                            if (LogHelper.Instance.IsLevelEnabled(LogEntryType.Debug))
                                LogHelper.Instance.Log("[AttributeXmlConfiguration] Unable to find property '{0}'", (object)name);
                        }
                        else if (Attribute.GetCustomAttribute((MemberInfo)property, typeof(XmlConfigurationAttribute)) is XmlConfigurationAttribute)
                        {
                            object obj = ConversionHelper.ChangeType((object)childNode.InnerText, property.PropertyType);
                            property.SetValue((object)this, obj, (object[])null);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Instance.Log("[AbstractXmlConfiguration] LoadProperties caught an exception.", ex);
                    }
                }
                LogHelper.Instance.Log("[AbstractXmlConfiguration] Time to load defaults on type {0}: {1}ms", (object)this.ThisType.Name, (object)executionTimer.ElapsedMilliseconds);
                executionTimer.Stop();
            }
            this.LoadPropertiesInner(document, errors);
        }

        public void StoreProperties(XmlDocument document, ErrorList errors)
        {
            using (ExecutionTimer executionTimer = new ExecutionTimer())
            {
                foreach (PropertyInfo property in this.ThisType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (Attribute.GetCustomAttribute((MemberInfo)property, typeof(XmlConfigurationAttribute)) is XmlConfigurationAttribute)
                    {
                        try
                        {
                            string str = property.GetValue((object)this, (object[])null).ToString();
                            this.SetSingleNodeValue((XmlNode)document.DocumentElement, string.Format("{0}/{1}", (object)this.FileNodeName, (object)property.Name), (object)str);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Instance.Log(string.Format("[AbstractXmlConfiguration] StoreProperties caught an exception processing property {0}", (object)property.Name), ex);
                        }
                    }
                }
                executionTimer.Stop();
                LogHelper.Instance.Log("[AbstractXmlConfiguration] Time to save defaults on type {0}: {1}ms", (object)this.ThisType.Name, (object)executionTimer.ElapsedMilliseconds);
            }
            this.StorePropertiesInner(document, errors);
        }

        public void Import(ErrorList errors) => this.ImportInner(errors);

        public void Upgrade(XmlDocument document, ErrorList errors)
        {
            this.UpgradeInner(document, errors);
        }

        [Browsable(false)]
        public string RootName { get; private set; }

        protected AttributeXmlConfiguration(string xmlRoot, Type type)
        {
            this.RootName = xmlRoot;
            this.ThisType = type;
            using (ExecutionTimer executionTimer = new ExecutionTimer())
            {
                foreach (PropertyInfo property in this.ThisType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (Attribute.GetCustomAttribute((MemberInfo)property, typeof(XmlConfigurationAttribute)) is XmlConfigurationAttribute customAttribute)
                    {
                        try
                        {
                            object obj = ConversionHelper.ChangeType((object)customAttribute.DefaultValue, property.PropertyType);
                            property.SetValue((object)this, obj, (object[])null);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Instance.Log(string.Format("[AttributeXmlConfiguration] Ctor caught an exception processing property {0}.", (object)property.Name), ex);
                        }
                    }
                }
                executionTimer.Stop();
                LogHelper.Instance.Log("[AttributeXmlConfiguration] Time to set defaults on type {0}: {1}ms", (object)type.Name, (object)executionTimer.ElapsedMilliseconds);
            }
        }

        protected abstract void ImportInner(ErrorList errors);

        protected abstract void UpgradeInner(XmlDocument document, ErrorList errors);

        protected abstract void LoadPropertiesInner(XmlDocument document, ErrorList errors);

        protected abstract void StorePropertiesInner(XmlDocument document, ErrorList errors);

        protected abstract string FileNodeName { get; }

        private void SetSingleNodeValue(XmlNode node, string path, object value)
        {
            this.GetTargetNode(node, path).InnerText = value.ToString();
        }

        private T GetNodeValue<T>(XmlNode parent, string singleNode, T defaultValue)
        {
            XmlNode xmlNode = parent.SelectSingleNode(singleNode);
            try
            {
                IRuntimeService service = ServiceLocator.Instance.GetService<IRuntimeService>();
                if (xmlNode != null)
                {
                    if (!string.IsNullOrEmpty(xmlNode.InnerText))
                        return (T)ConversionHelper.ChangeType((object)service.ExpandConstantMacros(xmlNode.InnerText), typeof(T));
                }
            }
            catch (ArgumentException ex)
            {
            }
            return defaultValue;
        }

        private XmlNode GetTargetNode(XmlNode node, string path)
        {
            string[] strArray = path.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            XmlNode targetNode = node;
            foreach (string str in strArray)
            {
                XmlNode newChild = targetNode.SelectSingleNode(str);
                if (newChild == null)
                {
                    newChild = (XmlNode)targetNode.OwnerDocument.CreateElement(str);
                    targetNode.AppendChild(newChild);
                }
                targetNode = newChild;
            }
            return targetNode;
        }
    }
}

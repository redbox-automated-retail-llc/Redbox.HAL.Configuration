using Redbox.HAL.Component.Model;
using Redbox.HAL.Component.Model.Attributes;
using Redbox.HAL.Component.Model.Extensions;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Redbox.HAL.Configuration
{
    public static class PropertyHelper
    {
        public static string FormatObjectAsXml(object instance, string rootName)
        {
            using (StringWriter w = new StringWriter())
            {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter((TextWriter)w))
                {
                    xmlTextWriter.WriteStartDocument();
                    xmlTextWriter.WriteStartElement(rootName);
                    PropertyHelper.WalkObjectTree(instance, (XmlWriter)xmlTextWriter);
                    xmlTextWriter.WriteEndElement();
                    xmlTextWriter.WriteEndDocument();
                    xmlTextWriter.Flush();
                    return w.ToString();
                }
            }
        }

        public static void UpdateObjectFromXml(object instance, XmlNode parentNode)
        {
            if (instance == null || parentNode == null)
                return;
            XmlNodeList xmlNodeList1 = parentNode.SelectNodes("property");
            if (xmlNodeList1 == null)
                return;
            foreach (XmlNode parentNode1 in xmlNodeList1)
            {
                try
                {
                    if (parentNode1.Attributes["read-only"] != null)
                    {
                        if (string.Compare(parentNode1.Attributes["read-only"].Value, bool.TrueString, true) == 0)
                            continue;
                    }
                    PropertyInfo element = (PropertyInfo)null;
                    if (parentNode1.Attributes["name"] != null)
                        element = instance.GetType().GetProperty(parentNode1.Attributes["name"].Value, BindingFlags.Instance | BindingFlags.Public);
                    if (element != null)
                    {
                        XmlNodeList xmlNodeList2 = parentNode1.SelectNodes("property");
                        if (xmlNodeList2 != null && xmlNodeList2.Count > 0)
                        {
                            PropertyHelper.UpdateObjectFromXml(element.GetValue(instance, new object[0]), parentNode1);
                        }
                        else
                        {
                            CustomEditorAttribute customAttribute = (CustomEditorAttribute)Attribute.GetCustomAttribute((MemberInfo)element, typeof(CustomEditorAttribute));
                            if (parentNode1.ChildNodes.Count > 0 && customAttribute != null && customAttribute.SetMethodName != null)
                                instance.GetType().GetMethod(customAttribute.SetMethodName, BindingFlags.Instance | BindingFlags.Public)?.Invoke(instance, new object[1]
                                {
                  (object) parentNode1
                                });
                            else if (parentNode1.Attributes["value"] != null)
                            {
                                string str = parentNode1.Attributes["value"].Value;
                                element.SetValue(instance, ConversionHelper.ChangeType((object)str, element.PropertyType), (object[])null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Log("UpdateXml caught exception", ex);
                }
            }
        }

        private static void WalkObjectTree(object instance, XmlWriter xmlWriter)
        {
            if (instance == null)
                return;
            foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                BrowsableAttribute customAttribute1 = (BrowsableAttribute)Attribute.GetCustomAttribute((MemberInfo)property, typeof(BrowsableAttribute));
                if (customAttribute1 == null || customAttribute1.Browsable)
                {
                    xmlWriter.WriteStartElement("property");
                    xmlWriter.WriteAttributeString("name", property.Name);
                    DisplayNameAttribute customAttribute2 = (DisplayNameAttribute)Attribute.GetCustomAttribute((MemberInfo)property, typeof(DisplayNameAttribute));
                    if (customAttribute2 != null)
                        xmlWriter.WriteAttributeString("display-name", customAttribute2.DisplayName);
                    DescriptionAttribute customAttribute3 = (DescriptionAttribute)Attribute.GetCustomAttribute((MemberInfo)property, typeof(DescriptionAttribute));
                    if (customAttribute3 != null)
                        xmlWriter.WriteAttributeString("description", customAttribute3.Description);
                    CategoryAttribute customAttribute4 = (CategoryAttribute)Attribute.GetCustomAttribute((MemberInfo)property, typeof(CategoryAttribute));
                    if (customAttribute4 != null)
                        xmlWriter.WriteAttributeString("category", customAttribute4.Category);
                    ReadOnlyAttribute customAttribute5 = (ReadOnlyAttribute)Attribute.GetCustomAttribute((MemberInfo)property, typeof(ReadOnlyAttribute));
                    if (customAttribute5 != null)
                        xmlWriter.WriteAttributeString("read-only", XmlConvert.ToString(customAttribute5.IsReadOnly));
                    CustomEditorAttribute customAttribute6 = (CustomEditorAttribute)Attribute.GetCustomAttribute((MemberInfo)property, typeof(CustomEditorAttribute));
                    if (customAttribute6 != null)
                    {
                        xmlWriter.WriteAttributeString("custom-editor", customAttribute6.Text);
                        if (customAttribute6.GetMethodName != null)
                            instance.GetType().GetMethod(customAttribute6.GetMethodName, BindingFlags.Instance | BindingFlags.Public)?.Invoke(instance, new object[1]
                            {
                (object) xmlWriter
                            });
                    }
                    else
                    {
                        object obj = (object)null;
                        try
                        {
                            obj = property.GetValue(instance, new object[0]);
                            if (obj != null)
                            {
                                TypeConverter converter = TypeDescriptor.GetConverter(obj);
                                string str = converter == null || !converter.CanConvertTo(typeof(string)) ? obj.ToString() : converter.ConvertToString(obj);
                                xmlWriter.WriteAttributeString("value", str);
                                xmlWriter.WriteAttributeString("default-value", str);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Instance.Log(string.Format("Walk tree: caught an exception {0}", (object)ex.Message), LogEntryType.Error);
                        }
                        if (!Attribute.IsDefined((MemberInfo)property, typeof(ExcludeTypeAttribute)))
                            xmlWriter.WriteAttributeString("type", property.PropertyType.AssemblyQualifiedName);
                        if (Attribute.IsDefined((MemberInfo)property, typeof(RecurseAttribute)) && obj != null)
                        {
                            PropertyHelper.WalkObjectTree(obj, xmlWriter);
                        }
                        else
                        {
                            ValidValueListProviderAttribute customAttribute7 = (ValidValueListProviderAttribute)Attribute.GetCustomAttribute((MemberInfo)property, typeof(ValidValueListProviderAttribute));
                            if (customAttribute7 != null)
                            {
                                MethodInfo method = instance.GetType().GetMethod(customAttribute7.MethodName, BindingFlags.Instance | BindingFlags.Public);
                                if (method != null && method.Invoke(instance, (object[])null) is string[] strArray)
                                {
                                    xmlWriter.WriteAttributeString("valid-value-count", strArray.Length.ToString());
                                    foreach (string str in strArray)
                                    {
                                        xmlWriter.WriteStartElement("valid");
                                        xmlWriter.WriteAttributeString("value", str);
                                        xmlWriter.WriteEndElement();
                                    }
                                }
                            }
                        }
                    }
                    xmlWriter.WriteEndElement();
                }
            }
        }
    }
}

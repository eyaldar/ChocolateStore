using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ChocolateStore
{
    public class XmlToDynamic
    {
        public static void Parse(dynamic parent, XElement node, string rootElementName)
        {
            var item = new ExpandoObject();

            if (node.Name.LocalName == rootElementName)
            {
                Parse(parent, node);
            }
            else if (node.HasElements)
            {
                foreach (var element in node.Elements())
                {
                    Parse(parent, element, rootElementName);
                }
            }
        }

        private static void Parse(dynamic parent, XElement node)
        {
            var item = new ExpandoObject();

            if (node.HasElements)
            {
                var list = new List<dynamic>();
                foreach (var element in node.Elements())
                {
                    Parse(list, element);
                }

                //AddProperty(item, node.Elements().First().Name.LocalName, list);
                AddProperty(parent, node.Name.LocalName, list);
            }
            else
            {
                AddAttributes(node, item);
                AddProperty(parent, node.Name.LocalName, item);
            }
        }

        private static void AddAttributes(XElement node, ExpandoObject item)
        {
            foreach (var attribute in node.Attributes())
            {
                if (!attribute.Name.LocalName.Equals("xmlns"))
                {
                    AddProperty(item, attribute.Name.LocalName, attribute.Value.Trim());
                }
            }
        }

        private static void AddProperty(dynamic parent, string name, object value)
        {
            if (parent is List<dynamic>)
            {
                (parent as List<dynamic>).Add(value);
            }
            else
            {
                (parent as IDictionary<String, object>)[name] = value;
            }
        }
    }
}

using System.Xml;
using System.Xml.Serialization;

namespace HGT.EAM.WebServices.Conector.Architecture.Extensions;

public static class XmlExtensions
{
    public static T DeserializeFromXmlElement<T>(this XmlElement element)
    {
        var serializer = new XmlSerializer(typeof(T));

        return (T)serializer.Deserialize(new XmlNodeReader(element));
    }

    public static string GetStringXML<T>(this T @object) where T : class
    {
        string xmlString;
        XmlSerializer serializer = new XmlSerializer(typeof(T));

        using (StringWriter writer = new StringWriter())
        {
            serializer.Serialize(writer, @object);
            xmlString = writer.ToString();
        }
        return xmlString;
    }

    public static XmlElement SerializeToXmlElement(this object o)
    {
        var doc = new XmlDocument();

        using (XmlWriter writer = doc.CreateNavigator().AppendChild())
        {
            new XmlSerializer(o.GetType()).Serialize(writer, o);
        }

        return doc.DocumentElement;
    }
}

using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace GtsTest.Services.Mes.WebService
{
    /// <summary>
    /// SOAP 1.1 / 1.2 请求构造器
    /// </summary>
    public static class SoapEnvelopeBuilder
    {
        /// <summary>
        /// SOAP 1.1 命名空间
        /// </summary>
        public const string NsSoap11 = "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        /// SOAP 1.2 命名空间
        /// </summary>
        public const string NsSoap12 = "http://www.w3.org/2003/05/soap-envelope";

        /// <summary>
        /// 构造 SOAP 请求 XML
        /// </summary>
        /// <param name="methodName">方法名，如 "ReportProduction"</param>
        /// <param name="targetNamespace">命名空间，如 "http://tempuri.org/"</param>
        /// <param name="parameters">参数字典</param>
        /// <param name="soapVersion">"1.1" 或 "1.2"</param>
        public static string Build(
            string methodName,
            string targetNamespace,
            IDictionary<string, object?> parameters,
            string soapVersion = "1.1")
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false,
                Encoding = new UTF8Encoding(false)
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                string soapNs = soapVersion == "1.2" ? NsSoap12 : NsSoap11;

                writer.WriteStartDocument();
                writer.WriteStartElement("soap", "Envelope", soapNs);
                writer.WriteStartElement("soap", "Body", soapNs);

                // 方法节点（带 targetNamespace）
                writer.WriteStartElement(methodName, targetNamespace);

                // 参数
                foreach (var kv in parameters)
                {
                    writer.WriteStartElement(kv.Key);
                    if (kv.Value != null)
                        writer.WriteValue(kv.Value.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // method
                writer.WriteEndElement(); // Body
                writer.WriteEndElement(); // Envelope
                writer.WriteEndDocument();
            }

            return sb.ToString();
        }

        /// <summary>
        /// SOAP 1.1 的 Content-Type
        /// </summary>
        public static string ContentType11 => "text/xml; charset=utf-8";

        /// <summary>
        /// SOAP 1.2 的 Content-Type
        /// </summary>
        public static string ContentType12 => "application/soap+xml; charset=utf-8";
    }
}
using System;
using System.Xml;

namespace GtsTest.Services.Mes.WebService
{
    /// <summary>
    /// SOAP 响应解析器
    /// </summary>
    public static class SoapResponseParser
    {
        /// <summary>
        /// 解析 SOAP 响应，提取指定节点的文本值
        /// </summary>
        /// <param name="xml">响应 XML</param>
        /// <param name="resultNodeName">返回节点名，如 "ReportProductionResult"；为空则返回 Body 全部文本</param>
        public static string? GetResultValue(string xml, string? resultNodeName = null)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var nsMgr = new XmlNamespaceManager(doc.NameTable);
                nsMgr.AddNamespace("soap11", SoapEnvelopeBuilder.NsSoap11);
                nsMgr.AddNamespace("soap12", SoapEnvelopeBuilder.NsSoap12);

                // 检查 Fault
                var fault = doc.SelectSingleNode("//soap11:Fault", nsMgr)
                          ?? doc.SelectSingleNode("//soap12:Fault", nsMgr);
                if (fault != null)
                {
                    var faultString = fault.SelectSingleNode(".//faultstring")?.InnerText
                                   ?? fault.SelectSingleNode(".//soap12:Text", nsMgr)?.InnerText
                                   ?? "Unknown SOAP Fault";
                    throw new Exception($"SOAP Fault: {faultString}");
                }

                // 定位 Body
                var body = doc.SelectSingleNode("//soap11:Body", nsMgr)
                        ?? doc.SelectSingleNode("//soap12:Body", nsMgr);
                if (body == null) return null;

                // 找目标节点
                if (!string.IsNullOrEmpty(resultNodeName))
                {
                    var node = FindNodeByName(body, resultNodeName);
                    return node?.InnerText;
                }

                // 返回第一个子元素的 InnerText
                return body.FirstChild?.InnerText;
            }
            catch (Exception ex)
            {
                throw new Exception($"SOAP 响应解析失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 递归查找指定名称的节点（忽略命名空间）
        /// </summary>
        private static XmlNode? FindNodeByName(XmlNode parent, string name)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child.LocalName == name) return child;
                var found = FindNodeByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// 判断响应是否为 SOAP Fault
        /// </summary>
        public static bool IsFault(string xml, out string? faultMessage)
        {
            faultMessage = null;
            if (string.IsNullOrWhiteSpace(xml)) return false;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var nsMgr = new XmlNamespaceManager(doc.NameTable);
                nsMgr.AddNamespace("soap11", SoapEnvelopeBuilder.NsSoap11);
                nsMgr.AddNamespace("soap12", SoapEnvelopeBuilder.NsSoap12);

                var fault = doc.SelectSingleNode("//soap11:Fault", nsMgr)
                          ?? doc.SelectSingleNode("//soap12:Fault", nsMgr);
                if (fault != null)
                {
                    faultMessage = fault.SelectSingleNode(".//faultstring")?.InnerText
                                ?? fault.SelectSingleNode(".//soap12:Text", nsMgr)?.InnerText
                                ?? "Unknown Fault";
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
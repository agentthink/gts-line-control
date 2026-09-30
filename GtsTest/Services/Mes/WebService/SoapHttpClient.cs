using GtsTest.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GtsTest.Services.Mes.WebService
{
    /// <summary>
    /// 基于 HttpClient 的 SOAP 客户端
    /// 不依赖 WCF，直接构造/解析 SOAP Envelope
    /// </summary>
    public class SoapHttpClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _soapAction;
        private readonly string _targetNamespace;
        private readonly string _soapVersion;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="endpointUrl">WebService 地址，如 http://localhost:8080/Service.asmx</param>
        /// <param name="targetNamespace">命名空间，如 "http://tempuri.org/"</param>
        /// <param name="soapAction">SOAPAction，如 "http://tempuri.org/ReportProduction"；SOAP 1.1 必填</param>
        /// <param name="soapVersion">"1.1" 或 "1.2"</param>
        /// <param name="timeoutMs">超时（毫秒）</param>
        public SoapHttpClient(
            string endpointUrl,
            string targetNamespace = "http://tempuri.org/",
            string soapAction = "",
            string soapVersion = "1.1",
            int timeoutMs = 10000)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(endpointUrl),
                Timeout = TimeSpan.FromMilliseconds(timeoutMs)
            };
            _targetNamespace = targetNamespace;
            _soapAction = soapAction;
            _soapVersion = soapVersion;
        }

        /// <summary>
        /// 添加默认 HTTP 头
        /// </summary>
        public void AddHeader(string name, string value)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
        }

        /// <summary>
        /// 调用 SOAP 方法
        /// </summary>
        /// <param name="methodName">方法名</param>
        /// <param name="parameters">参数</param>
        /// <param name="resultNodeName">响应中结果节点名（可选）</param>
        /// <param name="ct">取消令牌</param>
        public async Task<string?> InvokeAsync(
            string methodName,
            IDictionary<string, object?> parameters,
            string? resultNodeName = null,
            CancellationToken ct = default)
        {
            // 1. 构造 SOAP 请求
            string soapXml = SoapEnvelopeBuilder.Build(
                methodName, _targetNamespace, parameters, _soapVersion);

            AppLogger.Debug($"[SOAP] 请求 {methodName}: {soapXml}", "MES-SOAP");

            // 2. 构造 HTTP 请求
            var content = new StringContent(soapXml, Encoding.UTF8);

            if (_soapVersion == "1.2")
            {
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                    SoapEnvelopeBuilder.ContentType12);
                if (!string.IsNullOrEmpty(_soapAction))
                {
                    // SOAP 1.2：action 放在 Content-Type 的 action 参数里
                    content.Headers.ContentType.Parameters.Add(
                        new System.Net.Http.Headers.NameValueHeaderValue("action", $"\"{_soapAction}\""));
                }
            }
            else
            {
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                    SoapEnvelopeBuilder.ContentType11);
                if (!string.IsNullOrEmpty(_soapAction))
                {
                    content.Headers.Add("SOAPAction", $"\"{_soapAction}\"");
                }
            }

            // 3. 发送请求
            var response = await _httpClient.PostAsync("", content, ct).ConfigureAwait(false);

            string responseXml = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            AppLogger.Debug($"[SOAP] 响应 HTTP {(int)response.StatusCode}: {responseXml}", "MES-SOAP");

            // 4. 检查 HTTP 状态码
            if (!response.IsSuccessStatusCode)
            {
                // 5xx 服务端错误 / 4xx 客户端错误
                throw new HttpRequestException(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\n{responseXml}");
            }

            // 5. 检查 SOAP Fault
            if (SoapResponseParser.IsFault(responseXml, out var faultMsg))
            {
                throw new Exception($"SOAP Fault: {faultMsg}");
            }

            // 6. 解析响应
            return SoapResponseParser.GetResultValue(responseXml, resultNodeName);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
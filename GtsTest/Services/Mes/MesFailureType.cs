namespace GtsTest.Services.Mes
{
    /// <summary>
    /// MES 上报失败类型（用于分级重传策略）
    /// </summary>
    public enum MesFailureType
    {
        /// <summary>成功（无失败）</summary>
        None = 0,

        /// <summary>连接超时 / 请求超时</summary>
        Timeout = 1,

        /// <summary>网络连接失败</summary>
        ConnectionFailed = 2,

        /// <summary>服务端错误（5xx）</summary>
        ServerError = 3,

        /// <summary>客户端错误（4xx，配置错误，不自动重试）</summary>
        ClientError = 4,

        /// <summary>未知错误</summary>
        Unknown = 99
    }
}
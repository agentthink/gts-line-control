namespace GtsTest.Services.Data
{
    /// <summary>
    /// 兼容接口：历史上用于用户仓储，现在语义上就是 IUserRepository。
    /// 保留此名称以保证老代码（如 LoginForm / AuthenticationService）零改动。
    ///
    /// 新代码请直接依赖 IUserRepository。
    /// </summary>
    public interface IDataRepository : IUserRepository
    {
    }
}
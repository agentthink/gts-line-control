using GtsTest.Models;
using System.Collections.Generic;

namespace GtsTest.Services.Data
{
    /// <summary>
    /// 用户仓储接口：只定义"用户表"相关的数据访问契约。
    /// </summary>
    public interface IUserRepository
    {
        User? GetUserByUsername(string username);
        bool AddUser(User user);
        bool UpdateUser(User user);
        List<User> GetAllUsers(bool includeDeleted = false);
        bool SoftDeleteUser(long userId, string deletedBy);
        bool RestoreUser(long userId);
    }
}
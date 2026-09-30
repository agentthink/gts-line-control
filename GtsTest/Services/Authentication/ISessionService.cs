using GtsTest.Models;
using System;

namespace GtsTest.Services.Authentication
{
    /// <summary>
    /// 会话服务接口：管理当前登录用户。
    /// 好处：可被 DI 装配、可 mock、可切换实现（如未来改为多用户会话）。
    /// </summary>
    public interface ISessionService
    {
        /// <summary>当前登录用户（未登录为 null）</summary>
        User? CurrentUser { get; }

        /// <summary>是否已登录</summary>
        bool IsLoggedIn { get; }

        /// <summary>用户变化事件（登录 / 注销时触发）</summary>
        event Action<User?>? OnUserChanged;

        /// <summary>登录</summary>
        void Login(User user);

        /// <summary>注销</summary>
        void Logout();
    }
}
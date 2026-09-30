using GtsTest.Core;                 // ← 修复 CS0103：AppLogger 在 GtsTest.Core
using GtsTest.Models;
using GtsTest.Services.Data;
using System;

namespace GtsTest.Services.Authentication
{
    /// <summary>
    /// 【静态兼容层】保留原有的静态入口（SessionManager.Login / CurrentUser / OnUserChanged）
    /// 内部委托到 ISessionService 实例。
    ///
    /// 迁移期结束后可删除此类，把调用点全部改为注入 ISessionService。
    /// </summary>
    public static class SessionManager
    {
        private static ISessionService _default;

        static SessionManager()
        {
            // 兜底：DI 装配之前也能用（自组装一个默认实例）
            var auditService = new AuditServiceImpl();
            _default = new SessionServiceImpl(auditService);
            _default.OnUserChanged += OnDefaultUserChanged;
        }

        /// <summary>设置默认实例（由 DI 容器装配时调用）</summary>
        public static void SetDefault(ISessionService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            // 解除旧实例的事件订阅
            try { _default.OnUserChanged -= OnDefaultUserChanged; } catch { }

            _default = service;
            _default.OnUserChanged += OnDefaultUserChanged;

            // 立刻同步当前用户到 AppLogger（若 DI 实例已有用户）
            AppLogger.CurrentUser = _default.CurrentUser?.Username ?? "未登录";
        }

        private static void OnDefaultUserChanged(User? user)
        {
            try { OnUserChanged?.Invoke(user); }
            catch { /* 订阅者异常不影响主流程 */ }
        }

        // ---------- 静态外观 ----------
        public static event Action<User?>? OnUserChanged;

        public static User? CurrentUser => _default.CurrentUser;
        public static bool IsLoggedIn => _default.IsLoggedIn;

        /// <summary>登录（保留 repo 参数以兼容旧调用，实际由 DI 实例处理审计）</summary>
        public static void Login(User user, IDataRepository repo)
            => _default.Login(user);

        /// <summary>注销（保留 repo 参数以兼容旧调用）</summary>
        public static void Logout(IDataRepository repo)
            => _default.Logout();
    }
}
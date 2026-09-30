using GtsTest.Core;
using GtsTest.Models;
using GtsTest.Services.Data;
using System;

namespace GtsTest.Services.Authentication
{
    /// <summary>
    /// 会话服务默认实现：单例持有当前用户 + 登录/注销时写审计日志。
    /// </summary>
    public class SessionServiceImpl : ISessionService
    {
        private readonly IAuditService _auditService;
        private User? _currentUser;

        public event Action<User?>? OnUserChanged;

        public User? CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null;

        public SessionServiceImpl(IAuditService auditService)
        {
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        public void Login(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            _currentUser = user;
            AppLogger.CurrentUser = user.Username ?? "未登录";

            try { OnUserChanged?.Invoke(user); }
            catch (Exception ex) { AppLogger.Warn($"OnUserChanged 处理异常: {ex.Message}", "Session"); }

            try
            {
                _auditService.Log(user.Id, user.Username, "Login", "用户登录成功", null);
            }
            catch { }
        }

        public void Logout()
        {
            try
            {
                if (_currentUser != null)
                    _auditService.Log(_currentUser.Id, _currentUser.Username, "Logout", "用户注销", null);
            }
            catch { }

            _currentUser = null;
            AppLogger.CurrentUser = "未登录";

            try { OnUserChanged?.Invoke(null); }
            catch (Exception ex) { AppLogger.Warn($"OnUserChanged 处理异常: {ex.Message}", "Session"); }
        }
    }
}
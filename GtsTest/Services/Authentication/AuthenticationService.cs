using GtsTest.Core;
using GtsTest.Models;
using GtsTest.Services.Data;
using System;
using System.Security.Cryptography;

namespace GtsTest.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _repo;   // 🆕 从 IDataRepository 改为 IUserRepository
        private const int MaxFailedAttempts = 5;
        private readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthenticationService(IUserRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        // 注册用户
        public bool RegisterUser(string username, string fullName, string password, string role = "Operator")
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var existing = _repo.GetUserByUsername(username);
            if (existing != null) return false;

            var hash = AuthenticationHelper.HashPassword(password);
            var user = new User
            {
                Username = username,
                PasswordHash = hash,
                Salt = null,
                FullName = fullName,
                Role = role,
                IsActive = 1,
                CreatedTime = DateTime.UtcNow.ToString("o")
            };

            var ok = _repo.AddUser(user);
            if (ok) AppLogger.Info($"用户 {username} 已注册", "Auth");
            else AppLogger.Warn($"注册用户 {username} 失败", "Auth");
            return ok;
        }

        public bool ValidateCredentials(string username, string password, out User? userOut)
        {
            userOut = null;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var user = _repo.GetUserByUsername(username);
            if (user == null) return false;
            if (user.IsActive == 0) return false;

            // 检查锁定状态
            if (!string.IsNullOrEmpty(user.LockoutUntil))
            {
                if (DateTime.TryParse(user.LockoutUntil, out var lockoutTime))
                {
                    if (DateTime.UtcNow < lockoutTime)
                    {
                        AppLogger.Warn($"用户 {username} 已被锁定至 {lockoutTime}", "Auth");
                        return false;
                    }
                }
            }

            // 优先尝试新格式 PBKDF2
            if (AuthenticationHelper.VerifyPassword(password, user.PasswordHash, out bool needsRehash))
            {
                if (needsRehash)
                {
                    try
                    {
                        user.PasswordHash = AuthenticationHelper.HashPassword(password);
                        _repo.UpdateUser(user);
                        AppLogger.Info($"用户 {username} 的密码哈希已升级（自动迁移）", "Auth");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Warn($"用户 {username} 密码迁移失败: {ex.Message}", "Auth");
                    }
                }

                ResetFailedAttempts(user);
                userOut = user;
                AppLogger.Info($"用户 {username} 登录成功", "Auth");
                return true;
            }

            // 兼容旧的 SHA256(salt+password) 格式
            if (!string.IsNullOrEmpty(user.Salt))
            {
                try
                {
                    using var sha = SHA256.Create();
                    var combined = System.Text.Encoding.UTF8.GetBytes(user.Salt + password);
                    var digest = sha.ComputeHash(combined);
                    var digestHex = BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();

                    if (string.Equals(user.PasswordHash, digestHex, StringComparison.OrdinalIgnoreCase))
                    {
                        user.PasswordHash = AuthenticationHelper.HashPassword(password);
                        user.Salt = null;
                        _repo.UpdateUser(user);

                        ResetFailedAttempts(user);
                        userOut = user;
                        AppLogger.Info($"用户 {username} 使用旧哈希登录，已自动迁移到 PBKDF2", "Auth");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warn($"旧哈希验证出错: {ex.Message}", "Auth");
                }
            }

            // 验证失败：增加失败计数
            IncrementFailedAttempts(user);
            AppLogger.Warn($"用户 {username} 登录失败", "Auth");
            return false;
        }

        public bool ChangePassword(string username, string currentPassword, string newPassword)
        {
            var user = _repo.GetUserByUsername(username);
            if (user == null) return false;

            if (!ValidateCredentials(username, currentPassword, out var _))
                return false;

            user.PasswordHash = AuthenticationHelper.HashPassword(newPassword);
            user.Salt = null;
            var ok = _repo.UpdateUser(user);
            if (ok) AppLogger.Info($"用户 {username} 修改了密码", "Auth");
            return ok;
        }

        public string ResetPasswordAsAdmin(string username)
        {
            var user = _repo.GetUserByUsername(username);
            if (user == null) throw new InvalidOperationException("user not found");

            var (plain, hash) = AuthenticationHelper.CreateInitialPassword();
            user.PasswordHash = hash;
            user.Salt = null;
            _repo.UpdateUser(user);

            AppLogger.Info($"管理员为 {username} 重置了密码（临时）", "Auth");
            return plain;
        }

        public string? EnsureDefaultAdmin()
        {
            var admin = _repo.GetUserByUsername("admin");
            if (admin != null) return null;

            var (plain, hash) = AuthenticationHelper.CreateInitialPassword();
            var user = new User
            {
                Username = "admin",
                FullName = "超级管理员",
                PasswordHash = hash,
                Salt = null,
                Role = "Admin",
                IsActive = 1,
                CreatedTime = DateTime.UtcNow.ToString("o")
            };
            var ok = _repo.AddUser(user);
            if (ok)
            {
                AppLogger.Info($"已创建默认管理员帐号 admin（密码: {plain}），请立即修改密码", "Auth");
                return plain;
            }
            else
            {
                AppLogger.Error("创建默认管理员失败", "Auth");
                return null;
            }
        }

        // ----------------- 失败计数/锁定 -----------------
        private void ResetFailedAttempts(User user)
        {
            try
            {
                user.FailedAttempts = 0;
                user.LockoutUntil = null;
                _repo.UpdateUser(user);
            }
            catch { }
        }

        private void IncrementFailedAttempts(User user)
        {
            try
            {
                user.FailedAttempts++;
                if (user.FailedAttempts >= MaxFailedAttempts)
                {
                    user.LockoutUntil = DateTime.UtcNow.Add(LockoutDuration).ToString("o");
                    AppLogger.Warn($"用户 {user.Username} 被锁定到 {user.LockoutUntil}", "Auth");
                }
                _repo.UpdateUser(user);
            }
            catch { }
        }

        // ★★★ 权限判断：不区分大小写比较角色 ★★★
        public bool HasPermission(User user, string permissionCode)
        {
            if (user == null) return false;

            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase)) return true;

            if (string.Equals(user.Role, "Engineer", StringComparison.OrdinalIgnoreCase))
            {
                if (permissionCode == PermissionCodes.UserManage) return false;
                if (permissionCode == PermissionCodes.AuditView) return false;
                return true;
            }

            if (string.Equals(user.Role, "Operator", StringComparison.OrdinalIgnoreCase))
            {
                return permissionCode switch
                {
                    PermissionCodes.DeviceView => true,
                    PermissionCodes.DeviceStart => true,
                    PermissionCodes.DeviceStop => true,
                    PermissionCodes.DeviceStartAll => true,
                    PermissionCodes.DeviceStopAll => true,
                    PermissionCodes.DeviceReset => true,
                    PermissionCodes.WorkflowRun => true,
                    PermissionCodes.WorkflowStop => true,
                    PermissionCodes.AlarmReset => true,
                    PermissionCodes.AlarmAcknowledge => true,
                    PermissionCodes.AlarmResolve => true,
                    PermissionCodes.EmergencyStop => true,
                    _ => false
                };
            }

            return false;
        }
    }
}
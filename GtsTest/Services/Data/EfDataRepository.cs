using GtsTest.Data;
using GtsTest.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtsTest.Services.Data
{
    /// <summary>
    /// 用户仓储的 EF Core 实现。
    ///
    /// ★ 关键设计：本类**不持有** DbContext 字段，
    ///   每个方法内部使用短生命周期的 DbContext（using 释放），
    ///   从而保证多线程并发调用时线程安全（DbContext 本身线程不安全）。
    ///
    /// 因此本类是无状态的，可以安全地注册为 DI 单例。
    /// </summary>
    public class EfDataRepository : IDataRepository, IDisposable
    {
        /// <summary>无参构造：兼容旧代码 + DI 装配</summary>
        public EfDataRepository()
        {
            // 不再创建长生命周期的 DbContext
        }

        /// <summary>保留带参构造以兼容旧代码（但内部不再使用传入的 db 作为长期字段）</summary>
        public EfDataRepository(GtsDbContext db)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            // 忽略参数，避免外部误以为它是长期持有的
        }

        // ================================================================
        // 用户管理
        // ================================================================
        public User? GetUserByUsername(string username)
        {
            using var db = DbContextFactory.Create();
            return db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Username == username && u.IsDeleted == 0);
        }

        public bool AddUser(User user)
        {
            if (user == null) return false;
            try
            {
                using var db = DbContextFactory.Create();
                db.Users.Add(user);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Core.AppLogger.Warn($"AddUser 失败: {ex.Message}", "EfRepo");
                return false;
            }
        }

        public bool UpdateUser(User user)
        {
            if (user == null) return false;
            try
            {
                using var db = DbContextFactory.Create();
                var existing = db.Users.Find(user.Id);
                if (existing == null) return false;

                // 把 user 的所有字段值复制到 existing（EF 会追踪 existing）
                db.Entry(existing).CurrentValues.SetValues(user);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Core.AppLogger.Warn($"UpdateUser 失败: {ex.Message}", "EfRepo");
                return false;
            }
        }

        public List<User> GetAllUsers(bool includeDeleted = false)
        {
            using var db = DbContextFactory.Create();
            var q = db.Users.AsNoTracking();
            if (!includeDeleted)
                q = q.Where(u => u.IsDeleted == 0);
            return q.OrderBy(u => u.Username).ToList();
        }

        // ================================================================
        // 逻辑删除
        // ================================================================
        public bool SoftDeleteUser(long userId, string deletedBy)
        {
            try
            {
                using var db = DbContextFactory.Create();
                var user = db.Users.Find(userId);
                if (user == null) return false;
                user.IsDeleted = 1;
                user.DeletedTime = DateTime.UtcNow.ToString("o");
                user.DeletedBy = deletedBy ?? "系统";
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Core.AppLogger.Warn($"SoftDeleteUser 失败: {ex.Message}", "EfRepo");
                return false;
            }
        }

        public bool RestoreUser(long userId)
        {
            try
            {
                using var db = DbContextFactory.Create();
                var user = db.Users.Find(userId);
                if (user == null) return false;
                user.IsDeleted = 0;
                user.DeletedTime = null;
                user.DeletedBy = null;
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Core.AppLogger.Warn($"RestoreUser 失败: {ex.Message}", "EfRepo");
                return false;
            }
        }

        // ================================================================
        // 无状态类，无需释放任何资源
        // ================================================================
        public void Dispose() { }
    }
}
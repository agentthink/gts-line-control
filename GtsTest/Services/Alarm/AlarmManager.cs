using GtsTest.Data;
using GtsTest.Services.Data;
using GtsTest.Services.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtsTest.Services.Alarm
{
    public class AlarmManager : IAlarmManager
    {
        private readonly List<AlarmRecord> _alarms = new List<AlarmRecord>();
        private readonly object _lock = new object();
        private readonly IAlarmRepository _repository;
        private readonly ILogger _logger;
        private bool _loaded = false;

        public event EventHandler<AlarmRecord> AlarmAdded;
        public event EventHandler<int> AlarmAcknowledged;
        public event EventHandler<int> AlarmResolved;

        /// <summary>无参构造（向后兼容旧代码，内部自行组装仓储）</summary>
        public AlarmManager() : this(null, null) { }

        /// <summary>DI 注入构造</summary>
        public AlarmManager(IAlarmRepository? repository, ILogger? logger)
        {
            _logger = logger ?? new AppLoggerWrapper();

            // DI 未提供仓储时降级自组装（保证旧代码可用）
             _repository = repository
                ?? new AlarmRepository(new DbConnectionFactory(), _logger); 

            _repository.EnsureTable();
            LoadFromDatabase();
        }

        public void LoadFromDatabase()
        {
            lock (_lock)
            {
                _alarms.Clear();
                var active = _repository.QueryActive();
                _alarms.AddRange(active);
                _loaded = true;
                _logger.Info($"📚 已加载 {active.Count} 条活动报警", "AlarmManager");
            }
        }

        public void TriggerAlarm(string deviceId, string message, AlarmSeverity severity)
        {
            var record = new AlarmRecord
            {
                DeviceId = deviceId,
                Message = message,
                Severity = severity,
                Timestamp = DateTime.Now,
                IsAcknowledged = false,
                IsResolved = false
            };

            // 1. 持久化，回填 Id
            try
            {
                record.Id = _repository.Insert(record);
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ 报警写入数据库失败: {ex.Message}", "AlarmManager");
                return;
            }

            // 2. 加入内存列表
            lock (_lock)
            {
                _alarms.Insert(0, record);
            }

            // 3. 事件通知（只触发一次）
            AlarmAdded?.Invoke(this, record);

            _logger.Info($"🔔 报警触发: 设备={deviceId}, 严重等级={severity}, 消息={message}", "AlarmManager");
        }

        public bool AcknowledgeAlarm(int alarmId, string user)
        {
            lock (_lock)
            {
                var alarm = _alarms.FirstOrDefault(a => a.Id == alarmId);
                if (alarm == null || alarm.IsAcknowledged) return false;

                alarm.IsAcknowledged = true;
                alarm.AcknowledgedBy = user;
                alarm.AcknowledgedTime = DateTime.Now;

                try { _repository.Update(alarm); }
                catch (Exception ex) { _logger.Error($"❌ 确认报警写库失败: {ex.Message}", "AlarmManager"); }

                AlarmAcknowledged?.Invoke(this, alarmId);
                return true;
            }
        }

        public bool ResolveAlarm(int alarmId, string user)
        {
            lock (_lock)
            {
                var alarm = _alarms.FirstOrDefault(a => a.Id == alarmId);
                if (alarm == null || alarm.IsResolved) return false;

                alarm.IsResolved = true;
                alarm.ResolvedBy = user;
                alarm.ResolvedTime = DateTime.Now;

                try { _repository.Update(alarm); }
                catch (Exception ex) { _logger.Error($"❌ 解决报警写库失败: {ex.Message}", "AlarmManager"); }

                AlarmResolved?.Invoke(this, alarmId);
                return true;
            }
        }

        public IEnumerable<AlarmRecord> GetActiveAlarms()
        {
            lock (_lock) return _alarms.Where(a => !a.IsResolved).ToList();
        }

        public IEnumerable<AlarmRecord> GetAllAlarms()
        {
            lock (_lock) return _alarms.ToList();
        }
    }
}
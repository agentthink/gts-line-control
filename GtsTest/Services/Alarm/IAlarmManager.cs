using GtsTest.Services.Data;
using System;
using System.Collections.Generic;

namespace GtsTest.Services.Alarm
{
    public interface IAlarmManager
    {
        event EventHandler<AlarmRecord> AlarmAdded;
        event EventHandler<int> AlarmAcknowledged;
        event EventHandler<int> AlarmResolved;

        void TriggerAlarm(string deviceId, string message, AlarmSeverity severity);
        bool AcknowledgeAlarm(int alarmId, string user);
        bool ResolveAlarm(int alarmId, string user);
        IEnumerable<AlarmRecord> GetActiveAlarms();
        IEnumerable<AlarmRecord> GetAllAlarms();
        void LoadFromDatabase();
    }
}
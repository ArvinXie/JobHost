using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JobHost
{
    public class UnparallelTask
    {
        private readonly string TaskName = "";

        private Logger SysLogger;

        private static readonly object LockSync = new object();
        public static Dictionary<string, int> TaskDict { get; set; }



        public UnparallelTask(string taskName)
        {
            TaskName = taskName;
            SysLogger = LogManager.GetLogger(taskName);
        }

        public void Handle(string taskKey, Action action)
        {
            var key = $"{TaskName}-{taskKey}";
            bool accessQuit = false;
            try
            {
                if (!CanTaskRun(key))
                {
                    accessQuit = true;
                    SysLogger.Info($"{TaskName}执行中，跳过:{taskKey}");
                    return;
                }
                SysLogger.Info($"{TaskName}开始执行:{taskKey}");
                action?.Invoke();
            }
            finally
            {
                if (!accessQuit)
                {
                    SysLogger.Info($"{TaskName}执行完成:{taskKey}");
                    ReleaseTaskInfo(key);
                }
            }

        }

        private void ReleaseTaskInfo(string taskKey)
        {
            lock (LockSync)
            {
                if (TaskDict.ContainsKey(taskKey))
                {
                    TaskDict.Remove(taskKey);
                }
            }
            var taskCount = TaskDict.Keys?.Where(m => m.StartsWith($"{TaskName}-"))?.Count() ?? 0;
            SysLogger.Info($"{TaskName}剩余任务量：{taskCount}");
        }

        private bool CanTaskRun(string taskKey)
        {
            lock (LockSync)
            {
                if (TaskDict == null) TaskDict = new Dictionary<string, int>();
                if (TaskDict.ContainsKey(taskKey))
                {
                    return false;
                }
                TaskDict.Add(taskKey, 0);
            }
            return true;
        }

    }
}

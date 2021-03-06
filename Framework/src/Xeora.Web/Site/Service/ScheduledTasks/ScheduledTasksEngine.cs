﻿using System;
using System.Threading;
using System.Collections.Concurrent;

namespace Xeora.Web.Site.Service
{
    public class ScheduledTasksEngine : Basics.Service.IScheduledTaskEngine
    {
        System.Timers.Timer _ExecutionTimer;

        ConcurrentDictionary<long, ConcurrentQueue<TaskInfo>> _ExecutionList;
        ConcurrentDictionary<string, bool> _ListOfCanceled;

        public ScheduledTasksEngine()
        {
            this._ExecutionTimer = new System.Timers.Timer(1000);
            this._ExecutionTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.Execute);
            this._ExecutionTimer.Start();

            this._ExecutionList = new ConcurrentDictionary<long, ConcurrentQueue<TaskInfo>>();
            this._ListOfCanceled = new ConcurrentDictionary<string, bool>();
        }

        public string RegisterTask(Action<object[]> scheduledCallBack, object[] @params, DateTime executionTime)
        {
            long executionID = Helper.DateTime.Format(executionTime);

            ConcurrentQueue<TaskInfo> queue;
            if (!this._ExecutionList.TryGetValue(executionID, out queue))
            {
                queue = new ConcurrentQueue<TaskInfo>();

                if (!this._ExecutionList.TryAdd(executionID, queue))
                    return this.RegisterTask(scheduledCallBack, @params, executionTime);
            }

            TaskInfo taskInfo = new TaskInfo(scheduledCallBack, @params, executionTime);

            queue.Enqueue(taskInfo);

            return taskInfo.ID;
        }

        public string RegisterTask(Action<object[]> scheduledCallBack, object[] @params, TimeSpan executionTime)
        {
            DateTime absoluteExecutionTime = DateTime.Now.Add(executionTime);

            return this.RegisterTask(scheduledCallBack, @params, absoluteExecutionTime);
        }

        public void UnRegisterTask(string id) =>
            this._ListOfCanceled.TryAdd(id, true);

        private void Execute(object sender, EventArgs args)
        {
            long executionID = Helper.DateTime.Format();

            ConcurrentQueue<TaskInfo> queue;
            if (this._ExecutionList.TryRemove(executionID, out queue))
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.ExecutionThread), queue);
        }

        private void ExecutionThread(object state)
        {
            ConcurrentQueue<TaskInfo> queue = (ConcurrentQueue<TaskInfo>)state;

            while (!queue.IsEmpty)
            {
                TaskInfo taskInfo;
                if (queue.TryDequeue(out taskInfo))
                {
                    bool dummy;
                    if (!this._ListOfCanceled.TryRemove(taskInfo.ID, out dummy))
                        ThreadPool.QueueUserWorkItem(
                            (object taskState) => {
                                try
                                {
                                    ((TaskInfo)taskState).Execute();
                                }
                                catch (System.Exception ex)
                                {
                                    Helper.EventLogger.Log(ex);
                                }
                            }, 
                            taskInfo
                        );
                }
            }
        }
    }
}

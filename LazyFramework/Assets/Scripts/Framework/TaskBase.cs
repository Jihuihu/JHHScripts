using System;

public class TaskBase
{

    public TaskBase(string name)
    {
        Name = name;
        State = TaskState.Init;
        m_taskManager = TaskManager.Instance;
    }

    /// <summary>
    /// 启动task，并注册到manager
    /// </summary>
    /// <returns></returns>
    public bool Start(System.Object param = null)
    {
        if (State != TaskState.Init)
            return false;

        if (!m_taskManager.RegisterTask(this))
        {
            return false;
        }

        State = TaskState.Running;
        if (!OnStart(param))
        {
            Stop();
            return false;
        }

        if (EventOnStart != null)
            EventOnStart(this);


        return true;
    }

    public void Tick()
    {
        if (State == TaskState.Running)
        {
            OnTick();
        }
    }

    /// <summary>
    /// 停止task,并从manager注销
    /// </summary>
    public void Stop()
    {
        if (State == TaskState.Stopped)
            return;

        State = TaskState.Stopped;

        OnStop();

        if (EventOnStop != null)
            EventOnStop(this);

        m_taskManager.UnregisterTask(this);
    }

    /// <summary>
    /// 暂停task
    /// </summary>
    public void Pause()
    {
        if (State != TaskState.Running && State != TaskState.Init)
            return;

        // 记录暂停开始时间
        PauseStartTime = DateTime.Now;

        State = TaskState.Paused;

        OnPause();

        if (EventOnPause != null)
            EventOnPause(this);
    }

    /// <summary>
    /// 从暂停中恢复task
    /// </summary>
    /// <returns></returns>
    public bool Resume(System.Object param = null)
    {
        if (State != TaskState.Paused)
            return false;

        State = TaskState.Running;

        if (!OnResume(param))
        {
            State = TaskState.Paused;
            return false;
        }

        if (EventOnResume != null)
            EventOnResume(this);
        return true;
    }

    protected void ClearOnStopEvent()
    {
        EventOnStop = null;
    }

    /// <summary>
    /// task事件回调处理虚函数,之类需要在其中实现各自逻辑
    /// </summary>
    /// <returns></returns>
    protected virtual bool OnStart(System.Object param) { return true; } // do nothing
    protected virtual void OnPause() { } // do nothing
    protected virtual bool OnResume(System.Object param) { return true; } // do nothing
    protected virtual void OnStop() { } // do nothing
    protected virtual void OnTick() { } // do nothing

    /// <summary>
    /// Task向外通知的事件
    /// </summary>
    public event Action<TaskBase> EventOnStart;
    public event Action<TaskBase> EventOnStop;
    public event Action<TaskBase> EventOnPause;
    public event Action<TaskBase> EventOnResume;

    /// <summary>
    /// task管理器
    /// </summary>
    private TaskManager m_taskManager;

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// 状态
    /// </summary>
    public TaskState State { get; private set; }

    /// <summary>
    /// 暂停的开始时间
    /// </summary>
    public DateTime PauseStartTime { get; private set; }

    /// <summary>
    /// task的状态定义
    /// </summary>
    public enum TaskState
    {
        Init,
        Running,
        Paused,
        Stopped,
    }
}

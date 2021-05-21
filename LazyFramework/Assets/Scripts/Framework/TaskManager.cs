using System;
using System.Collections.Generic;

/// <summary>
/// Task管理器
/// </summary>
public class TaskManager
{

    private TaskManager() { }

    public static TaskManager CreateTaskManager()
    {
        if (m_instance == null)
        {
            m_instance = new TaskManager();
        }
        return m_instance;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <returns></returns>
    public bool Initlize()
    {
        return true;
    }

    /// <summary>
    /// 释放
    /// </summary>
    public void Uninitlize()
    {
        m_taskList4TickLoop.AddRange(m_taskList);
        foreach (var task in m_taskList4TickLoop)
        {
            if (task.State != TaskBase.TaskState.Stopped)
            {
                task.Stop();
            }
        }
        m_taskList.Clear();
        m_taskRegDict.Clear();
    }

    /// <summary>
    /// 由task自己调用，向管理器注册
    /// </summary>
    /// <param name="task"></param>
    public bool RegisterTask(TaskBase task)
    {
        // 处理注册冲突的情况
        if (!string.IsNullOrEmpty(task.Name) && m_taskRegDict.ContainsKey(task.Name))
        {
            if (m_taskRegDict[task.Name] != task)
                Debug.LogError(String.Format("Task name collision. Name: {0}.", task.Name));
            else
                Debug.LogError(String.Format("Readding task. Name: {0}.", task.Name));
            return false;
        }

        // 处理注册冲突的情况
        if (m_taskRegDict.ContainsValue(task))
        {
            Debug.LogError(String.Format("Re-adding same task with different name. Task name {0}", task.Name));
            return false;
        }

        // 将task添加到容器中
        m_taskList.Add(task);
        if (!string.IsNullOrEmpty(task.Name))
            m_taskRegDict.Add(task.Name, task);

        return true;
    }
    /// <summary>
    /// 有task自己调用，向管理器注销
    /// </summary>
    /// <param name="task"></param>
    public void UnregisterTask(TaskBase task)
    {
        if (m_taskList.Remove(task))
        {
            if (!string.IsNullOrEmpty(task.Name))
            {
                m_taskRegDict.Remove(task.Name);
            }
            return;
        }
        Debug.LogWarning("Can't find task " + task.Name + " in task manager.");
    }

    /// <summary>
    /// 启动Task
    /// </summary>
    /// <returns></returns>
    public TaskBase StartTask()
    {
        /* 
           检查需要启动的Task与其它Task的冲突关系
           1.获取冲突组，准备将冲突组中的Task全部Stop
           3.启动指定Task

           不存在暂停组，直接共存，后面的Task继续运行，根据逻辑需求暂停,暂停时逻辑开销除去Tick中的都不存在了，而且必须要HideAllView 渲染开销也不存在

            // 悬浮窗的控制 极端案例:
            主界面+日常任务+签到+点击显示物品 此时的DC是不是很夸张了 这种设计尽量不要使用 理论上每个非悬浮窗的UITask都包含底板            
            二级界面 使用SimpleUI  MiniUI 用来做功能的快速支持

            UITask中基本保持一个在前，其它Pause或者Stop，但是Battle肯定是和BattleUI共存的 还有哪些会共存？ 
            ProjectL中 WorldSceneTask 还有 BattleSceneTask

            只有UITask之间才会自动Stop(关闭冲突的，比如战斗内外冲突)，其它Task之间依靠逻辑控制
            StartTask（targetTask,autoClearOther = true）
            private startTaskInternal 内部管理时使用 （内部处理后 Start 和Return时启动其它Task）
        */

        return null;
    }

    /// <summary>
    /// 根据Task名字关闭对应的Task
    /// </summary>
    /// <param name="taskName"></param>
    public void StopTaskWithTaskName(string taskName)
    {
        if (string.IsNullOrEmpty(taskName))
            return;

        TaskBase currTask = null;
        if (m_taskRegDict.TryGetValue(taskName, out currTask))
        {
            // 停止Task
            currTask.Stop();

            // 注销
            UnregisterTask(currTask);
        }
    }

    public void Tick()
    {
        m_taskList4TickLoop.AddRange(m_taskList);
        foreach (var task in m_taskList4TickLoop)
        {
            task.Tick();
        }
        m_taskList4TickLoop.Clear();
    }


    /// <summary>
    /// 单例访问器
    /// </summary>
    public static TaskManager Instance { get { return m_instance; } }
    private static TaskManager m_instance;

    /// <summary>
    /// task 列表
    /// </summary>
    private List<TaskBase> m_taskList = new List<TaskBase>();
    private List<TaskBase> m_taskList4TickLoop = new List<TaskBase>();
    /// <summary>
    /// 以task name为key的字典
    /// </summary>
    private Dictionary<String, TaskBase> m_taskRegDict = new Dictionary<string, TaskBase>();

}

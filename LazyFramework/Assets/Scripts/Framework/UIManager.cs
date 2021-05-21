using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using FairyGUI;

namespace MaxGame.UI
{
    /// <summary>
    /// 2019 08 21 UI管理
    /// 按显示层级分类，通过层级来确定当前最上层级的是哪个UI（是UGUI还是Fairyui）
    /// 1、 底层 PVE、城建item、战斗【城建item、战斗不在UIManager管理】
    /// 2、 全屏的界面（原入栈的UI），主界面和其他全屏界面，开启后，会将该队列内前一个UI关闭（主界面除外）
    /// 3、 非全屏，资源条
    /// 4、 非全屏，其他界面（各种非全屏界面、小窗、确认框、提示、新手引导界面等等）
    /// 
    /// 20210518 新UI管理
    /// 栈由保存UITaskBase，改为保存全部显示的UIIntent，UIIntent对应有uitask
    /// UIIntent记录是否全屏
    /// 开启界面处理：
    ///     非全屏，开启或找到对应uitask，移动到栈顶
    ///     全屏，开启或找到对应uitask，移动到栈顶，此后面的界面都隐藏
    /// 关闭界面处理：
    ///     全屏，释放对应uitask，之后遍历此界面后面的需要重新开启的界面，自底向上依次开启
    ///     非全屏，释放对应uitask，自此向下当前显示的栈，若查找到同一name，则重新开启之
    /// 开启或关闭全屏界面的后处理
    ///     记录当前全屏界面
    ///     处理对应全屏界面的资源条
    /// 资源条
    ///     对于fairy，资源条直接挂在到对应界面
    ///     对于ugui，启动资源条uitask
    /// fairy的uitask的sortingOrder
    ///     4 提示层，高于引导
    ///     3 新手引导空挡板
    ///     2 新手引导层
    ///     1 普通层
    ///     0 默认层，尽量不用
    /// fairy级别的popup层
    /// 普通层管理问题	一个uitask的层级插入问题（还原一个被占用或隐藏的ui时）
    /// 资源条不入栈，单独处理（入栈带来很多栈内添加麻烦）
    /// </summary>

    public class UIManager
    {
        //默认UI相机的Depth
        private const int UICameraDepthDefault = 90;
        //低于默认UI相机的Depth  -- 用来控制GUI和Fairy显示
        private const int UICameraDepthLow = 89;
        //高于默认UI相机的Depth  -- 用来控制GUI和Fairy显示
        private const int UICameraDepthHigh = 91;

        private UIManager() { }

        /// <summary>
        /// uitask的分组信息
        /// </summary>
        private Dictionary<string, int> m_uiTaskGroupRegDict = new Dictionary<string, int>();
        /// <summary>
        /// uitask的字典，名称为key
        /// </summary>
        private Dictionary<string, UITaskBase> m_uiTaskDict = new Dictionary<string, UITaskBase>();
        /// <summary>
        /// uitaskgroup的冲突信息
        /// </summary>
        private List<List<int>> m_uiTaskGroupConflictList = new List<List<int>>();
        /// <summary>
        /// 需要停止的task的列表
        /// </summary>
        private List<UITaskBase> m_taskList4Stop = new List<UITaskBase>();
        /// <summary>
        /// 上一次tick暂停超时的时间
        /// </summary>
        protected DateTime m_lastTickPauseTimeOutTime = DateTime.MinValue;
        /// <summary>
        /// 暂停超时，单位秒
        /// </summary>
        public const double UITaskPauseTimeOut = 120;

        public static bool HaveSetRoot = false;
        public static Camera UICamera;
        // 将来扩充多个Root内的Layer,使用新的结构
        public static Transform UIRoot_UsedLayer;
        public static Transform UIRoot_UnUseLayer;
        public static Transform UIRoot_UICut;
        public static Transform UIRoot_3DUILayer;

        public static Transform Other_Chapters;

        /// <summary>
        /// 单例访问器
        /// </summary>
        public static UIManager Instance { get { return m_instance; } }
        private static UIManager m_instance;

        private bool m_IsMainUITop = true;
        public bool IsMainUITop
        {
            get { return m_IsMainUITop; }
        }

        private bool m_IsMainUITopExceptGuideSoft = true;
        public Boolean IsMainUITopExceptGuideSoft
        {
            get { return m_IsMainUITopExceptGuideSoft; }
        }


        //提供给预加载的UI，加载后立刻隐藏
        private List<UITaskBase> m_uiPauseOnStart = new List<UITaskBase>();

        private Dictionary<string, string> _fairyPkgDependence = new Dictionary<string, string>();

        public static UIManager CreteUIMgr()
        {
            if (m_instance == null)
            {
                m_instance = new UIManager();
                //if (!m_instance.Initlize())
                //    Debug.LogError("Error ! m_uiManager.Initlize() fail");
            }
            return m_instance;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public bool Initlize()
        {
            Debug.Log(string.Format("UIManager.Initlize start"));

            #region UI 组之间的冲突关系

            // 登录与其它互斥
            SetUITaskGroupConflict((int)ComDef.UIGroup.Login, (int)ComDef.UIGroup.Common);
            SetUITaskGroupConflict((int)ComDef.UIGroup.Login, (int)ComDef.UIGroup.World);
            SetUITaskGroupConflict((int)ComDef.UIGroup.Login, (int)ComDef.UIGroup.Battle);
            SetUITaskGroupConflict((int)ComDef.UIGroup.Login, (int)ComDef.UIGroup.ReLogin);

            // 世界 与战斗互斥
            SetUITaskGroupConflict((int)ComDef.UIGroup.World, (int)ComDef.UIGroup.Battle);
            #endregion
            return true;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Uninitlize()
        {
            Debug.Log(string.Format("UIManager.Uninitlize"));
            if (m_uiTaskGroupRegDict != null)
                m_uiTaskGroupRegDict.Clear();
            if (m_uiTaskDict != null)
            {
                foreach (var item in m_uiTaskDict.Values)
                {
                    item.Pause();
                }
            }
            if (m_uiTaskGroupConflictList != null)
                m_uiTaskGroupConflictList.Clear();
            if (m_taskList4Stop != null)
                m_taskList4Stop.Clear();
            //if (m_RunUITasks != null)
            //    m_RunUITasks.Clear();
            m_IntentList.Clear();

            _fairyPkgDependence.Clear();
        }

        /// <summary>
        /// 启动uitask
        /// </summary>
        /// <param name="intent"></param>
        /// <param name="pushIntentToStack"></param>
        /// <param name="clearIntentStack">废弃</param>
        /// <returns></returns>
        public UITaskBase StartUITask(UIIntent intent, bool pushIntentToStack = true, bool clearIntentStack = false)
        {
            Debug.Log(string.Format("-- UIManager StartUITask task={0} pushToStack={1}", intent.TargetTaskName, pushIntentToStack));

            // 首先检查是否task信息是否已经注册
            if (!m_uiTaskGroupRegDict.ContainsKey(intent.TargetTaskName))
            {
                Debug.LogError(string.Format("StartUITask fail for Unregisted TargetTaskName={0}", intent.TargetTaskName));
                return null;
            }

            //bool m_TmpShowMainUI = IsShowMainUI();
            //bool _showMainTop = IsShowMainUI(true);
            // 得到uitask的实例
            var targetTask = GetOrCreateUITask(intent);
            if (targetTask == null)
            {
                Debug.LogError(string.Format("StartUITask fail for GetOrCreateUITask null TargetTaskName={0}", intent.TargetTaskName));
                return null;
            }

            // 启动uitask
            if (StartUITaskInternal_New(targetTask, intent, pushIntentToStack))
            {
                if (m_uiPauseOnStart.Count > 0)
                {
                    for (int i = 0; i < m_uiPauseOnStart.Count; i++)
                        CloseUI(m_uiPauseOnStart[i]);
                    m_uiPauseOnStart.Clear();
                }
                return targetTask;
            }
            Debug.LogError(string.Format("StartUITask fail for StartUITaskInternal fail TargetTaskName={0}", intent.TargetTaskName));
            targetTask = null;
            return null;
        }
        /// <summary>
        /// 增加加载后立刻隐藏的缓存（因为时序问题，加载立刻隐藏在同一帧不能成功）
        /// </summary>
        /// <param name="ui"></param>
        public void AddStartPauseUI(UITaskBase ui)
        {
            m_uiPauseOnStart.Add(ui);
        }


        /// <summary>
        /// 关闭UI，自动返回上一级
        /// </summary>
        /// <param name="targetTaskName">目标UI的name</param>
        /// <param name="bIncludePauseStack">是否包括缓存中的UI</param>
        public void CloseUI(string targetTaskName, bool bIncludePauseStack = false)
        {
            if (!m_uiTaskDict.ContainsKey(targetTaskName))
            {
                Debug.Log("-- ui 当前要关闭的UI = " + targetTaskName + " 不存在");
                return;
            }
            UITaskBase nowUITask = m_uiTaskDict[targetTaskName];    //当前要关闭的UI
            CloseUI(nowUITask, bIncludePauseStack);
        }

        /// <summary>
        /// 根据状态启动或者恢复task
        /// </summary>
        /// <param name="targetTask"></param>
        /// <param name="intent"></param>
        /// <returns></returns>
        private bool StartOrResumeTask(UITaskBase targetTask, UIIntent intent)
        {
            switch (targetTask.State)
            {
                case TaskBase.TaskState.Init:
                    targetTask.EventOnStop += OnUITaskStop;
                    return targetTask.Start(intent);
                case TaskBase.TaskState.Running:
                    return targetTask.OnNewIntent(intent);
                case TaskBase.TaskState.Paused:
                    return targetTask.Resume(intent);
                case TaskBase.TaskState.Stopped:
                    Debug.LogError(string.Format("StartOrResumeTask fail in TaskState.Stopped task={0}", targetTask.Name));
                    return false;
                default:
                    return false;
            }
        }

        public void Tick()
        {
            // 每5秒检查一次
            var delta = System.DateTime.Now - m_lastTickPauseTimeOutTime;
            if (delta.TotalSeconds < 5)
            {
                return;
            }

            m_lastTickPauseTimeOutTime = DateTime.Now;

            // 计算暂停超时时间
            DateTime pauseTimeOutTime = DateTime.Now.AddSeconds(-UITaskPauseTimeOut);

            // 停止所有收集到的task
            if (m_taskList4Stop.Count != 0)
            {
                foreach (var task in m_taskList4Stop)
                {
                    task.Stop();
                }
                m_taskList4Stop.Clear();
            }
        }
        /// <summary>
        /// 注册uitask的分组
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="group"></param>
        public void RegisterUITaskWithGroup(string taskName, int group)
        {
            m_uiTaskGroupRegDict[taskName] = group;
        }

        /// <summary>
        /// 注册分组的冲突关系
        /// </summary>
        /// <param name="group1"></param>
        /// <param name="group2"></param>
        public void SetUITaskGroupConflict(uint group1, uint group2)
        {
            if (group1 == group2)
            {
                return;
            }
            while (m_uiTaskGroupConflictList.Count < group1 + 1)
            {
                m_uiTaskGroupConflictList.Add(new List<int>());
            }
            while (m_uiTaskGroupConflictList.Count < group2 + 1)
            {
                m_uiTaskGroupConflictList.Add(new List<int>());
            }
            m_uiTaskGroupConflictList[(int)group1].Add((int)group2);
            m_uiTaskGroupConflictList[(int)group2].Add((int)group1);
        }

        /// <summary>
        /// 获取或者创建指定的uitask
        /// </summary>
        /// <typeparam name="TaskType"></typeparam>
        /// <param name="intent"></param>
        /// <returns></returns>
        private UITaskBase GetOrCreateUITask(UIIntent intent)
        {
            // 查看task是否已经存在
            UITaskBase targetTask;
            if (!m_uiTaskDict.TryGetValue(intent.TargetTaskName, out targetTask))
            {
                // 如果不存在创建新的task
                targetTask = Activator.CreateInstance(intent.TaskType, intent.TargetTaskName) as UITaskBase;
                // 注册到task字典
                m_uiTaskDict[intent.TargetTaskName] = targetTask;
            }
            return targetTask;
        }

        /// <summary>
        /// uitask停止的回调
        /// </summary>
        /// <param name="task"></param>
        private void OnUITaskStop(TaskBase task)
        {
            Debug.Log("UIManager::OnUITaskStop " + task.Name);

            if (m_uiTaskDict.ContainsKey(task.Name))
            {
                m_uiTaskDict.Remove(task.Name);
            }
        }

        /// <summary>
        /// 初始化资源 1.获取UICamera 2.获取UIRoot用于Layer管理
        /// </summary>
        public bool InitUiMgrStaticRes()
        {
            var uiRoot = GameObject.Find("UIRoot");

            if (HaveSetRoot) {
                GameObject.Destroy(uiRoot);
                return true;
            }

            HaveSetRoot = true;
            uiRoot.name = "UIRoot_DontDestroy";
            GameObject.DontDestroyOnLoad(uiRoot);

            if (uiRoot == null)
            {
                Debug.LogError("UIManager 初始化失败 未找到 UIRoot");
                return false;
            }

            var uiCamera = uiRoot.transform.Find("UICamera");
            UIRoot_UICut = uiRoot.transform.Find("Canvas_Cut");

            if (uiCamera == null || UIRoot_UICut == null)
            {
                Debug.LogError("UIMgr 初始化失败 UICamera或 UIRoot_UICut 未找到");
                return false;
            }

            // 获取UICamera
            UICamera = uiCamera.GetComponent<Camera>();
            // ... 未来获取多个相关Camera

            UIRoot_UsedLayer = uiRoot.transform.Find("UsedUILayer");
            UIRoot_UnUseLayer = uiRoot.transform.Find("UnUsedUILayer");
            UIRoot_3DUILayer = uiRoot.transform.Find("3DUILayer");
            // temp
            Other_Chapters = uiRoot.transform.Find("OtherLayer/Chapters");
            return true;
        }

        /// <summary>
        /// 设置UITask的 Layer为不使用
        /// </summary>
        /// <param name="go"></param>
        public static void UnUsedLayer(GameObject go)
        {
            Debug.LogError("UIMgr.UnUsedLayer  原逻辑已剔除 注意功能情况");
        }

        /// <summary>
        /// 设置UITask的Layer为使用
        /// </summary>
        /// <param name="go"></param>
        public static void UsedLayer(GameObject go)
        {
            Debug.LogError("UIMgr.UsedLayer  原逻辑已剔除 注意功能情况");
        }

        /// <summary>
        /// 通过名字查找已经创建过的UITaskBase
        /// </summary>
        /// <param name="sName">名字</param>
        /// <returns></returns>
        public UITaskBase GetTaskBaseByName(string sName)
        {
            if (m_uiTaskDict.ContainsKey(sName))
                return m_uiTaskDict[sName];
            else
                return null;
        }


        #region 20210519 栈内改为 UIIntent 方案
        private List<UIIntentInfo> m_IntentList = new List<UIIntentInfo>();
        private UIIntentInfo m_CurFullUIIntentInfo;
        private bool m_IsMainTopNew;

        private bool StartUITaskInternal_New<TaskType>(TaskType targetTask, UIIntent intent, bool bfull)
            where TaskType : UITaskBase
        {
            Debug.Log("-- ui newmgr  StartUITaskInternal_New "+ intent.TargetTaskName);

            UIIntentInfo newIntent = new UIIntentInfo(intent, bfull);
            newIntent.m_UiTask = targetTask;
            newIntent.m_IsHide = false;

            if(bfull)
            {
                //全屏，隐藏后面的UI
                for (int i = m_IntentList.Count - 1; i >= 0; i--)
                {
                    UIIntentInfo vint = m_IntentList[i];
                    if (vint.m_IsHide)
                        continue;
                    if (vint.m_UiTask == targetTask)
                        continue;
                    vint.m_IsHide = true;
                    if (vint.m_UiTask != null)
                    {
                        vint.m_UiTask.Pause();
                        Debug.Log("-- ui newmgr  StartUITaskInternal_New " + intent.TargetTaskName + " 全屏界面 底下已有UITask隐藏 " + vint.m_Intent.TargetTaskName);
                    }
                    else
                    {
                        Debug.Log("-- ui newmgr  StartUITaskInternal_New " + intent.TargetTaskName + " 全屏界面 底下无UITask隐藏 " + vint.m_Intent.TargetTaskName);
                    }
                }
                m_CurFullUIIntentInfo = newIntent;
            }
            Debug.Log("-- ui newmgr  StartUITaskInternal_New " + intent.TargetTaskName + " StartOrResumeTask begin");
            bool ret = StartOrResumeTask(targetTask, intent);
            Debug.Log("-- ui newmgr  StartUITaskInternal_New " + intent.TargetTaskName + " StartOrResumeTask end result "+ret);
            if (!ret)
            {
                Debug.LogError(string.Format("StartUITask fail task={0}", targetTask.Name));
                return false;
            }
            m_IntentList.Add(newIntent);

            Debug.Log("-- ui newmgr  StartUITaskInternal_New " + intent.TargetTaskName + " CurrentStackCheck2_new m_IsMainTopNew " + m_IsMainTopNew);
            return true;
        }

        /// <summary>
        /// 关闭UI,自动返回上一级
        /// </summary>
        /// <param name="nowUITask">目标UI的UITaskBase</param>
        /// <param name="bIncludePauseStack">是否包括缓存中的UI</param>
        public void CloseUI(UITaskBase nowUITask, bool bIncludePauseStack = false)
        {
            Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name);
            int idx = -1;
            int nextidx = -1;

            for (int i = m_IntentList.Count - 1; i >= 0; i--)
            {
                if (idx == -1)
                {
                    if (m_IntentList[i].m_UiTask == nowUITask)
                    {    //找到了
                        idx = i;
                        Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 找到所在list序列号 "+i);
                        if (m_IntentList[i].m_IsHide)
                        {
                            //本来就是隐藏的，直接处理
                            if (m_IntentList[i].m_UiTask != null)
                            {
                                Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 找到一个隐藏的 有 uitask 界面 直接处理");
                            }
                                Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 找到一个隐藏的 无 uitask 界面 直接处理");
                            m_IntentList.RemoveAt(i);
                            return; ;
                        }
                    }
                }
                else
                {
                    if(m_IntentList[idx].m_bFull)
                    {
                        //如果是关闭全屏，找下一个全屏
                        if (m_IntentList[i].m_bFull)
                        {
                            nextidx = i;
                            Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 找到下一个全屏 序列号 "+i);
                            break;
                        }
                    }
                    else
                    {
                        //否则，找下一个全屏内是否还有同类UI
                        if (m_IntentList[i].m_bFull)
                            break;
                        if(m_IntentList[i].m_Intent.TargetTaskName == nowUITask.Name)
                        {
                            nextidx = i;
                            Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 找到下一个同名界面 序列号 " + i);
                            break;
                        }
                    }
                }
            }
            if(idx != -1)
            {
                if(m_IntentList[idx].m_bFull)
                    m_CurFullUIIntentInfo = null;
                if (m_IntentList[idx].m_bFull)
                {
                    //最后一个全屏界面关闭，底下的都打开
                    bool bNoneFull = false;
                    if (nextidx == -1)
                    {
                        nextidx = 0;
                        bNoneFull = true;
                    }
                    //如果是关闭全屏，找下一个全屏
                    for (int i = nextidx; i< idx; i++)
                    {
                        if(m_IntentList[i].m_Intent.TargetTaskName == nowUITask.Name)
                        {
                            m_IntentList[i].m_UiTask = nowUITask;
                            m_IntentList[idx].m_UiTask = null;
                        }
                        if (m_IntentList[i].m_UiTask != null)
                        {
                            StartOrResumeTask(m_IntentList[i].m_UiTask, m_IntentList[i].m_Intent);
                            m_IntentList[i].m_IsHide = false;
                            Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 关闭一个全屏，恢复其他界面 序号 " + i + " name "+ m_IntentList[i].m_Intent.TargetTaskName);
                        }
                    }
                    if(!bNoneFull)
                        m_CurFullUIIntentInfo = m_IntentList[nextidx];
                }
                else
                {
                    if (nextidx != -1)
                    {
                        //否则，找下一个全屏内是否还有同类UI
                        m_IntentList[nextidx].m_UiTask = nowUITask;
                        m_IntentList[idx].m_UiTask = null;
                        StartOrResumeTask(nowUITask, m_IntentList[nextidx].m_Intent);
                        m_IntentList[nextidx].m_IsHide = false;
                        Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 关闭一个非全屏，恢复同名界面 序号 " + nextidx);

                    }
                }
                if (m_IntentList[idx].m_UiTask != null)
                {
                    Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 停止界面，移除出列表 ");
                }
                else
                {
                    Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name + " 不用停止界面， 移除出列表 ");
                }
                m_IntentList.RemoveAt(idx);
                if(m_IntentList.Count >0)
                {
                    //处理相机
                    Debug.Log("-- ui newmgr  CloseUI " + nowUITask.Name  + " CurrentStackCheck2_new m_IsMainTopNew " + m_IsMainTopNew);

                }
            }
        }

        /// <summary>
        /// 临时锁，限制在CloseStackButMain方法时不考虑MainPopup
        /// </summary>
        private bool m_LockMainPopup = false;

        /// <summary>
        /// 关闭栈内UI，直到主界面(可能保留PVE界面)
        /// </summary>
        public void CloseStackButMain(bool lockMainPopup = true)
        {
            m_LockMainPopup = true;
            bool nowShowMainUI = m_CurFullUIIntentInfo == null;
            List<UIIntentInfo> backups = new List<UIIntentInfo>();
            for (int i = m_IntentList.Count - 1; i >= 0; i--)
            {
                
            }
            m_IntentList.Clear();
            m_CurFullUIIntentInfo = null;
            if (backups.Count > 0)
            {
                for (int i = backups.Count - 1; i >= 0; i--)
                {
                    backups[i].m_IsHide = false;
                    m_IntentList.Add(backups[i]);
                }
            }

            m_LockMainPopup = false;
        }

        /// <summary>
        /// 返回上一个打开的UITask,如果不存在 则停留在当前UITask
        /// 已过时，需替换成closeUI
        /// </summary>
        /// <returns></returns>
        public UITaskBase Back()
        {
            UITaskBase lastui = null;
            if(m_IntentList.Count > 1)
            {
                lastui = m_IntentList[m_IntentList.Count - 1].m_UiTask;
                CloseUI(lastui);
            }
            return lastui;
        }

        public bool IsNoFullOnMain()
        {
            return m_CurFullUIIntentInfo == null;
        }
        #endregion
    }
    public delegate UITaskBase ActionShowUI();

    //主界面弹窗
    public class MainShowUI
    {
        /// <summary>
        /// 主界面弹出的优先级越大越先弹出
        /// </summary>
        public int priority = 0;

        public ActionShowUI call;

        /// <summary>
        /// 弹窗显示的UI名称
        /// </summary>
        public string TaskUIName;

        //按优先级大到小排
        public int SortByPriority(MainShowUI data)
        {
            if (data.priority < priority)
            {
                return -1;
            }
            else if (data.priority > priority)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public static MainShowUI Create(string pTaskName, ActionShowUI _call, int _priority)
        {
            MainShowUI UI = new MainShowUI();
            UI.call = _call;
            UI.priority = _priority;
            UI.TaskUIName = pTaskName;
            return UI;
        }
    }

    /// <summary>
    /// 非入栈UI的状况
    /// </summary>
    public enum OutStackUIState
    {
        None = 0,       //没有UI
        UGUI = 1,       //顶层是UGUI
        Fairy = 2,      //顶层是Fairy
    }

    public class UIIntentInfo
    {
        public UIIntent m_Intent { get; private set; }
        public bool m_bFull { get; private set; }
        public bool m_IsHide;
        public UITaskBase m_UiTask;

        public UIIntentInfo(UIIntent intent, bool bfull)
        {
            m_Intent = intent;
            m_bFull = bfull;
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;
using UnityEngine.UI;

namespace MaxGame.UI
{
    public class UITaskBase : TaskBase
    {
        public UITaskBase(string name)
             : base(name)
        { }

        public UIIntent m_intent;

        /// <summary>
        /// FairyGUI创建的UI 需要重写此方法 并创建自己所需的Panel 
        /// </summary>
        /// <returns></returns>
        protected virtual GComponent FairyCreatePanel()
        {
            return null;
        }
        public bool IsFairyUI { get; protected set; }
        /// <summary>
        /// 是否半透明 默认不透明
        /// 如果透明 不会自动关闭上一个UI
        /// </summary>
        public bool IsTransparent = false;


        public UIWindow mWindow;
        public UIPackage mPackage;

        public GComponent mainFPanel;

        private bool staticPrefabIsLoad = false;
        private bool staticPrefabLoading = false;       //预制体正在加载
        protected bool dynamicPrefabIsLoad = false;
        // 实例化的Prefab
        protected GameObject Go;
        protected bool Is4Main = false;
        public TaskState m_StatsWhenPoptemp;

        protected bool m_GOCreated = false;
        protected sealed override bool OnStart(System.Object param)
        {
            return OnStart(param as UIIntent);
        }

        protected virtual bool OnStart(UIIntent intent)
        {
            m_intent = intent;

            if (m_intent != null && m_intent.TargetMode == "Main")
            {
                Is4Main = true;
            }

            return StartUpdatePipeLine(intent);
        }

        /// <summary>
        /// 当task停止
        /// </summary>
        protected override void OnStop()
        {
            ClearAllContextAndRes();
        }

        /// <summary>
        /// 当task暂停
        /// </summary>
        protected override void OnPause()
        {
            Debug.Log(string.Format("-- ui UITask {0} OnPause", this.Name));

            if (mWindow != null)
            {
//                UnloadUIPkg();
                mWindow.Hide();
            }
            // HideView
            if (Go != null)
            {
                UIManager.UnUsedLayer(Go);
            }

            for (int i = 0; i < m_ChildUITask.Count; i++)
            {
                m_ChildUITask[i].Pause();
            }
        }

        /// <summary>
        /// 当task恢复
        /// </summary>
        /// <returns></returns>
        protected sealed override bool OnResume(System.Object param = null)
        {
            Debug.Log(string.Format("-- ui UITask {0} OnResume param {1}", this.Name, param));

            return OnResume(param as UIIntent);
        }
        protected virtual bool OnResume(UIIntent intent)
        {
            m_intent = intent;
            if (intent != null && intent.TargetMode == "Main")
            {
                Is4Main = true;
            }
            if (mWindow != null)
            {
                mWindow.Show();
            }
            if (Go != null) UIManager.UsedLayer(Go);

            bool brlt = StartUpdatePipeLine(intent);
            for (int i = 0; i < m_ChildUITask.Count; i++)
            {
                m_ChildUITask[i].Resume(m_ChildUITask[i].m_intent);
            }
            return brlt;
        }

        /// <summary>
        /// 当task还在运行中的时候，传来新的intent需求
        /// </summary>
        /// <param name="intent"></param>
        /// <returns></returns>
        public virtual bool OnNewIntent(UIIntent intent)
        {
            m_intent = intent;
            return StartUpdatePipeLine(intent);
        }

        public virtual int GetSortingOrder()
        {
            if (mWindow != null)
                return mWindow.sortingOrder;
            return 0;
        }

        /// <summary>
        /// UI是否在显示
        /// </summary>
        /// <returns></returns>
        public bool IsShow()
        {
            return (m_GOCreated && (State == TaskState.Running || State == TaskState.Init));
        }

        protected bool StartUpdatePipeLine(UIIntent intent, bool OnlyUpdateView = false)
        {
            // 加载基础的静态Prefab
            var path = StaticPrefabPath();

            // 创建UI对象 非FairyGUI创建的对象 MainFPanel会是空的
            if (mainFPanel == null) { mainFPanel = FairyCreatePanel(); }

            if (mainFPanel != null)
            {
                // FairyGUI创建的对象,加入Window机制
                if (mWindow == null)
                {
                    mWindow = new UIWindow();
                    mWindow.contentPane = mainFPanel;
                    DoGameObjectCreate();
                    m_GOCreated = true;
                    StartUpdateView();
//                    OnGameObjectCreate();

                }
                else
                    StartUpdateView();
                IsFairyUI = true;
            }
            else
            {
                if (!staticPrefabIsLoad)
                {
                    CreatePrefabUI(intent, path);
                    return true;
                }
            }

            if (!OnlyUpdateView)
            {
                // 更新数据缓存
                UpdateDataCache();
                //                return true;      //UI刷新数据我用的是StartUpdateView(),取消注释会导致某些功能数据刷新不了
            }

            if(!IsFairyUI && m_GOCreated)
                StartUpdateView();

            return true;
        }

        /// <summary>
        /// 创建 Prefab生成的UI
        /// </summary>
        /// <param name="intent"></param>
        /// <param name="path"></param>
        private void CreatePrefabUI(UIIntent intent, string path)
        {
            if (!string.IsNullOrEmpty(path) && !staticPrefabIsLoad && !staticPrefabLoading)
            {
                staticPrefabLoading = true;
                //if (Name == ComDef.FightUITask)
                //    MaxUtil.R_RecordTime("-- rec fightui LoadPrefab");
                // todo:jhh 资源加载先弄进来

                //ResourcesLoader.Load<GameObject>(path, prefab =>
                //{
                //    //    MaxUtil.P_RecordTime("-- rec fightui LoadPrefab");
                //    staticPrefabLoading = false;
                //    if (prefab != null)
                //    {
                //        staticPrefabIsLoad = true;

                //        #region // todo:jhh 根据逻辑需求,需要等待主界面动画
                //        var uiRoot = UIManager.UIRoot_UsedLayer;
                //        // 动画完成后的回调
                //        Go = UnityEngine.GameObject.Instantiate(prefab, uiRoot, false);
                        
                //        {
                //            UIManager.UsedLayer(Go);
                //            // temp 是否需要删除?
                //            DoGameObjectCreate();
                //            m_GOCreated = true;
                //            if (State == TaskState.Paused)
                //            {
                //                //异步时，已经被关闭了，直接隐藏
                //                UIManager.UnUsedLayer(Go);
                //            }
                //            else
                //            {
                //                // 基础静态资源加载完成,驱动View更新
                //                StartUpdateView();

                //                //if (Name == ComDef.FightUITask)
                //                //    MaxUtil.R_RecordTime("-- rec fightui LoadDynamicRes");
                //                // 自动开始加载动态资源
                //                LoadDynamicRes();
                //                //if (Name == ComDef.FightUITask)
                //                //    MaxUtil.P_RecordTime("-- rec fightui LoadDynamicRes");
                //                //if (Name == ComDef.FightUITask)
                //                //    MaxUtil.P_RecordTime("-- rec fightui init");
                //                EventManager.Brocast(EventNameConstant.EventUITaskCreated, this.Name);
                //            }
                //        }
                //        #endregion

                //        if(path != FightUITask.PrefabPath)
                //        {
                //            var canvas = Go.GetComponent<Canvas>();
                //            // UICamera指定
                //            if (canvas != null)
                //            {
                //                canvas.worldCamera = UIManager.UICamera;
                //            }
                //        }
                //    }
                //    else
                //    {
                //        Debug.LogError(string.Format("加载UITask  {0} 的BasePrefab{1} 失败:", intent.TargetTaskName, path));
                //    }
                //});
            }
        }
        
        private void LoadDynamicRes()
        {
            var resList = DynamicResPath();
            if (resList == null) return;

            resList.Sort((UIResData x, UIResData y) =>
            {
                if (x.Priority > y.Priority)
                    return -1;
                else if (x.Priority < y.Priority)
                    return 1;
                else
                    return 0;
            });

            var count = 0;
            GameObject go;
            for (var i = 0; i < resList.Count; i++)
            {
                var resData = resList[i];
                var data = resData;
                // 动态资源 避免同一帧全部开始
                //ResourcesLoader.Load<GameObject>(data.ResPath, prefab =>
                //{
                //    //// 单个资源加载后的Cache和CallBack
                //    //AssetUtil.AddAssetCache_New(resData.ResPath, prefab);
                //    var parent = UIManager.UIRoot_UsedLayer;
                //    if (data.Parent != null) parent = data.Parent;
                //    go = UnityEngine.GameObject.Instantiate(prefab, parent, false);
                //    var canvas = go.GetComponent<Canvas>();
                //    // UICamera指定
                //    if (canvas != null)
                //    {
                //        canvas.worldCamera = UIManager.UICamera;
                //    }

                //    if (data.OnLoadOver != null)
                //    {
                //        data.OnLoadOver(data.ResPath, go);
                //    }

                //    // 全部资源加载情况
                //    count++;
                //    if (count >= resList.Count)
                //    {
                //        OnDynamicResAllLoadOver();
                //    }
                //});
            }
        }
        
        protected virtual string StaticPrefabPath()
        {
            return string.Empty;
        }

        protected virtual List<UIResData> DynamicResPath()
        {
            return null;
        }

        /// <summary>
        /// 全部动态资源加载结束
        /// </summary>
        protected virtual void OnDynamicResAllLoadOver()
        {
            dynamicPrefabIsLoad = true;
        }

        private void DoGameObjectCreate()
        {
            OnGameObjectCreate();
        }

        protected virtual void OnGameObjectCreate()
        {
        }

        /// <summary>
        /// 启动更新显示
        /// </summary>
        protected virtual void StartUpdateView()
        {

        }

        /// <summary>
        /// 更新数据缓存
        /// </summary>
        protected virtual void UpdateDataCache()
        {
            // do nothing
        }

        /// <summary>
        /// 清理所有现场
        /// </summary>
        protected virtual void ClearAllContextAndRes()
        {
            // 请销毁 实例化的对象和缓存的数据

            staticPrefabIsLoad = false;
            dynamicPrefabIsLoad = false;

            if (mWindow != null)
            {
                // 经过测试 Dispose 只是将对应GameObject回收
                mWindow.Dispose();
            }

            if (Go != null)
            {
                GameObject.Destroy(Go);
            }
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 这个uiTask是否需要使用暂停超时机制
        /// </summary>
        public bool IsNeedPauseTimeOut { get; private set; }

        private bool StaticResIsSucc = false;

        #region 子UITask关联

        //子UITask 必然随着本界面的pause一起pause，激活则一起激活
        private List<UITaskBase> m_ChildUITask = new List<UITaskBase>();

        /// <summary>
        /// 关联一个子UITask，记住关闭时要配套使用RemoveChildUITask来移除，否则界面管理会异常
        /// </summary>
        /// <param name="uitask"></param>
        public void AddChildUITask(UITaskBase uitask)
        {
            if (m_ChildUITask.IndexOf(uitask) >= 0)
                m_ChildUITask.Remove(uitask);
            m_ChildUITask.Add(uitask);
        }
        /// <summary>
        /// 移除一个子UITask
        /// </summary>
        /// <param name="uitask"></param>
        public void RemoveChildUITask(UITaskBase uitask)
        {
            if (m_ChildUITask.IndexOf(uitask) >= 0)
                m_ChildUITask.Remove(uitask);
        }

        #endregion 子UITask关联
    }

    public class UIResData
    {
        public string ResPath;
        public Action<string, GameObject> OnLoadOver;
        public int Priority;
        public Transform Parent;

        public UIResData(string resPath, Action<string, GameObject> onLoadOver = null, int priority = 0,Transform parent = null)
        {
            ResPath = resPath;
            OnLoadOver = onLoadOver;
            this.Priority = priority;
            Parent = parent;
        }
    }
}


using System;
using System.Collections.Generic;

namespace MaxGame.UI
{
    public class UIIntent
    {
        public UIIntent(string targetTaskName, Type type,string targetMode = null)
        {
            TargetTaskName = targetTaskName;
            TargetMode = targetMode;
            TaskType = type;
        }

        /// <summary>
        /// 想要开启的task目标
        /// </summary>
        public string TargetTaskName { get; private set; }

        public Type TaskType;

        /// <summary>
        /// 需要开启的模式
        /// </summary>
        public string TargetMode { get; set; }
    }

    /// <summary>
    /// 带有一个params字典的UIIntent
    /// </summary>
    public class UIIntentCustom : UIIntent
    {
        public UIIntentCustom(string targetTaskName, Type type,string targetMode = null)
            : base(targetTaskName, type, targetMode)
        {
        }

        public void SetParam(string key, Object value)
        {
            m_params[key] = value;
        }
        public bool TryGetParam(string key, out Object value)
        {
            return m_params.TryGetValue(key, out value);
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="uiIntent"></param>
        /// <returns></returns>
        public T GetClassParam<T>(string key) where T : class
        {
            System.Object o;
            if (TryGetParam(key, out o))
            {
                return o as T;
            }
            return default(T);
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="uiIntent"></param>
        /// <returns></returns>
        public T GetStructParam<T>(string key) where T : struct
        {
            System.Object o;
            if (TryGetParam(key, out o))
            {
                return (T)o;
            }
            return default(T);
        }


        private Dictionary<string, Object> m_params = new Dictionary<string, object>();

        // 2021.5.19 zhengzhe: Add to print the params
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
            sb.Append("UIIntentCustom 参数列表:\n");
            foreach(var kv in m_params)
            {
                sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// 可返回上一个Task的UIIntent
    /// </summary>
    public class UIIntentReturnable : UIIntentCustom
    {
        public UIIntentReturnable(UIIntent prevTaskIntent, string targetTaskName, string targetMode = null)
            : base(targetTaskName, prevTaskIntent.TaskType, targetMode)
        {
            PrevTaskIntent = prevTaskIntent;
        }

        public UIIntent PrevTaskIntent { set; get; }
    }
}

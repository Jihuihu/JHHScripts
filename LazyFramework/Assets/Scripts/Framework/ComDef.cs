namespace MaxGame.UI
{
    internal class ComDef
    {
        public enum UIGroup
        {
            Entry = 0,
            Login,
            ReLogin,
            /// <summary>
            /// 目前只有MainUITask
            /// </summary>
            Common,
            /// <summary>
            /// 目前除战斗外的UI均为WorldGroup
            /// </summary>
            World,
            Battle,
        }
    }
}
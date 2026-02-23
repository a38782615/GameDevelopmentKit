namespace ET.Client
{
    /// <summary>
    /// GASHost 已废弃 - 技能系统现在由 ET 的 IUpdate 驱动
    /// AbilitySystemComponent 作为 ET Component 挂载在 Unit 上
    /// 不再需要 MonoBehaviour 单例驱动 Tick
    /// 
    /// 保留此文件避免编译错误，后续清理引用后可删除
    /// </summary>
    [System.Obsolete("GASHost 已废弃，技能系统由 ET IUpdate 驱动。请移除对 GASHost 的引用。")]
    public class GASHost : UnityEngine.MonoBehaviour
    {
        [StaticField]
        private static GASHost _instance;
        public static GASHost Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new UnityEngine.GameObject("[GASHost_Deprecated]");
                    _instance = go.AddComponent<GASHost>();
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public float TimeScale { get; set; } = 1f;
        public bool IsPaused { get; set; }

        [System.Obsolete("不再需要注册ASC")]
        public void Register(AbilitySystemComponent asc) { }

        [System.Obsolete("不再需要注销ASC")]
        public void Unregister(AbilitySystemComponent asc) { }

        public void ClearAll() { }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}

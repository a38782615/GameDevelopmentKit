
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 投射物效果Spec
    /// 负责生成投射物并管理其生命周期
    /// 注意：这是一个特殊的Effect，生命周期由投射物控制
    /// </summary>
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class ProjectileEffectSpec : Entity, IAwake
    {
        public ProjectileController _projectileController;
        public GameObject _projectileObject;
    }
}

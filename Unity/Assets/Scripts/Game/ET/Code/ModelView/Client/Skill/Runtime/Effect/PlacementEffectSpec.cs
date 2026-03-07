
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 放置物效果Spec
    /// 负责生成放置物并管理其生命周期
    /// 支持进入/离开/停留三种事件
    /// </summary>
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class PlacementEffectSpec : Entity, IAwake
    {
        public PlacementController _placementController;
        public GameObject _placementObject;

    }


}

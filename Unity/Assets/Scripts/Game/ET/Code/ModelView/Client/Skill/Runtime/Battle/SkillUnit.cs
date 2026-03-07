using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// SkillUnit - 战斗单位的 MonoBehaviour 桥接
    /// 现在通过 ET 的 Unit Entity 创建 AbilitySystemComponent
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [ComponentOf(typeof(Unit))]
    public partial class SkillUnit : Entity, IAwake
    {
        public EntityRef<Unit> Unit => this.GetParent<Unit>();
        public EntityRef<AbilitySystemComponent> ASC => GetComponent<AbilitySystemComponent>();

        public AbilitySystemComponent ownerASC => this.ASC.As();
    }
}

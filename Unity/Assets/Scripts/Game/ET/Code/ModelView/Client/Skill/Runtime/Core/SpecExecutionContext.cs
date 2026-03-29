using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Skill 执行上下文，只保留运行时逻辑数据，不缓存表现对象引用。
    /// </summary>
    [ChildOf(typeof(GameplayAbilitySpec))]
    public partial class SpecExecutionContext : Entity, IAwake, IDestroy
    {
        public EntityRef<GameplayEffectSpec> OwnerEffectSpec;
        public EntityRef<AbilitySystemComponent> Caster;
        public List<EntityRef<AbilitySystemComponent>> Targets = new List<EntityRef<AbilitySystemComponent>>();
        public EntityRef<AbilitySystemComponent> MainTarget;
        public EntityRef<AbilitySystemComponent> ParentInputTarget;
        public int AbilityLevel = 1;
        public int StackCount = 1;
        public Dictionary<string, object> CustomData = new Dictionary<string, object>();
    }
}

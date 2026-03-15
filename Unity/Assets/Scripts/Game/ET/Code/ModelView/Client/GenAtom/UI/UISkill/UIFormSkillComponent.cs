using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormSkillComponent : UGFUIForm<MonoUIFormSkill>, IAwake, IUGFUIFormOnOpen, IUGFUIFormOnClose, IUGFUIFormOnUpdate
    {
        public readonly List<EntityRef<GameplayAbilitySpec>> SkillSpecs = new List<EntityRef<GameplayAbilitySpec>>();
        public readonly List<MonoUISkillItem> SkillItems = new List<MonoUISkillItem>();
        public readonly Dictionary<int, EntityRef<SkillCellComponent>> SkillCellMap = new Dictionary<int, EntityRef<SkillCellComponent>>();
        public float ListSyncLeftTime;
#if UNITY_EDITOR
        public int EditorSmokeRunId;
        public bool EditorSmokeTriggered;
        public float EditorSmokeReportLeftTime = -1f;
        public bool EditorSmokeResultLogged;
        public string EditorSmokeSkillLabel;
        public string EditorSmokeStateOverrideText;
        public float EditorSmokeStateOverrideLeftTime;
        public EntityRef<GameplayAbilitySpec> EditorSmokeSpec;
#endif
    }
}

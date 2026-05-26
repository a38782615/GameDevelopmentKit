using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormSkill : UGFUIForm<MonoUIFormSkill>, IAwake, IUGFUIFormOnOpen, IUGFUIFormOnClose, IUGFUIFormOnUpdate
    {
        public readonly List<EntityRef<SkillCardRuntime>> SkillCards = new List<EntityRef<SkillCardRuntime>>();
        public readonly List<MonoUISkillItem> SkillItems = new List<MonoUISkillItem>();
        public readonly Dictionary<int, EntityRef<UIWidgetSkillItem>> SkillCellMap = new Dictionary<int, EntityRef<UIWidgetSkillItem>>();
        public float ListSyncLeftTime;
        public bool IsRerenderingMap;
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

using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(SkillUnit))]
    public partial class SkillCardDeckComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public long NextCardInstanceId = 1;
        public int BattleCardConfigId;
        public int SkillCardRuleId;
        public int DrawCount;
        public int HandLimit;
        public float InitMp;
        public float CycleSeconds;
        public float CurrentCycleTime;
        public float MoveDrainMpPerSecond;
        public float CurrentMoveDrainTime;
        public float PassiveTriggerIntervalSeconds;
        public float PassiveTriggerElapsed;
        public bool IsMoveDraining;
        public List<long> DrawPileCardIds = new List<long>();
        public List<long> HandCardIds = new List<long>();
        public List<long> AbilityCardIds = new List<long>();
        public List<long> DiscardPileCardIds = new List<long>();
        public List<long> DestroyedCardIds = new List<long>();
    }
}

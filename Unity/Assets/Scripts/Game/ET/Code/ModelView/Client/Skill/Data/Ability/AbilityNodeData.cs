using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace ET.Client
{
    [Serializable]
    public class TimeEffectData : Object
    {
        public int triggerTime = 0;
        public int portIdValue;
        [FormerlySerializedAs("portId")]
        public string legacyPortId;

        public TimeEffectData()
        {
            this.portIdValue = SkillPortIdUtility.ResolveAnimationTrackPortId(Guid.NewGuid().ToString());
        }

        public int PortId
        {
            get
            {
                if (!string.IsNullOrEmpty(this.legacyPortId))
                {
                    this.portIdValue = SkillPortIdUtility.ResolveAnimationTrackPortId(this.legacyPortId);
                    return this.portIdValue;
                }

                if (this.portIdValue > SkillPortId.Invalid)
                {
                    return this.portIdValue;
                }

                this.portIdValue = SkillPortIdUtility.ResolveAnimationTrackPortId(Guid.NewGuid().ToString());
                return this.portIdValue;
            }
            set
            {
                this.portIdValue = value;
                this.legacyPortId = null;
            }
        }
    }

    [Serializable]
    public class TimeCueData : Object
    {
        public int startTime = 0;
        public int endTime = 5;
        public int portIdValue;
        [FormerlySerializedAs("portId")]
        public string legacyPortId;

        public TimeCueData()
        {
            this.portIdValue = SkillPortIdUtility.ResolveAnimationTrackPortId(Guid.NewGuid().ToString());
        }

        public int PortId
        {
            get
            {
                return this.portIdValue;
            }
            set
            {
                this.portIdValue = value;
                this.legacyPortId = null;
            }
        }
    }

    [Serializable]
    public class AbilityNodeData : NodeData
    {
        public int skillId = 0;
        public GameplayTagSet assetTags;
        public GameplayTagSet cancelAbilitiesWithTags;
        public GameplayTagSet blockAbilitiesWithTags;
        public GameplayTagSet activationOwnedTags;
        public GameplayTagSet activationRequiredTags;
        public GameplayTagSet activationBlockedTags;
        public GameplayTagSet ongoingBlockedTags;
        public List<AbilityEventPortData> eventOutputPorts = new List<AbilityEventPortData>();
    }

    [Serializable]
    public class AbilityEventPortData : Object
    {
        public GameplayEventType eventType = GameplayEventType.OnHit;
        public string customEventTag = "";
        public int PortId;
    }
}

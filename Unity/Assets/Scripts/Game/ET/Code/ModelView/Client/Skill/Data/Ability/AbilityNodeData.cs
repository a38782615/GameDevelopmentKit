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
        public int portIdValue;
        [FormerlySerializedAs("portId")]
        public string legacyPortId;
        public string customEventTag = "";

        public int PortId
        {
            get
            {
                if (!string.IsNullOrEmpty(this.legacyPortId))
                {
                    this.portIdValue = SkillPortIdUtility.ResolveAbilityEventPortId(this.legacyPortId);
                    return this.portIdValue;
                }

                if (this.portIdValue > SkillPortId.Invalid)
                {
                    return this.portIdValue;
                }

                this.portIdValue = SkillPortIdUtility.ResolveAbilityEventPortId(this.eventType, this.customEventTag);
                return this.portIdValue;
            }
            set
            {
                this.portIdValue = value;
                this.legacyPortId = null;
            }
        }
    }

    public struct AbilityTagContainer
    {
        public GameplayTagSet AssetTags;
        public GameplayTagSet CancelAbilitiesWithTags;
        public GameplayTagSet BlockAbilitiesWithTags;
        public GameplayTagSet ActivationOwnedTags;
        public GameplayTagSet ActivationRequiredTags;
        public GameplayTagSet ActivationBlockedTags;
        public GameplayTagSet OngoingBlockedTags;

        public AbilityTagContainer(AbilityNodeData data)
        {
            this.AssetTags = data.assetTags;
            this.CancelAbilitiesWithTags = data.cancelAbilitiesWithTags;
            this.BlockAbilitiesWithTags = data.blockAbilitiesWithTags;
            this.ActivationOwnedTags = data.activationOwnedTags;
            this.ActivationRequiredTags = data.activationRequiredTags;
            this.ActivationBlockedTags = data.activationBlockedTags;
            this.OngoingBlockedTags = data.ongoingBlockedTags;
        }

        public AbilityTagContainer(
            GameplayTagSet assetTags,
            GameplayTagSet cancelAbilityTags,
            GameplayTagSet blockAbilityTags,
            GameplayTagSet activationOwnedTags,
            GameplayTagSet activationRequiredTags,
            GameplayTagSet activationBlockedTags,
            GameplayTagSet ongoingBlockedTags)
        {
            this.AssetTags = assetTags;
            this.CancelAbilitiesWithTags = cancelAbilityTags;
            this.BlockAbilitiesWithTags = blockAbilityTags;
            this.ActivationOwnedTags = activationOwnedTags;
            this.ActivationRequiredTags = activationRequiredTags;
            this.ActivationBlockedTags = activationBlockedTags;
            this.OngoingBlockedTags = ongoingBlockedTags;
        }
    }
}

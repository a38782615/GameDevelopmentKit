
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 飘字Cue Spec
    /// 显示伤害、治疗、状态等飘字
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.GameplayCueSpec))]
    public partial class SoundCueSpecHandler : ACueHandler
    {
        public SoundCueNodeData GetNode()
        {
            var nodeData = NodeData as SoundCueNodeData;
            return nodeData;
        }
        public SoundCueSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<SoundCueSpec>();
            if (selfSpec == null)
            {
                Spec.AddComponent<SoundCueSpec>();
            }
            return selfSpec;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }
        // ============ 初始化 ============

        public override void OnInitialize()
        {
            var selfSpec = SelfSpec();
            var nodeData = GetNode();
            if (nodeData != null)
            {
                selfSpec.SoundClip = nodeData.soundClip;
                selfSpec.SoundVolume = nodeData.soundVolume;
                selfSpec.SoundLoop = nodeData.soundLoop;
                Spec.DestroyWithNode = nodeData.destroyWithNode;
            }
        }

        // ============ 执行 ============

        public override void PlayCue(AbilitySystemComponent target)
        {
            var nodeData = GetNode();
            if (nodeData == null)
                return;

            var source = Spec.GetCueTarget();

            // 播放音效
            Spec.ActiveCue = GameplayCueManager.Instance.PlaySoundCue(nodeData, source, target);

            if (Spec.ActiveCue != null)
            {
                Spec.IsRunning = true;
            }
        }

        public override void StopCue()
        {
            if (Spec.ActiveCue != null)
            {
                GameplayCueManager.Instance.StopCue(Spec.ActiveCue);
                Spec.ActiveCue = null;
            }
        }

        public override void Reset()
        {
            var selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                return;
            }
            selfSpec.SoundClip = null;
            selfSpec.SoundVolume = 1f;
            selfSpec.SoundLoop = false;
            Spec.DestroyWithNode = false;
        }
    }
}
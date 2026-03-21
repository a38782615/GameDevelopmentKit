using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GameplayCueSpec))]
    public partial class SoundCueSpecHandler : ACueHandler
    {
        public SoundCueNodeData GetNode()
        {
            return NodeData as SoundCueNodeData;
        }

        public SoundCueSpec SelfSpec()
        {
            SoundCueSpec selfSpec = Spec.GetComponent<SoundCueSpec>();
            if (selfSpec == null)
            {
                selfSpec = Spec.AddComponent<SoundCueSpec>();
            }

            return selfSpec;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override void OnInitialize()
        {
            SoundCueSpec selfSpec = SelfSpec();
            SoundCueNodeData nodeData = GetNode();
            if (nodeData == null)
            {
                return;
            }

            selfSpec.SoundClip = nodeData.soundClip;
            selfSpec.SoundVolume = nodeData.soundVolume;
            selfSpec.SoundLoop = nodeData.soundLoop;
            Spec.DestroyWithNode = nodeData.destroyWithNode;
        }

        public override void PlayCue(AbilitySystemComponent target)
        {
            SoundCueNodeData nodeData = GetNode();
            if (nodeData == null)
            {
                return;
            }

            GameplayCueManager manager = Spec.GetGameplayCueManager();
            if (manager == null)
            {
                return;
            }

            AbilitySystemComponent source = Spec.GetCueTarget();
            Spec.ActiveCue = manager.PlaySoundCue(nodeData, source, target);
            if (Spec.ActiveCue != null)
            {
                Spec.IsRunning = true;
            }
        }

        public override void StopCue()
        {
            if (Spec.ActiveCue == null)
            {
                return;
            }

            GameplayCueManager manager = Spec.GetGameplayCueManager();
            if (manager != null)
            {
                manager.StopCue(Spec.ActiveCue);
            }
            else
            {
                Spec.ActiveCue.Stop();
            }

            Spec.ActiveCue = null;
        }

        public override void Reset()
        {
            SoundCueSpec selfSpec = SelfSpec();
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

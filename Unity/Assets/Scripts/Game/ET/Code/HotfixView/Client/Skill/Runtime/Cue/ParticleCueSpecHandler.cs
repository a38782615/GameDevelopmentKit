using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GameplayCueSpec))]
    public partial class ParticleCueSpecHandler : ACueHandler
    {
        public ParticleCueNodeData GetNode()
        {
            return NodeData as ParticleCueNodeData;
        }

        public ParticleCueSpec SelfSpec()
        {
            ParticleCueSpec selfSpec = Spec.GetComponent<ParticleCueSpec>();
            if (selfSpec == null)
            {
                selfSpec = Spec.AddComponent<ParticleCueSpec>();
            }

            return selfSpec;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override void OnInitialize()
        {
            ParticleCueSpec selfSpec = SelfSpec();
            ParticleCueNodeData nodeData = GetNode();
            if (nodeData == null)
            {
                return;
            }

            selfSpec.ParticleEntityId = nodeData.particleEntityId;
            selfSpec.PositionSource = nodeData.positionSource;
            selfSpec.ParticleBindingName = nodeData.particleBindingName;
            selfSpec.ParticleOffset = nodeData.particleOffset;
            selfSpec.ParticleScale = nodeData.particleScale;
            selfSpec.AttachToTarget = nodeData.attachToTarget;
            selfSpec.ParticleLoop = nodeData.particleLoop;
            Spec.DestroyWithNode = nodeData.destroyWithNode;
        }

        public override void PlayCue(AbilitySystemComponent target)
        {
            ParticleCueNodeData nodeData = GetNode();
            ParticleCueSpec selfSpec = SelfSpec();
            SpecExecutionContext context = GetContext();
            if (nodeData == null || selfSpec == null || context == null)
            {
                return;
            }

            GameObject sourceObject = context.GetSourceObject(selfSpec.PositionSource);
            float facingDirection = 1f;
            if (sourceObject != null)
            {
                facingDirection = sourceObject.transform.localScale.x >= 0 ? -1f : 1f;
            }

            Vector3 position = context.GetPosition(selfSpec.PositionSource, selfSpec.ParticleBindingName);
            Vector3 adjustedOffset = selfSpec.ParticleOffset;
            adjustedOffset.x *= facingDirection;
            position += adjustedOffset;

            Vector3 adjustedScale = selfSpec.ParticleScale;
            adjustedScale.x *= facingDirection;

            Transform attachTransform = null;
            if (selfSpec.AttachToTarget && sourceObject != null)
            {
                if (!string.IsNullOrEmpty(selfSpec.ParticleBindingName))
                {
                    Transform bindingPoint = sourceObject.transform.Find(selfSpec.ParticleBindingName);
                    attachTransform = bindingPoint ?? sourceObject.transform;
                }
                else
                {
                    attachTransform = sourceObject.transform;
                }
            }

            GameplayCueManager manager = Spec.GetGameplayCueManager();
            if (manager == null)
            {
                return;
            }

            Spec.ActiveCue = manager.PlayParticleCue(
                selfSpec.ParticleEntityId,
                position,
                adjustedScale,
                attachTransform,
                selfSpec.ParticleLoop);

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
            ParticleCueSpec selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                return;
            }

            selfSpec.ParticleEntityId = 0;
            selfSpec.PositionSource = PositionSourceType.ParentInput;
            selfSpec.ParticleBindingName = string.Empty;
            selfSpec.ParticleOffset = Vector3.zero;
            selfSpec.ParticleScale = Vector3.one;
            selfSpec.AttachToTarget = true;
            selfSpec.ParticleLoop = false;
            Spec.DestroyWithNode = false;
        }
    }
}

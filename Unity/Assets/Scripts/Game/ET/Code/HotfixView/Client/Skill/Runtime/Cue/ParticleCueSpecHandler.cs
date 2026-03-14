
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 飘字Cue Spec
    /// 显示伤害、治疗、状态等飘字
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.GameplayCueSpec))]
    public partial class ParticleCueSpecHandler : ACueHandler
    {
        public ParticleCueNodeData GetNode()
        {
            var nodeData = NodeData as ParticleCueNodeData;
            return nodeData;
        }
        public ParticleCueSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<ParticleCueSpec>();
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
        // ============ 初始化 ============

        public override void OnInitialize()
        {
            var selfSpec = SelfSpec();
            var nodeData = GetNode();
            if (nodeData != null)
            {
                selfSpec.ParticlePrefab = nodeData.particlePrefab;
                selfSpec.PositionSource = nodeData.positionSource;
                selfSpec.ParticleBindingName = nodeData.particleBindingName;
                selfSpec.ParticleOffset = nodeData.particleOffset;
                selfSpec.ParticleScale = nodeData.particleScale;
                selfSpec.AttachToTarget = nodeData.attachToTarget;
                selfSpec.ParticleLoop = nodeData.particleLoop;
                Spec.DestroyWithNode = nodeData.destroyWithNode;
            }
        }

        // ============ 执行 ============

        public override void PlayCue(AbilitySystemComponent target)
        {
            var nodeData = GetNode();
            if (nodeData == null)
                return;
            var selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                return;
            }
            var Context = GetContext();

            // 获取位置来源对象，用于确定朝向和挂点
            var sourceObject = Context.GetSourceObject(selfSpec.PositionSource);

            // 获取朝向（2D角色通过localScale.x判断：>=0朝左，<0朝右）
            float facingDirection = 1f;
            if (sourceObject != null)
            {
                facingDirection = sourceObject.transform.localScale.x >= 0 ? 1f : -1f;
            }

            // 使用 PositionSourceType 获取播放位置，根据朝向翻转X偏移
            Vector3 position = Context.GetPosition(selfSpec.PositionSource, selfSpec.ParticleBindingName);
            Vector3 adjustedOffset = selfSpec.ParticleOffset;
            adjustedOffset.x *= facingDirection;
            position += adjustedOffset;

            // 根据朝向翻转X缩放
            Vector3 adjustedScale = selfSpec.ParticleScale;
            adjustedScale.x *= facingDirection;

            // 获取附着的 Transform（如果需要附着）
            Transform attachTransform = null;
            if (selfSpec.AttachToTarget)
            {
                if (sourceObject != null)
                {
                    // 如果有挂点，附着到挂点
                    if (!string.IsNullOrEmpty(selfSpec.ParticleBindingName))
                    {
                        var bindingPoint = sourceObject.transform.Find(selfSpec.ParticleBindingName);
                        attachTransform = bindingPoint ?? sourceObject.transform;
                    }
                    else
                    {
                        attachTransform = sourceObject.transform;
                    }
                }
            }

            // 播放粒子特效
            Spec.ActiveCue = GameplayCueManager.GetOrCreate().PlayParticleCue(
                nodeData.particlePrefab,
                position,
                adjustedScale,
                attachTransform,
                selfSpec.ParticleLoop
            );

            if (Spec.ActiveCue != null)
            {
                Spec.IsRunning = true;
            }
        }

        public override void StopCue()
        {
            if (Spec.ActiveCue != null)
            {
                GameplayCueManager.GetOrCreate().StopCue(Spec.ActiveCue);
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
            selfSpec.ParticlePrefab = null;
            selfSpec.PositionSource = PositionSourceType.ParentInput;
            selfSpec.ParticleBindingName = "";
            selfSpec.ParticleOffset = Vector3.zero;
            selfSpec.ParticleScale = Vector3.one;
            selfSpec.AttachToTarget = true;
            selfSpec.ParticleLoop = false;
            Spec.DestroyWithNode = false;
        }
    }
}

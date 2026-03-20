
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 放置物效果 Spec。
    /// 负责生成放置物并管理其生命周期，支持进入和离开事件。
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.PlacementEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    public partial class PlacementEffectSpecHandler : AEffectHandler
    {
        public PlacementEffectSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<PlacementEffectSpec>();
            return selfSpec;
        }

        public PlacementEffectNodeData GetNode()
        {
            var nodeData = NodeData as PlacementEffectNodeData;
            return nodeData;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override void Execute()
        {
            Spec.Execute();
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return GetContext();
        }

        public override void OnCompleteHook()
        {
        }

        public override void OnInitialize()
        {
        }

        public override void OnPeriodicHook()
        {
        }

        public override void Reset()
        {
            Spec.ResetEffect();
        }

        public override void Tick(float deltaTime)
        {
            Spec.TickEffect(deltaTime);
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
            var selfSpec = SelfSpec();
            var nodeData = GetNode();
            if (nodeData == null)
            {
                return;
            }

            var context = GetContext();
            if (context == null)
            {
                return;
            }

            Vector3 position = context.GetPosition(nodeData.positionSource, nodeData.positionBindingName);
            SpawnPlacement(position);
            context.SetPlacementObject(selfSpec._placementObject);
        }

        private void SpawnPlacement(Vector3 position)
        {
            var selfSpec = SelfSpec();
            var nodeData = GetNode();

            if (selfSpec == null || nodeData == null)
            {
                return;
            }

            if (nodeData.placementPrefab != null)
            {
                selfSpec._placementObject = UnityEngine.Object.Instantiate(nodeData.placementPrefab, position, UnityEngine.Quaternion.identity);
            }
            else
            {
                selfSpec._placementObject = new GameObject("Placement");
                selfSpec._placementObject.transform.position = position;
            }

            if (!nodeData.enableCollision)
            {
                return;
            }

            selfSpec._placementController = selfSpec._placementObject.GetComponent<PlacementController>();
            if (selfSpec._placementController == null)
            {
                selfSpec._placementController = selfSpec._placementObject.AddComponent<PlacementController>();
            }

            selfSpec._placementController.Initialize(new PlacementInitData
            {
                CollisionRadius = nodeData.collisionRadius,
                CollisionTargetTags = nodeData.collisionTargetTags,
                CollisionExcludeTags = nodeData.collisionExcludeTags,
                SourceASC = Spec.Source
            });

            selfSpec._placementController.OnEnter += OnTargetEnter;
            selfSpec._placementController.OnExit += OnTargetExit;
        }

        private void OnTargetEnter(AbilitySystemComponent target)
        {
            if (target == null || Spec == null || Spec.IsDisposed)
            {
                return;
            }

            SpecExecutionContext context = GetContext();
            if (context == null)
            {
                return;
            }

            SpecExecutionContext ctx = context.CreateWithParentInput(target);
            if (ctx == null)
            {
                return;
            }

            try
            {
                ctx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.PlacementEffect.OnEnter);
            }
            finally
            {
                ctx.Dispose();
            }
        }

        private void OnTargetExit(AbilitySystemComponent target)
        {
            ExecuteExitFlow(target);
        }

        public override void Cancel()
        {
            var selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                if (Spec != null && !Spec.IsDisposed)
                {
                    Spec.CancelEffect();
                }

                return;
            }

            PlacementController controller = selfSpec._placementController;
            if (controller != null)
            {
                // Unsubscribe before replaying exit flow to avoid re-entering Cancel.
                controller.OnEnter -= OnTargetEnter;
                controller.OnExit -= OnTargetExit;
                selfSpec._placementController = null;

                foreach (AbilitySystemComponent target in controller.GetCurrentTargets())
                {
                    ExecuteExitFlow(target);
                }

                controller.ClearAllTargets();
            }

            if (selfSpec._placementObject != null)
            {
                UnityEngine.Object.Destroy(selfSpec._placementObject);
                selfSpec._placementObject = null;
            }

            Spec.CancelEffect();
        }

        private void ExecuteExitFlow(AbilitySystemComponent target)
        {
            if (target == null || Spec == null || Spec.IsDisposed)
            {
                return;
            }

            SpecExecutionContext context = GetContext();
            if (context == null)
            {
                return;
            }

            SpecExecutionContext ctx = context.CreateWithParentInput(target);
            if (ctx == null)
            {
                return;
            }

            try
            {
                ctx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.PlacementEffect.OnExit);
            }
            finally
            {
                ctx.Dispose();
            }
        }
    }
}

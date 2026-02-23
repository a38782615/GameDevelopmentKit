

using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 放置物效果Spec
    /// 负责生成放置物并管理其生命周期
    /// 支持进入/离开/停留三种事件
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
            if (nodeData == null) return;
            var Context = GetContext();
            // 使用 PositionSourceType 获取放置位置
            Vector3 position = Context.GetPosition(nodeData.positionSource, nodeData.positionBindingName);

            // 生成放置物
            SpawnPlacement(position);

            // 将放置物对象设置到上下文中，供子节点使用
            Context.PlacementObject = selfSpec._placementObject;
        }

        /// <summary>
        /// 生成放置物
        /// </summary>
        private void SpawnPlacement(Vector3 position)
        {
            var selfSpec = SelfSpec();
            var nodeData = GetNode();

            // 创建放置物GameObject
            if (nodeData.placementPrefab != null)
            {
                selfSpec._placementObject = UnityEngine.Object.Instantiate(nodeData.placementPrefab, position, UnityEngine.Quaternion.identity);
            }
            else
            {
                // 没有预制体时创建一个简单的GameObject
                selfSpec._placementObject = new GameObject("Placement");
                selfSpec._placementObject.transform.position = position;
            }

            // 如果启用碰撞，添加控制器
            if (nodeData.enableCollision)
            {
                selfSpec._placementController = selfSpec._placementObject.GetComponent<PlacementController>();
                if (selfSpec._placementController == null)
                {
                    selfSpec._placementController = selfSpec._placementObject.AddComponent<PlacementController>();
                }

                // 初始化控制器
                selfSpec._placementController.Initialize(new PlacementInitData
                {
                    CollisionRadius = nodeData.collisionRadius,
                    CollisionTargetTags = nodeData.collisionTargetTags,
                    CollisionExcludeTags = nodeData.collisionExcludeTags,
                    SourceASC = Spec.Source
                });

                // 注册事件
                selfSpec._placementController.OnEnter += OnTargetEnter;
                selfSpec._placementController.OnExit += OnTargetExit;
            }
        }

        /// <summary>
        /// 目标进入回调
        /// </summary>
        private void OnTargetEnter(AbilitySystemComponent target)
        {
            if (target == null) return;
            var Context = GetContext();
            // 创建带有目标的上下文
            var ctx = Context.CreateWithParentInput(target);

            // 执行进入时端口
            ctx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, "进入时");
        }

        /// <summary>
        /// 目标离开回调
        /// </summary>
        private void OnTargetExit(AbilitySystemComponent target)
        {
            if (target == null) return;

            var Context = GetContext();
            // 创建带有目标的上下文
            var ctx = Context.CreateWithParentInput(target);

            // 执行离开时端口
            ctx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, "离开时");
        }


        /// <summary>
        /// Effect取消时清理放置物
        /// </summary>
        public override void Cancel()
        {
            var selfSpec = SelfSpec();
            if (selfSpec._placementController != null)
            {
                // 触发所有当前目标的离开事件
                selfSpec._placementController.TriggerAllExitEvents();

                // 取消事件订阅
                selfSpec._placementController.OnEnter -= OnTargetEnter;
                selfSpec._placementController.OnExit -= OnTargetExit;
                selfSpec._placementController = null;
            }

            if (selfSpec._placementObject != null)
            {
                UnityEngine.Object.Destroy(selfSpec._placementObject);
                selfSpec._placementObject = null;
            }
            Spec.CancelEffect();
        }
    }
}

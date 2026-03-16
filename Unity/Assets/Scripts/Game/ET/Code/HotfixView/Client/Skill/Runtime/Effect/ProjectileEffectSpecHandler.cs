

using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 投射物效果Spec
    /// 负责生成投射物并管理其生命周期
    /// 注意：这是一个特殊的Effect，生命周期由投射物控制
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.ProjectileEffectSpec))]
    public partial class ProjectileEffectSpecHandler : AEffectHandler
    {
        public ProjectileEffectSpec SelfSpec()
        {
            if (Spec == null || Spec.IsDisposed)
            {
                return null;
            }

            var selfSpec = Spec.GetComponent<ProjectileEffectSpec>();
            return selfSpec;
        }
        public ProjectileEffectNodeData GetNode()
        {
            var nodeData = NodeData as ProjectileEffectNodeData;
            return nodeData;
        }
        public override SpecExecutionContext GetContext()
        {
            return Spec?.GetContext();
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

        public override void OnInitialize()
        {
            Spec.OnInitialize();

            // 强制设置为永久效果，生命周期由投射物控制
            Spec.Duration = -1f;
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
            var nodeData = GetNode();
            if (nodeData == null) return;
            var selfSpec = SelfSpec();
            var Context = GetContext();
            if (selfSpec == null || Context == null) return;
            // 使用 PositionSourceType 获取发射位置
            Vector2 launchPosition = Context.GetPosition(nodeData.launchPositionSource, nodeData.launchBindingName);

            // 使用 PositionSourceType 获取目标位置
            Vector2 targetPosition = Context.GetPosition(nodeData.targetPositionSource, nodeData.targetBindingName);

            // 获取目标单位（仅单位模式需要）
            AbilitySystemComponent targetUnit = null;
            if (nodeData.projectileTargetType == ProjectileTargetType.Unit)
            {
                // 根据目标位置来源获取对应的 ASC
                targetUnit = GetTargetUnitFromPositionSource(nodeData.targetPositionSource);
            }

            // 计算发射方向
            Vector2 direction = (targetPosition - launchPosition).normalized;

            // 应用偏移角度（仅点模式）
            if (nodeData.projectileTargetType == ProjectileTargetType.Position && Mathf.Abs(nodeData.offsetAngle) > 0.01f)
            {
                direction = RotateVector2(direction, -nodeData.offsetAngle);
            }

            // 生成投射物
            SpawnProjectile(launchPosition, targetPosition, direction, targetUnit);

            // 将投射物对象设置到上下文中，供子节点使用
            Context.ProjectileObject = selfSpec._projectileObject;
        }

        /// <summary>
        /// 根据 PositionSourceType 获取目标单位
        /// </summary>
        private AbilitySystemComponent GetTargetUnitFromPositionSource(PositionSourceType sourceType)
        {
            var Context = GetContext();
            switch (sourceType)
            {
                case PositionSourceType.Caster:
                    return Context.Caster;
                case PositionSourceType.MainTarget:
                    return Context.MainTarget;
                case PositionSourceType.ParentInput:
                    return Context.ParentInputTarget;
                default:
                    return Context.MainTarget;
            }
        }

        /// <summary>
        /// 生成投射物
        /// </summary>
        private void SpawnProjectile(Vector2 launchPosition, Vector2 targetPosition, Vector2 direction, AbilitySystemComponent targetUnit)
        {
            var nodeData = GetNode();
            var selfSpec = SelfSpec();
            if (selfSpec == null) return;
#if UNITY_EDITOR
#endif

            // 创建投射物GameObject
            if (nodeData.projectilePrefab != null)
            {
                selfSpec._projectileObject = UnityEngine.Object.Instantiate(nodeData.projectilePrefab, launchPosition, UnityEngine.Quaternion.identity);
            }
            else
            {
                // 没有预制体时创建一个简单的GameObject
                selfSpec._projectileObject = new GameObject("Projectile");
                selfSpec._projectileObject.transform.position = launchPosition;
            }

            // 添加或获取ProjectileController
            var controller = selfSpec._projectileObject.GetComponent<ProjectileController>();
            if (controller == null)
            {
                controller = selfSpec._projectileObject.AddComponent<ProjectileController>();
            }

            // 初始化投射物
            controller.Initialize(new ProjectileInitData
            {
                LaunchPosition = launchPosition,
                TargetPosition = targetPosition,
                Direction = direction,
                TargetUnit = targetUnit,
                TargetType = nodeData.projectileTargetType,
                FlyOver = nodeData.flyOver,
                CurveHeight = nodeData.curveHeight,
                Speed = nodeData.speed,
                MaxDistance = nodeData.maxDistance,
                CollisionRadius = nodeData.collisionRadius,
                IsPiercing = nodeData.isPiercing,
                MaxPierceCount = nodeData.maxPierceCount,
                CollisionTargetTags = nodeData.collisionTargetTags,
                CollisionExcludeTags = nodeData.collisionExcludeTags,
                TargetBindingName = nodeData.targetBindingName,
                SkillId = Spec.SkillId,
                NodeGuid = Spec.NodeGuid,
                Context = Spec.Context,
                SourceASC = Spec.Source,
                // 反弹设置
                IsBouncing = nodeData.isBouncing,
                BounceTargetMode = nodeData.bounceTargetMode,
                MaxBounceCount = nodeData.maxBounceCount,
                BounceSearchRadius = nodeData.bounceSearchRadius,
                CanBounceToSameTarget = nodeData.canBounceToSameTarget,
                ExcludeSourceCamp = nodeData.excludeSourceCamp,
                BounceAngleOffset = nodeData.bounceAngleOffset
            });

            // 保存引用
            selfSpec._projectileController = controller;
#if UNITY_EDITOR
#endif

            // 注册事件
            controller.OnHit += OnProjectileHit;
            controller.OnReachTarget += OnProjectileReachTarget;
            controller.OnBounce += OnProjectileBounce;
            controller.OnDestroy += OnProjectileDestroy;
        }

        /// <summary>
        /// 投射物命中回调
        /// </summary>
        private void OnProjectileHit(AbilitySystemComponent hitTarget, Vector2 hitPosition)
        {
            if (hitTarget == null) return;
            var Context = GetContext();
            var selfSpec = SelfSpec();
            if (Context == null || selfSpec == null) return;
            // 创建带有命中目标的上下文
            var hitContext = Context.CreateWithParentInput(hitTarget);
            if (hitContext == null) return;
            hitContext.SetCustomData("HitPosition", hitPosition);
            // 确保投射物对象在上下文中
            hitContext.ProjectileObject = selfSpec._projectileObject;

            // 执行碰撞时端口
            hitContext.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnHit);
        }

        /// <summary>
        /// 投射物到达目标回调
        /// </summary>
        private void OnProjectileReachTarget(Vector2 position)
        {
            var ctx = GetExecutionContext();
            var selfSpec = SelfSpec();
            if (ctx == null || selfSpec == null) return;
            ctx.SetCustomData("ReachPosition", position);
            // 确保投射物对象在上下文中
            ctx.ProjectileObject = selfSpec._projectileObject;

            // 执行到达目标位置端口
            ctx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnReachTarget);
        }

        /// <summary>
        /// 投射物反弹回调
        /// </summary>
        private void OnProjectileBounce(AbilitySystemComponent nextTarget, Vector2 bouncePosition)
        {
            if (nextTarget == null) return;

            var Context = GetContext();
            var selfSpec = SelfSpec();
            if (Context == null || selfSpec == null) return;
            // 创建带有反弹目标的上下文
            var bounceContext = Context.CreateWithParentInput(nextTarget);
            if (bounceContext == null) return;
            bounceContext.SetCustomData("BouncePosition", bouncePosition);
            bounceContext.ProjectileObject = selfSpec._projectileObject;

            // 执行反弹时端口
            bounceContext.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnBounce);
        }

        /// <summary>
        /// 投射物销毁回调
        /// </summary>
        private void OnProjectileDestroy()
        {
            var selfSpec = SelfSpec();
            if (selfSpec != null)
            {
                selfSpec._projectileController = null;
                selfSpec._projectileObject = null;
            }

            // 投射物销毁时，结束Effect
            if (Spec != null && !Spec.IsDisposed)
            {
                Spec.RemoveEffect();
            }
        }

        /// <summary>
        /// 取消Effect时，也要销毁投射物
        /// </summary>
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
            // 如果投射物还存在，销毁它
            if (selfSpec._projectileController != null)
            {
                // 先取消事件订阅，避免循环调用
                selfSpec._projectileController.OnHit -= OnProjectileHit;
                selfSpec._projectileController.OnReachTarget -= OnProjectileReachTarget;
                selfSpec._projectileController.OnBounce -= OnProjectileBounce;
                selfSpec._projectileController.OnDestroy -= OnProjectileDestroy;

                // 销毁投射物
                if (selfSpec._projectileController.gameObject != null)
                {
                    UnityEngine.Object.Destroy(selfSpec._projectileController.gameObject);
                }
                selfSpec._projectileController = null;
                selfSpec._projectileObject = null;
            }

            Spec.CancelEffect();
        }

        /// <summary>
        /// 旋转2D向量
        /// </summary>
        private Vector2 RotateVector2(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}

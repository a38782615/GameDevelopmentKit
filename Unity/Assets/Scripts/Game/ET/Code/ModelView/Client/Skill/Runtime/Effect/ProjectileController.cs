using System;
using System.Collections.Generic;

using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 投射物控制器
    /// 挂在投射物GameObject上，负责飞行逻辑和碰撞检测
    /// </summary>
    [EnableClass]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    public class ProjectileController : MonoBehaviour
    {
        // ============ 事件 ============
        public event Action<AbilitySystemComponent, Vector2> OnHit;
        public event Action<Vector2> OnReachTarget;
        public event Action<AbilitySystemComponent, Vector2> OnBounce;
        public event Action OnDestroy;

        // ============ 配置数据 ============
        private ProjectileInitData _data;
        private bool _initialized = false;

        // ============ 运行时状态 ============
        private Vector2 _currentPosition;
        private Vector2 _currentDirection;
        private float _traveledDistance;
        private float _totalDistance;
        private int _hitCount;
        private int _bounceCount;
        private HashSet<AbilitySystemComponent> _hitTargets = new HashSet<AbilitySystemComponent>();
        private bool _reachedTarget = false;

        // ============ 曲线飞行 ============
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private float _flightProgress; // 0-1

#if UNITY_EDITOR
        private int _diagLastOverlapCount = -1;
#endif

        /// <summary>
        /// 初始化投射物
        /// </summary>
        public void Initialize(ProjectileInitData data)
        {
            _data = data;
            _initialized = true;

            _currentPosition = data.LaunchPosition;
            _currentDirection = data.Direction;
            _startPosition = data.LaunchPosition;
            _endPosition = data.TargetPosition;
            _totalDistance = Vector2.Distance(_startPosition, _endPosition);
            _traveledDistance = 0f;
            _flightProgress = 0f;
            _hitCount = 0;
            _bounceCount = 0;
            _hitTargets.Clear();
            _reachedTarget = false;
#if UNITY_EDITOR
            _diagLastOverlapCount = -1;
#endif

            // 设置初始朝向
            UpdateRotation();
        }

        private void Update()
        {
            if (!_initialized) return;

            float deltaTime = Time.deltaTime;

            // 更新位置
            UpdatePosition(deltaTime);

            // 碰撞检测
            CheckCollision();
            if (!_initialized) return;

            // 检查是否到达目标或超出距离
            CheckReachTarget();
            if (!_initialized) return;

            // 更新Transform
            transform.position = _currentPosition;
            UpdateRotation();
        }

        /// <summary>
        /// 更新位置
        /// </summary>
        private void UpdatePosition(float deltaTime)
        {
            float moveDistance = _data.Speed * deltaTime;
            _traveledDistance += moveDistance;

            if (_data.TargetType == ProjectileTargetType.Unit)
            {
                // 单位模式：追踪目标
                UpdatePositionForUnit(moveDistance);
            }
            else
            {
                // 点模式：飞向目标点
                UpdatePositionForPosition(moveDistance);
            }
        }

        /// <summary>
        /// 单位模式：追踪飞行
        /// </summary>
        private void UpdatePositionForUnit(float moveDistance)
        {
            // 更新目标位置（如果目标还存在）
            if (_data.TargetUnit?.Owner != null)
            {
                _endPosition = GetTargetUnitPosition();
                _totalDistance = Vector2.Distance(_currentPosition, _endPosition);
            }

            if (_data.CurveHeight > 0 && _totalDistance > 0.1f)
            {
                // 曲线追踪：使用抛物线插值
                _flightProgress = Mathf.Clamp01(_traveledDistance / (_totalDistance + _traveledDistance));

                // 计算直线位置
                Vector2 linearPos = Vector2.MoveTowards(_currentPosition, _endPosition, moveDistance);

                // 添加曲线偏移
                float curveOffset = _data.CurveHeight * 4f * _flightProgress * (1f - _flightProgress);
                Vector2 perpendicular = GetPerpendicular(_currentDirection);

                _currentPosition = linearPos + perpendicular * curveOffset;
                _currentDirection = (_endPosition - _currentPosition).normalized;
            }
            else
            {
                // 直线追踪
                _currentDirection = (_endPosition - _currentPosition).normalized;
                _currentPosition = Vector2.MoveTowards(_currentPosition, _endPosition, moveDistance);
            }
        }

        /// <summary>
        /// 点模式：直线/曲线飞行
        /// </summary>
        private void UpdatePositionForPosition(float moveDistance)
        {
            if (_data.FlyOver)
            {
                // 飞跃模式：沿初始方向一直飞
                _currentPosition += _currentDirection * moveDistance;
            }
            else if (_data.CurveHeight > 0 && _totalDistance > 0.1f)
            {
                // 曲线飞行（抛物线）
                _flightProgress = Mathf.Clamp01(_traveledDistance / _totalDistance);

                // 计算直线位置
                Vector2 linearPos = Vector2.Lerp(_startPosition, _endPosition, _flightProgress);

                // 添加抛物线偏移（垂直于飞行方向）
                float curveOffset = _data.CurveHeight * 4f * _flightProgress * (1f - _flightProgress);
                Vector2 perpendicular = GetPerpendicular((_endPosition - _startPosition).normalized);

                _currentPosition = linearPos + perpendicular * curveOffset;

                // 更新方向（用于旋转）
                if (_flightProgress < 1f)
                {
                    float nextProgress = Mathf.Clamp01((_traveledDistance + 0.1f) / _totalDistance);
                    Vector2 nextLinearPos = Vector2.Lerp(_startPosition, _endPosition, nextProgress);
                    float nextCurveOffset = _data.CurveHeight * 4f * nextProgress * (1f - nextProgress);
                    Vector2 nextPos = nextLinearPos + perpendicular * nextCurveOffset;
                    _currentDirection = (nextPos - _currentPosition).normalized;
                }
            }
            else
            {
                // 直线飞行
                _currentPosition += _currentDirection * moveDistance;
            }
        }

        /// <summary>
        /// 获取目标单位位置（考虑挂点）
        /// </summary>
        private Vector2 GetTargetUnitPosition()
        {
            if (_data.TargetUnit?.Owner == null)
                return _endPosition;

            var transform = _data.TargetUnit.Owner.transform;

            if (!string.IsNullOrEmpty(_data.TargetBindingName))
            {
                var bindingPoint = transform.Find(_data.TargetBindingName);
                if (bindingPoint != null)
                    return bindingPoint.position;
            }

            return transform.position;
        }

        /// <summary>
        /// 碰撞检测
        /// </summary>
        private void CheckCollision()
        {
            // 使用Physics2D.OverlapCircleAll检测碰撞
            var colliders = Physics2D.OverlapCircleAll(_currentPosition, _data.CollisionRadius);
#if UNITY_EDITOR
            if (_data.SkillId == "1010" && colliders.Length != _diagLastOverlapCount)
            {
                _diagLastOverlapCount = colliders.Length;
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                foreach (Collider2D collider in colliders)
                {
                    AbilitySystemComponent mappedAsc = GetASCFromCollider(collider);
                    if (builder.Length > 0)
                    {
                        builder.Append(';');
                    }

                    builder.Append(collider.name);
                    builder.Append("=>");
                    builder.Append(mappedAsc?.Owner != null ? mappedAsc.Owner.name : "null");
                }

            }
#endif

            foreach (var collider in colliders)
            {
                var asc = GetASCFromCollider(collider);
                if (asc == null) continue;

                // 跳过已命中的目标
                if (_hitTargets.Contains(asc)) continue;

                // 跳过发射者
                if (asc == _data.SourceASC) continue;

                // 检查标签
                if (!IsValidTarget(asc)) continue;

                // 命中！
                _hitTargets.Add(asc);
                _hitCount++;
#if UNITY_EDITOR
                if (_data.SkillId == "1010")
                {
                }
#endif

                // 触发命中事件
                OnHit?.Invoke(asc, _currentPosition);

                // 检查是否需要销毁或反弹
                if (!_data.IsPiercing)
                {
                    // 不穿透模式
                    if (_data.IsBouncing)
                    {
                        // 启用反弹：尝试反弹到下一个目标
                        if (_bounceCount < _data.MaxBounceCount)
                        {
                            if (TryBounceToNextTarget(asc, collider))
                            {
                                return; // 成功反弹，继续飞行
                            }
                            // 找不到反弹目标，继续沿当前方向飞行
                            return;
                        }
                        // 达到最大反弹次数，销毁
                        DestroyProjectile();
                        return;
                    }
                    else
                    {
                        // 未启用反弹：直接销毁
                        DestroyProjectile();
                        return;
                    }
                }
                else if (_hitCount >= _data.MaxPierceCount)
                {
                    // 达到最大穿透数，检查是否可以反弹
                    if (_data.IsBouncing && _bounceCount < _data.MaxBounceCount)
                    {
                        if (TryBounceToNextTarget(asc, collider))
                        {
                            _hitCount = 0; // 反弹后重置穿透计数
                            return;
                        }
                        // 找不到反弹目标，继续沿当前方向飞行
                        _hitCount = 0;
                        return;
                    }
                    // 无法反弹，销毁
                    DestroyProjectile();
                    return;
                }
            }
        }

        /// <summary>
        /// 检查是否到达目标
        /// </summary>
        private void CheckReachTarget()
        {
            if (_data.TargetType == ProjectileTargetType.Unit)
            {
                // 单位模式：检查是否碰到目标单位（由碰撞检测处理）
                // 如果目标死亡，继续飞行直到超出范围
                if (_data.TargetUnit?.Owner == null)
                {
                    // 目标已死亡，检查最大距离
                    if (_data.MaxDistance > 0 && _traveledDistance >= _data.MaxDistance)
                    {
                        DestroyProjectile();
                    }
                }
            }
            else
            {
                // 点模式
                if (_data.FlyOver)
                {
                    // 飞跃模式：检查是否经过目标点
                    if (!_reachedTarget)
                    {
                        float distToTarget = Vector2.Distance(_currentPosition, _endPosition);
                        if (distToTarget < _data.CollisionRadius || _traveledDistance >= _totalDistance)
                        {
                            _reachedTarget = true;
                            OnReachTarget?.Invoke(_endPosition);
                        }
                    }

                    // 检查最大距离
                    if (_data.MaxDistance > 0 && _traveledDistance >= _data.MaxDistance)
                    {
                        DestroyProjectile();
                    }
                }
                else
                {
                    // 非飞跃模式：到达目标点就停止
                    if (_flightProgress >= 1f || Vector2.Distance(_currentPosition, _endPosition) < 0.1f)
                    {
                        #if UNITY_EDITOR
                        if (_data.SkillId == "1010")
                        {
                        }
                        #endif
                        OnReachTarget?.Invoke(_endPosition);
                        DestroyProjectile();
                    }

                    // 检查最大距离
                    if (_data.MaxDistance > 0 && _traveledDistance >= _data.MaxDistance)
                    {
                        DestroyProjectile();
                    }
                }
            }
        }

        /// <summary>
        /// 从Collider2D获取AbilitySystemComponent
        /// </summary>
        private AbilitySystemComponent GetASCFromCollider(Collider2D collider)
        {
            return Collider2DRegistry.GetASC(collider);
        }

        /// <summary>
        /// 检查目标是否有效
        /// </summary>
        private bool IsValidTarget(AbilitySystemComponent target)
        {
            if (target == null) return false;

            // 检查目标标签（必须拥有）
            if (!_data.CollisionTargetTags.IsEmpty && !target.OwnedTags.HasAnyTags(_data.CollisionTargetTags))
                return false;

            // 检查排除标签
            if (!_data.CollisionExcludeTags.IsEmpty && target.OwnedTags.HasAnyTags(_data.CollisionExcludeTags))
                return false;

            return true;
        }

        /// <summary>
        /// 尝试反弹到下一个目标
        /// </summary>
        private bool TryBounceToNextTarget(AbilitySystemComponent currentTarget, Collider2D hitCollider)
        {
            // 记录反弹
            _bounceCount++;

            if (_data.BounceTargetMode == BounceTargetMode.SearchNearest)
            {
                // 搜索最近目标模式
                var nextTarget = FindNextBounceCandidate(currentTarget);
                if (nextTarget == null)
                {
                    _bounceCount--; // 回退计数
                    return false;
                }

                // 如果不允许反弹到相同目标，将当前目标加入已命中列表
                if (!_data.CanBounceToSameTarget)
                {
                    _hitTargets.Add(currentTarget);
                }

                // 更新目标
                _data.TargetUnit = nextTarget;
                _startPosition = _currentPosition;
                _endPosition = GetTargetUnitPosition();
                _totalDistance = Vector2.Distance(_startPosition, _endPosition);
                _traveledDistance = 0f;
                _flightProgress = 0f;
                _currentDirection = (_endPosition - _startPosition).normalized;

                // 触发反弹事件
                OnBounce?.Invoke(nextTarget, _currentPosition);
            }
            else
            {
                // 反向偏移角度模式
                // 计算反向方向并应用偏移角度
                Vector2 reflectDirection = GetReflectDirection(hitCollider, currentTarget);
                if (Mathf.Abs(_data.BounceAngleOffset) > 0.01f)
                {
                    reflectDirection = RotateVector2(reflectDirection, _data.BounceAngleOffset);
                }

                // 如果不允许反弹到相同目标，将当前目标加入已命中列表
                if (!_data.CanBounceToSameTarget && currentTarget != null)
                {
                    _hitTargets.Add(currentTarget);
                }

                // 更新方向和位置
                _currentDirection = reflectDirection;
                _startPosition = _currentPosition;
                _data.TargetUnit = null; // 清除目标单位，改为点模式飞行
                _traveledDistance = 0f;
                _flightProgress = 0f;

                // 触发反弹事件（无特定目标）
                OnBounce?.Invoke(null, _currentPosition);
            }

            return true;
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

        private Vector2 GetReflectDirection(Collider2D hitCollider, AbilitySystemComponent currentTarget)
        {
            Vector2 surfaceNormal = GetSurfaceNormal(hitCollider, currentTarget);
            Vector2 reflectDirection = Vector2.Reflect(_currentDirection.normalized, surfaceNormal).normalized;
            if (reflectDirection.sqrMagnitude <= 0.0001f)
            {
                reflectDirection = -_currentDirection.normalized;
            }

            return reflectDirection;
        }

        private Vector2 GetSurfaceNormal(Collider2D hitCollider, AbilitySystemComponent currentTarget)
        {
            if (hitCollider != null)
            {
                Vector2 closestPoint = hitCollider.ClosestPoint(_currentPosition - (_currentDirection.normalized * _data.CollisionRadius));
                Vector2 surfaceNormal = (_currentPosition - closestPoint).normalized;
                if (surfaceNormal.sqrMagnitude > 0.0001f)
                {
                    return surfaceNormal;
                }
            }

            if (currentTarget?.Owner != null)
            {
                Vector2 fallbackNormal = (_currentPosition - (Vector2)currentTarget.Owner.transform.position).normalized;
                if (fallbackNormal.sqrMagnitude > 0.0001f)
                {
                    return fallbackNormal;
                }
            }

            return -_currentDirection.normalized;
        }

        /// <summary>
        /// 搜索下一个反弹目标
        /// </summary>
        private AbilitySystemComponent FindNextBounceTarget(AbilitySystemComponent currentTarget)
        {
            var colliders = Physics2D.OverlapCircleAll(_currentPosition, _data.BounceSearchRadius);

            AbilitySystemComponent nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                var asc = GetASCFromCollider(collider);
                if (asc == null) continue;

                // 跳过发射者
                if (asc == _data.SourceASC) continue;

                // 跳过当前目标
                if (asc == currentTarget) continue;

                // 如果不允许反弹到相同目标，跳过已命中的目标
                if (!_data.CanBounceToSameTarget && _hitTargets.Contains(asc))
                    continue;

                // 检查标签
                if (!IsValidTarget(asc)) continue;

                // 计算距离，选择最近的目标
                float distance = Vector2.Distance(_currentPosition, (Vector2)asc.Owner.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = asc;
                }
            }

            return nearestTarget;
        }

        private AbilitySystemComponent FindNextBounceCandidate(AbilitySystemComponent currentTarget)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(_currentPosition, _data.BounceSearchRadius);

            AbilitySystemComponent nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = GetASCFromCollider(collider);
                if (asc == null || asc == _data.SourceASC || asc == currentTarget)
                {
                    continue;
                }

                if (!_data.CanBounceToSameTarget && _hitTargets.Contains(asc))
                {
                    continue;
                }

                if (ShouldExcludeSourceCamp(asc) || !IsValidTarget(asc))
                {
                    continue;
                }

                float distance = Vector2.Distance(_currentPosition, (Vector2)asc.Owner.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = asc;
                }
            }

            return nearestTarget;
        }

        private bool ShouldExcludeSourceCamp(AbilitySystemComponent target)
        {
            if (!_data.ExcludeSourceCamp)
            {
                return false;
            }

            GameplayTag sourceCampTag = GetCampTag(_data.SourceASC);
            if (!sourceCampTag.IsValid || target?.OwnedTags == null)
            {
                return false;
            }

            return target.OwnedTags.HasTagExact(sourceCampTag);
        }

        private GameplayTag GetCampTag(AbilitySystemComponent asc)
        {
            if (asc?.OwnedTags == null || asc.OwnedTags.IsEmpty)
            {
                return GameplayTag.None;
            }

            var tags = asc.OwnedTags.Tags;
            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTag tag = tags[i];
                if (tag.IsValid && tag.GetParent() == GameplayTagLibrary.unitType)
                {
                    return tag;
                }
            }

            return GameplayTag.None;
        }

        /// <summary>
        /// 获取垂直向量（用于曲线计算）
        /// 正数向上弯曲，负数向下弯曲
        /// </summary>
        private Vector2 GetPerpendicular(Vector2 dir)
        {
            // 返回向上的垂直向量（左手法则：dir顺时针旋转90°）
            return new Vector2(dir.y, -dir.x);
        }

        /// <summary>
        /// 更新旋转（朝向飞行方向）
        /// </summary>
        private void UpdateRotation()
        {
            if (_currentDirection.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(_currentDirection.y, _currentDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        /// <summary>
        /// 销毁投射物
        /// </summary>
        private void DestroyProjectile()
        {
            if (!_initialized)
            {
                return;
            }

            _initialized = false;
            enabled = false;
            OnDestroy?.Invoke();

            // 清理事件
            OnHit = null;
            OnReachTarget = null;
            OnBounce = null;
            OnDestroy = null;

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_initialized) return;

            // 绘制碰撞半径
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_currentPosition, _data.CollisionRadius);

            // 绘制目标位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_endPosition, 0.2f);

            // 绘制飞行路径
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_currentPosition, _endPosition);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(TargetSearcher))]
    [FriendOf(typeof(TargetSearcher))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    public static partial class TargetSearcherSystem
    {
        [EntitySystem]
        private static void Awake(this TargetSearcher self)
        {
        }
        // ============ 圆形搜索 ============
        /// <summary>
        /// 圆形范围搜索
        /// </summary>
        public static List<AbilitySystemComponent> SearchCircle(this TargetSearcher self, Vector3 center, float radius, SearchConfig config)
        {
            var results = new List<AbilitySystemComponent>();

            int layerMask = config.LayerMask != 0 ? config.LayerMask : TargetSearcher.DefaultLayerMask;
            var colliders = Physics.OverlapSphere(center, radius, layerMask);

            foreach (var collider in colliders)
            {
                var asc = self.GetASCFromCollider(collider);
                if (asc != null && self.IsValidTarget(asc, config))
                {
                    results.Add(asc);

                    if (config.MaxTargets > 0 && results.Count >= config.MaxTargets)
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// 圆形范围搜索（简化版）
        /// </summary>
        public static List<AbilitySystemComponent> SearchCircle(this TargetSearcher self, Vector3 center, float radius, AbilitySystemComponent searcher = null)
        {
            return self.SearchCircle(center, radius, new SearchConfig { Searcher = searcher });
        }

        // ============ 扇形搜索 ============

        /// <summary>
        /// 扇形范围搜索
        /// </summary>
        public static List<AbilitySystemComponent> SearchSector(this TargetSearcher self, Vector3 center, Vector3 forward, float radius, float angle, SearchConfig config)
        {
            var results = new List<AbilitySystemComponent>();

            int layerMask = config.LayerMask != 0 ? config.LayerMask : TargetSearcher.DefaultLayerMask;
            var colliders = Physics.OverlapSphere(center, radius, layerMask);

            float halfAngle = angle * 0.5f;

            foreach (var collider in colliders)
            {
                var asc = self.GetASCFromCollider(collider);
                if (asc == null || !self.IsValidTarget(asc, config))
                    continue;

                // 检查是否在扇形范围内
                var dirToTarget = (collider.transform.position - center).normalized;
                float angleToTarget = Vector3.Angle(forward, dirToTarget);

                if (angleToTarget <= halfAngle)
                {
                    results.Add(asc);

                    if (config.MaxTargets > 0 && results.Count >= config.MaxTargets)
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// 扇形范围搜索（简化版）
        /// </summary>
        public static List<AbilitySystemComponent> SearchSector(this TargetSearcher self, Vector3 center, Vector3 forward, float radius, float angle, AbilitySystemComponent searcher = null)
        {
            return self.SearchSector(center, forward, radius, angle, new SearchConfig { Searcher = searcher });
        }

        // ============ 矩形/直线搜索 ============

        /// <summary>
        /// 矩形范围搜索
        /// </summary>
        public static List<AbilitySystemComponent> SearchBox(this TargetSearcher self,Vector3 center, Vector3 halfExtents, Quaternion orientation, SearchConfig config)
        {
            var results = new List<AbilitySystemComponent>();

            int layerMask = config.LayerMask != 0 ? config.LayerMask : TargetSearcher.DefaultLayerMask;
            var colliders = Physics.OverlapBox(center, halfExtents, orientation, layerMask);

            foreach (var collider in colliders)
            {
                var asc = self.GetASCFromCollider(collider);
                if (asc != null && self.IsValidTarget(asc, config))
                {
                    results.Add(asc);

                    if (config.MaxTargets > 0 && results.Count >= config.MaxTargets)
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// 直线范围搜索
        /// </summary>
        public static List<AbilitySystemComponent> SearchLine(this TargetSearcher self,Vector3 start, Vector3 direction, float length, float width, SearchConfig config)
        {
            var results = new List<AbilitySystemComponent>();

            // 计算盒子中心和尺寸
            var center = start + direction * (length * 0.5f);
            var halfExtents = new Vector3(width * 0.5f, 1f, length * 0.5f);
            var rotation = Quaternion.LookRotation(direction);

            int layerMask = config.LayerMask != 0 ? config.LayerMask : TargetSearcher.DefaultLayerMask;
            var colliders = Physics.OverlapBox(center, halfExtents, rotation, layerMask);

            foreach (var collider in colliders)
            {
                var asc = self.GetASCFromCollider(collider);
                if (asc != null && self.IsValidTarget(asc, config))
                {
                    results.Add(asc);

                    if (config.MaxTargets > 0 && results.Count >= config.MaxTargets)
                        break;
                }
            }

            return results;
        }

        // ============ 射线搜索 ============

        /// <summary>
        /// 射线搜索（返回第一个命中的目标）
        /// </summary>
        public static AbilitySystemComponent SearchRay(this TargetSearcher self, Vector3 origin, Vector3 direction, float maxDistance, SearchConfig config)
        {
            int layerMask = config.LayerMask != 0 ? config.LayerMask : TargetSearcher.DefaultLayerMask;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
            {
                var asc = self.GetASCFromCollider(hit.collider);
                if (asc != null && self.IsValidTarget(asc, config))
                {
                    return asc;
                }
            }

            return null;
        }

        /// <summary>
        /// 射线搜索（返回所有命中的目标）
        /// </summary>
        public static List<AbilitySystemComponent> SearchRayAll(this TargetSearcher self, Vector3 origin, Vector3 direction, float maxDistance, SearchConfig config)
        {
            var results = new List<AbilitySystemComponent>();

            int layerMask = config.LayerMask != 0 ? config.LayerMask : TargetSearcher.DefaultLayerMask;
            var hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);

            foreach (var hit in hits)
            {
                var asc = self.GetASCFromCollider(hit.collider);
                if (asc != null && self.IsValidTarget(asc, config))
                {
                    results.Add(asc);

                    if (config.MaxTargets > 0 && results.Count >= config.MaxTargets)
                        break;
                }
            }

            return results;
        }

        // ============ 最近目标搜索 ============

        /// <summary>
        /// 搜索最近的目标
        /// </summary>
        public static AbilitySystemComponent SearchNearest(this TargetSearcher self, Vector3 center, float radius, SearchConfig config)
        {
            var targets = self.SearchCircle(center, radius, config);
            if (targets.Count == 0)
                return null;

            AbilitySystemComponent nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var target in targets)
            {
                Transform targetTransform = target?.GetOwnerTransform();
                if (targetTransform == null)
                    continue;

                float distance = Vector3.Distance(center, targetTransform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = target;
                }
            }

            return nearest;
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 从Collider获取ASC
        /// </summary>
        private static AbilitySystemComponent GetASCFromCollider(this TargetSearcher self,Collider collider)
        {
            if (collider == null)
                return null;

            // 尝试从GameObject获取ASC
            var asc = collider.GetComponent<AbilitySystemComponent>();
            if (asc != null)
                return asc;

            // 尝试从父对象获取
            asc = collider.GetComponentInParent<AbilitySystemComponent>();
            return asc;
        }

        /// <summary>
        /// 检查目标是否有效
        /// </summary>
        private static bool IsValidTarget(this TargetSearcher self,AbilitySystemComponent target, SearchConfig config)
        {
            if (target == null)
                return false;

            // 排除搜索者自己
            if (config.Searcher != null && target == config.Searcher)
                return false;

            // 检查目标标签
            if (!config.TargetTags.IsEmpty)
            {
                if (!target.HasAnyTags(config.TargetTags))
                    return false;
            }

            // 检查排除标签
            if (!config.ExcludeTags.IsEmpty)
            {
                if (target.HasAnyTags(config.ExcludeTags))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 按距离排序目标
        /// </summary>
        public static void SortByDistance(this TargetSearcher self,List<AbilitySystemComponent> targets, Vector3 center, bool ascending = true)
        {
            targets.Sort((a, b) =>
            {
                Transform transformA = a?.GetOwnerTransform();
                Transform transformB = b?.GetOwnerTransform();
                if (transformA == null) return 1;
                if (transformB == null) return -1;

                float distA = Vector3.Distance(center, transformA.position);
                float distB = Vector3.Distance(center, transformB.position);

                return ascending ? distA.CompareTo(distB) : distB.CompareTo(distA);
            });
        }

        /// <summary>
        /// 过滤目标列表
        /// </summary>
        public static List<AbilitySystemComponent> FilterTargets(this TargetSearcher self,List<AbilitySystemComponent> targets, SearchConfig config)
        {
            var filtered = new List<AbilitySystemComponent>();

            foreach (var target in targets)
            {
                if (self.IsValidTarget(target, config))
                {
                    filtered.Add(target);

                    if (config.MaxTargets > 0 && filtered.Count >= config.MaxTargets)
                        break;
                }
            }

            return filtered;
        }
    }
}

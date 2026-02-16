using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 简单目标提供者 - 直接持有目标列表
    /// </summary>
    [EnableClass]
    public class SimpleTargetProvider : ITargetProvider
    {
        private List<AbilitySystemComponent> _targets = new List<AbilitySystemComponent>();

        public int TargetCount => _targets.Count;

        public bool HasValidTargets => _targets.Count > 0;

        /// <summary>
        /// 添加目标
        /// </summary>
        public void AddTarget(AbilitySystemComponent target)
        {
            if (target != null && !_targets.Contains(target))
            {
                _targets.Add(target);
            }
        }

        /// <summary>
        /// 添加多个目标
        /// </summary>
        public void AddTargets(IEnumerable<AbilitySystemComponent> targets)
        {
            if (targets == null)
                return;

            foreach (var target in targets)
            {
                AddTarget(target);
            }
        }

        /// <summary>
        /// 移除目标
        /// </summary>
        public void RemoveTarget(AbilitySystemComponent target)
        {
            _targets.Remove(target);
        }

        public AbilitySystemComponent GetTarget()
        {
            return _targets.Count > 0 ? _targets[0] : null;
        }

        public List<AbilitySystemComponent> GetTargets()
        {
            return new List<AbilitySystemComponent>(_targets);
        }

        public void Clear()
        {
            _targets.Clear();
        }
    }
}

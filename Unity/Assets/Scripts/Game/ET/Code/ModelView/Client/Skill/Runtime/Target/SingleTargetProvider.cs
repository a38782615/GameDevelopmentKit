using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 单目标提供者 - 只持有一个目标
    /// </summary>
    [EnableClass]
    public class SingleTargetProvider : ITargetProvider
    {
        private AbilitySystemComponent _target;

        public SingleTargetProvider(AbilitySystemComponent target = null)
        {
            _target = target;
        }

        public int TargetCount => _target != null ? 1 : 0;

        public bool HasValidTargets => _target != null;

        public void SetTarget(AbilitySystemComponent target)
        {
            _target = target;
        }

        public AbilitySystemComponent GetTarget()
        {
            return _target;
        }

        public List<AbilitySystemComponent> GetTargets()
        {
            var list = new List<AbilitySystemComponent>();
            if (_target != null)
                list.Add(_target);
            return list;
        }

        public void Clear()
        {
            _target = null;
        }
    }
}

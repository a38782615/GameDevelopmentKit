using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 延迟目标提供者 - 在需要时才执行搜索
    /// </summary>
    public class LazyTargetProvider : ITargetProvider
    {
        private System.Func<List<AbilitySystemComponent>> _searchFunc;
        private List<AbilitySystemComponent> _cachedTargets;
        private bool _isCached;

        public LazyTargetProvider(System.Func<List<AbilitySystemComponent>> searchFunc)
        {
            _searchFunc = searchFunc;
            _isCached = false;
        }

        public int TargetCount
        {
            get
            {
                EnsureCached();
                return _cachedTargets?.Count ?? 0;
            }
        }

        public bool HasValidTargets
        {
            get
            {
                EnsureCached();
                return _cachedTargets != null && _cachedTargets.Count > 0;
            }
        }

        public AbilitySystemComponent GetTarget()
        {
            EnsureCached();
            return _cachedTargets?.Count > 0 ? _cachedTargets[0] : null;
        }

        public List<AbilitySystemComponent> GetTargets()
        {
            EnsureCached();
            return _cachedTargets != null ? new List<AbilitySystemComponent>(_cachedTargets) : new List<AbilitySystemComponent>();
        }

        public void Clear()
        {
            _cachedTargets?.Clear();
            _isCached = false;
        }

        /// <summary>
        /// 强制刷新缓存
        /// </summary>
        public void Refresh()
        {
            _isCached = false;
            EnsureCached();
        }

        private void EnsureCached()
        {
            if (!_isCached && _searchFunc != null)
            {
                _cachedTargets = _searchFunc();
                _isCached = true;
            }
        }
    }
}

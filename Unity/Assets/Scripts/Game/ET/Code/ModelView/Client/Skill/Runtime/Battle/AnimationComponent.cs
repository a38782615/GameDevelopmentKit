using System;
using UnityEngine;
#if Spine
using Spine.Unity;
#endif

namespace ET.Client
{
    [EnableClass]
    [FriendOf(typeof(AbilitySystemComponent))]
    public class AnimationComponent : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private AbilitySystemComponent _asc;
#if Spine
        private SkeletonAnimation _animation;
#endif
        private readonly GameplayTag _cachedStunTag = GameplayTagLibrary.Buff_DeBuff_Stun;
        private bool _isListening;

        public string StandAnimationName = "Stand";
        public string StunAnimationName = "Stun";

        [HideInInspector]
        public bool _isStunned;

        private void Awake()
        {
            this.EnsureAnimationReference();
        }

        private void OnEnable()
        {
            this.RegisterTagListeners();
        }

        private void OnDisable()
        {
            this.UnregisterTagListeners();
        }

        public void Initialize(AbilitySystemComponent asc)
        {
            if (!ReferenceEquals(_asc, asc))
            {
                this.UnregisterTagListeners();
                _asc = asc;
            }

            this.EnsureAnimationReference();
            this.RegisterTagListeners();
        }

        private void OnTagAdded(GameplayTag tag)
        {
            if (_isStunned || tag != _cachedStunTag)
            {
                return;
            }

            _isStunned = true;
            this.PlayAnimation(StunAnimationName, true);
        }

        private void OnTagRemoved(GameplayTag tag)
        {
            if (!_isStunned || tag != _cachedStunTag || _asc == null || _asc.OwnedTags.HasTag(_cachedStunTag))
            {
                return;
            }

            _isStunned = false;
            this.PlayAnimation(StandAnimationName, true);
        }

        public void PlayAnimation(string name, bool loop)
        {
#if Spine
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            this.EnsureAnimationReference();
            if (_animation?.AnimationState == null)
            {
                return;
            }

            var current = _animation.AnimationState.GetCurrent(0);
            if (current?.Animation?.Name == name)
            {
                return;
            }

            _animation.AnimationState.SetAnimation(0, name, loop);
#endif
        }

        private void RegisterTagListeners()
        {
            if (_isListening || !isActiveAndEnabled || _asc?.OwnedTags == null)
            {
                return;
            }

            _asc.OwnedTags.OnTagAdded += OnTagAdded;
            _asc.OwnedTags.OnTagRemoved += OnTagRemoved;
            _isListening = true;

            _isStunned = _asc.OwnedTags.HasTag(_cachedStunTag);
            if (_isStunned)
            {
                this.PlayAnimation(StunAnimationName, true);
            }
        }

        private void UnregisterTagListeners()
        {
            if (!_isListening || _asc?.OwnedTags == null)
            {
                return;
            }

            _asc.OwnedTags.OnTagAdded -= OnTagAdded;
            _asc.OwnedTags.OnTagRemoved -= OnTagRemoved;
            _isListening = false;
        }

        private void EnsureAnimationReference()
        {
#if Spine
            if (_animation == null)
            {
                _animation = GetComponentInChildren<SkeletonAnimation>(true);
            }
#endif
        }
    }
}

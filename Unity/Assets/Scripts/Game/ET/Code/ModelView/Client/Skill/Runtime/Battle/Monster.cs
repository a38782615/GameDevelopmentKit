
using UnityEngine;

namespace ET.Client
{
    public class Monster : SkillUnit
    {
        public SkillUnit target;
        public AnimationComponent AnimationComponent;
        private GameplayAbilitySpec _normalAttackSpec;

        void Start()
        {
            ownerASC.OwnedTags.AddTag(new GameplayTag("unitType.monster"));

            var unitData = Tables.Instance.DTUnit.GetOrDefault(id);
            if (unitData != null && unitData.ActiveSkill.Length > 0)
            {
                _normalAttackSpec = ownerASC.Abilities.FindAbilityById(unitData.ActiveSkill[0]);
            }
        }

        void Update()
        {
            TryNormalAttack();

            if (target)
            {
                Vector3 scale = transform.localScale;
                scale.x = target.transform.position.x < transform.position.x ? 1 : -1;
                transform.localScale = scale;
            }
        }

        private void TryNormalAttack()
        {
            if (_normalAttackSpec == null || target == null) return;

            if (!_normalAttackSpec.IsRunning && !AnimationComponent._isStunned)
            {
                bool success = ownerASC.TryActivateAbility(_normalAttackSpec, target.ownerASC);

                if (!success)
                {
                    AnimationComponent.PlayAnimation("Stand", true);
                }
            }
        }
    }
}
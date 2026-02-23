using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public class Monster : SkillUnit
    {
        public SkillUnit target;
        public AnimationComponent AnimationComponent;
        private GameplayAbilitySpec _normalAttackSpec;

        void Start()
        {
            var asc = GetASC();
            if (asc != null)
                asc.OwnedTags.AddTag(new GameplayTag("unitType.monster"));

            var unitData = Tables.Instance.DTUnit.GetOrDefault(id);
            if (unitData != null && unitData.ActiveSkill.Length > 0 && asc != null)
            {
                _normalAttackSpec = asc.Abilities?.FindAbilityById(unitData.ActiveSkill[0]);
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
            var asc = GetASC();
            if (_normalAttackSpec == null || target == null || asc == null) return;

            if (!_normalAttackSpec.IsRunning && !AnimationComponent._isStunned)
            {
                var targetASC = target.GetASC();
                bool success = asc.TryActivateAbility(_normalAttackSpec, targetASC);

                if (!success)
                {
                    AnimationComponent.PlayAnimation("Stand", true);
                }
            }
        }
    }
}

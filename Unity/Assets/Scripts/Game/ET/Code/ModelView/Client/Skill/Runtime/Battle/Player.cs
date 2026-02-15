
using UnityEngine;

namespace ET
{
    public class Player : SkillUnit
    {
        public SkillUnit target;

        void Start()
        {
            ownerASC.OwnedTags.AddTag(new GameplayTag("unitType.hero"));
        }

        void Update()
        {
            // 按键 1 触发 ThreeFire 技能 (SkillId: 1008)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var spec = ownerASC.Abilities.FindAbilityById(1008);
                if (spec != null)
                {
                    ownerASC.TryActivateAbility(spec);
                }
            }
        }
    }
}

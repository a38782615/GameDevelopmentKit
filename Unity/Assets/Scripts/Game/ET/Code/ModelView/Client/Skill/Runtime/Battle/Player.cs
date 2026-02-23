using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public class Player : SkillUnit
    {
        public SkillUnit target;

        void Start()
        {
            var asc = GetASC();
            if (asc != null)
                asc.OwnedTags.AddTag(new GameplayTag("unitType.hero"));
        }

        void Update()
        {
            var asc = GetASC();
            if (asc == null) return;

            // 按键 1 触发 ThreeFire 技能 (SkillId: 1008)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var spec = asc.Abilities?.FindAbilityById(1008);
                if (spec != null)
                {
                    asc.TryActivateAbility(spec);
                }
            }
        }
    }
}

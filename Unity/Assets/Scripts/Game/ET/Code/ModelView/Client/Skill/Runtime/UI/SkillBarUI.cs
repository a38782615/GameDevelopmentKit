using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ET
{

    /// <summary>
    /// 技能栏管理器 - 使用UGUI
    /// </summary>
    public class SkillBarUI : MonoBehaviour
    {
        [Header("引用")]
        public Player player;

        [Header("UI引用")]
        [Tooltip("技能槽预制体")]
        public GameObject skillSlotPrefab;

        [Tooltip("技能槽父物体")]
        public Transform slotContainer;

        private List<SkillSlotUI> _skillSlots = new List<SkillSlotUI>();

        void Start()
        {
            skillSlotPrefab.gameObject.SetActive(false);
            CreateSkillSlots();
        }

        private void CreateSkillSlots()
        {
            if (skillSlotPrefab == null || slotContainer == null || player == null) return;

            var tables = Tables.Instance;
            var unitData = tables.DTUnit.GetOrDefault(player.id);
            if (unitData == null) return;

            var tbSkill = tables.DTSkill;

            foreach (var skillId in unitData.ActiveSkill)
            {
                CreateSlot(skillId, tbSkill);
            }

            foreach (var skillId in unitData.PassiveSkill)
            {
                CreateSlot(skillId, tbSkill);
            }
        }

        private void CreateSlot(int skillId, DTSkill tbSkill)
        {
            var skillData = tbSkill.GetOrDefault(skillId);
            if (skillData == null) return;

            Sprite icon = Resources.Load<Sprite>(skillData.IconPath);

            GameObject slotObj = Instantiate(skillSlotPrefab, slotContainer);
            slotObj.gameObject.SetActive(true);
            var slotUI = slotObj.GetComponent<SkillSlotUI>();

            if (slotUI == null)
            {
                slotUI = slotObj.AddComponent<SkillSlotUI>();
            }

            slotUI.Initialize(skillId, skillData.Name, icon, this);
            _skillSlots.Add(slotUI);

            if (player.ownerASC != null)
            {
                slotUI.AbilitySpec = player.ownerASC.Abilities.FindAbilityById(skillId);
            }
        }

        public bool TryActivateSkill(SkillSlotUI slot)
        {
            if (player?.ownerASC == null || slot.AbilitySpec == null) return false;

            return player.ownerASC.TryActivateAbility(slot.AbilitySpec, player.target.ownerASC);
        }
    }
}

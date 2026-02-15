using System.Collections.Generic;

using UnityEngine;

namespace ET.Client
{

    public class SkillUnit : MonoBehaviour
    {
        public AbilitySystemComponent ownerASC;

        [Header("单位配置")]
        public int id;

        protected virtual void Awake()
        {
            ownerASC = new AbilitySystemComponent(this.gameObject);

            UnitManager.Instance.Register(this);

            InitFromTable();
        }

        protected virtual void OnDestroy()
        {
            UnitManager.Instance.Unregister(this);
        }

        private void InitFromTable()
        {
            var data = Tables.Instance.DTUnit.GetOrDefault(id);
            if (data == null)
            {
                Debug.LogWarning($"[Unit] TbUnit中找不到ID: {id}");
                return;
            }

            InitAttributes(data.InitialAttribute);
            GrantSkills(data.ActiveSkill);
            GrantSkills(data.PassiveSkill);
        }

        private void InitAttributes((int, int)[] attributes)
        {
            if (attributes == null) return;

            foreach (var (typeId, value) in attributes)
            {
                var attrType = (AttrType)typeId;
                if (!ownerASC.Attributes.HasAttribute(attrType))
                    ownerASC.Attributes.AddAttribute(attrType, value);
            }
        }

        private void GrantSkills(int[] skillIds)
        {
            if (skillIds == null) return;

            var tbSkill = Tables.Instance.DTSkill;
            foreach (var skillId in skillIds)
            {
                var skillData = tbSkill.GetOrDefault(skillId);
                if (skillData == null)
                {
                    Debug.LogWarning($"[Unit] 技能表中找不到ID: {skillId}");
                    continue;
                }
                var graphData = SkillDataCenter.Instance.GetSkillGraph(skillData.Id.ToString());
                ownerASC.GrantAbility(graphData, skillId);
            }
        }
    }

}
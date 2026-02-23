using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// SkillUnit - 战斗单位的 MonoBehaviour 桥接
    /// 现在通过 ET 的 Unit Entity 创建 AbilitySystemComponent
    /// </summary>
    [EnableClass]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]

    public class SkillUnit : MonoBehaviour
    {
        [Header("单位配置")]
        public int id;

        /// <summary>
        /// 关联的 ET Unit Entity Id
        /// </summary>
        public long UnitId;

        /// <summary>
        /// 获取关联的 AbilitySystemComponent
        /// </summary>
        public AbilitySystemComponent GetASC()
        {
            if (UnitId == 0) return null;
            // 需要通过场景获取 Unit，这里提供一个简化的访问方式
            return _cachedASC;
        }

        private AbilitySystemComponent _cachedASC;

        /// <summary>
        /// 通过 ET Unit 初始化技能系统
        /// 应在 Unit Entity 创建后调用
        /// </summary>
        public void InitFromUnit(Unit unit)
        {
            if (unit == null) return;

            UnitId = unit.Id;

            // 添加 ASC 组件
            var asc = unit.AddComponent<AbilitySystemComponent>();
            unit.AddComponent<GameObjectComponent>(gameObject);
            _cachedASC = asc;

            InitFromTable();
        }

        private void InitFromTable()
        {
            var asc = GetASC();
            if (asc == null) return;

            var data = Tables.Instance.DTUnit.GetOrDefault(id);
            if (data == null)
            {
                Debug.LogWarning($"[Unit] TbUnit中找不到ID: {id}");
                return;
            }

            InitAttributes(asc, data.InitialAttribute);
            GrantSkills(asc, data.ActiveSkill);
            GrantSkills(asc, data.PassiveSkill);
        }

        private void InitAttributes(AbilitySystemComponent asc, (int, int)[] attributes)
        {
            if (attributes == null) return;

            foreach (var (typeId, value) in attributes)
            {
                var attrType = (AttrType)typeId;
                if (!asc.Attributes.HasAttribute(attrType))
                    asc.Attributes.AddAttribute(attrType, value);
            }
        }

        private void GrantSkills(AbilitySystemComponent asc, int[] skillIds)
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
                asc.GrantAbility(graphData);
            }
        }
    }
}

using System.Collections.Generic;
using Game;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormUnitAttributeComponent))]
    [FriendOf(typeof(UIFormUnitAttributeComponent))]
    [FriendOf(typeof(AbilitySystemComponent))]
    public static partial class UIFormUnitAttributeComponentSystem
    {
        private const float RefreshInterval = 0.2f;

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormUnitAttributeComponent self)
        {
            self.RefreshLeftTime = 0f;
            self.EnsureLayoutBuilt();
            self.RefreshPanels();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormUnitAttributeComponent self, bool isShutdown)
        {
            self.ClearDynamicRows(self.View?.PlayerRowsRectTransform, self.PlayerRows);
            self.ClearDynamicRows(self.View?.MonsterRowsRectTransform, self.MonsterRows);
            self.OrderedAttrTypes.Clear();
            self.LayoutBuilt = false;
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnUpdate(this UIFormUnitAttributeComponent self, float elapseSeconds, float realElapseSeconds)
        {
            self.RefreshLeftTime -= elapseSeconds;
            if (self.RefreshLeftTime > 0f)
            {
                return;
            }

            self.RefreshLeftTime = RefreshInterval;
            self.RefreshPanels();
        }

        private static void EnsureLayoutBuilt(this UIFormUnitAttributeComponent self)
        {
            MonoUIFormUnitAttribute view = self.View;
            if (self.LayoutBuilt ||
                view?.PlayerRowsRectTransform == null ||
                view.MonsterRowsRectTransform == null ||
                view.PlayerCategoryTemplateText == null ||
                view.MonsterCategoryTemplateText == null ||
                view.PlayerItemTemplateAttributeRowTemplate == null ||
                view.MonsterItemTemplateAttributeRowTemplate == null)
            {
                return;
            }

            self.ClearDynamicRows(view.PlayerRowsRectTransform, self.PlayerRows);
            self.ClearDynamicRows(view.MonsterRowsRectTransform, self.MonsterRows);
            self.OrderedAttrTypes.Clear();

            self.BuildPanelRows(
                view.PlayerRowsRectTransform,
                view.PlayerCategoryTemplateText,
                view.PlayerItemTemplateAttributeRowTemplate,
                self.PlayerRows,
                true);
            self.BuildPanelRows(
                view.MonsterRowsRectTransform,
                view.MonsterCategoryTemplateText,
                view.MonsterItemTemplateAttributeRowTemplate,
                self.MonsterRows,
                false);

            self.LayoutBuilt = true;
        }

        private static void BuildPanelRows(
            this UIFormUnitAttributeComponent self,
            RectTransform rowsRoot,
            Text categoryTemplate,
            MonoUIUnitAttributeRow rowTemplate,
            List<MonoUIUnitAttributeRow> targetRows,
            bool recordAttrTypes)
        {
            if (rowsRoot == null || categoryTemplate == null || rowTemplate == null)
            {
                return;
            }

            categoryTemplate.gameObject.SetActive(false);
            rowTemplate.gameObject.SetActive(false);

            for (int categoryIndex = 0; categoryIndex < GetCategoryCount(); ++categoryIndex)
            {
                Text categoryText = global::UnityEngine.Object.Instantiate(categoryTemplate, rowsRoot, false);
                categoryText.gameObject.name = $"Category_{categoryIndex}";
                categoryText.text = GetCategoryName(categoryIndex);
                categoryText.gameObject.SetActive(true);

                int attributeCount = GetCategoryAttributeCount(categoryIndex);
                for (int attributeIndex = 0; attributeIndex < attributeCount; ++attributeIndex)
                {
                    AttrType attrType = GetCategoryAttributeType(categoryIndex, attributeIndex);
                    MonoUIUnitAttributeRow row = global::UnityEngine.Object.Instantiate(rowTemplate, rowsRoot, false);
                    row.gameObject.name = $"Row_{categoryIndex}_{attributeIndex}";
                    row.LabelText.text = GetAttributeName(attrType);
                    row.ValueText.text = "--";
                    row.gameObject.SetActive(true);
                    targetRows.Add(row);

                    if (recordAttrTypes)
                    {
                        self.OrderedAttrTypes.Add(attrType);
                    }
                }
            }
        }

        private static void ClearDynamicRows(
            this UIFormUnitAttributeComponent self,
            RectTransform rowsRoot,
            List<MonoUIUnitAttributeRow> rows)
        {
            if (rowsRoot != null)
            {
                for (int i = rowsRoot.childCount - 1; i >= 0; --i)
                {
                    Transform child = rowsRoot.GetChild(i);
                    if (child == null)
                    {
                        continue;
                    }

                    MonoUIUnitAttributeRow row = child.GetComponent<MonoUIUnitAttributeRow>();
                    bool isTemplate = row != null && !child.gameObject.activeSelf;
                    bool isCategoryTemplate = row == null && !child.gameObject.activeSelf;
                    if (isTemplate || isCategoryTemplate)
                    {
                        continue;
                    }

                    global::UnityEngine.Object.Destroy(child.gameObject);
                }
            }

            rows.Clear();
        }

        private static void RefreshPanels(this UIFormUnitAttributeComponent self)
        {
            self.EnsureLayoutBuilt();
            MonoUIFormUnitAttribute view = self.View;
            if (view?.PlayerPanelRectTransform == null || view.MonsterPanelRectTransform == null)
            {
                return;
            }

            Unit playerUnit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            Unit monsterUnit = self.FindNearestMonster(playerUnit);

            self.RefreshPanel(
                view.PlayerPanelRectTransform,
                view.PlayerTitleText,
                view.PlayerTagsText,
                self.PlayerRows,
                playerUnit,
                "Player Attributes");
            self.RefreshPanel(
                view.MonsterPanelRectTransform,
                view.MonsterTitleText,
                view.MonsterTagsText,
                self.MonsterRows,
                monsterUnit,
                "Boss Attributes");
        }

        private static void RefreshPanel(
            this UIFormUnitAttributeComponent self,
            RectTransform panelRectTransform,
            Text titleText,
            Text tagsText,
            List<MonoUIUnitAttributeRow> rows,
            Unit unit,
            string defaultTitle)
        {
            if (panelRectTransform == null || titleText == null || tagsText == null)
            {
                return;
            }

            AbilitySystemComponent asc = unit?.GetComponent<SkillUnit>()?.ASC.As();
            bool isVisible = asc != null;
            panelRectTransform.gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            titleText.text = defaultTitle;
            tagsText.text = $"Tags: {GetTagsText(asc)}";

            int count = rows.Count < self.OrderedAttrTypes.Count ? rows.Count : self.OrderedAttrTypes.Count;
            for (int i = 0; i < count; ++i)
            {
                MonoUIUnitAttributeRow row = rows[i];
                if (row == null)
                {
                    continue;
                }

                row.ValueText.text = GetAttributeValueText(asc, self.OrderedAttrTypes[i]);
            }
        }

        private static Unit FindNearestMonster(this UIFormUnitAttributeComponent self, Unit playerUnit)
        {
            Scene currentScene = self.Scene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (playerUnit == null || unitComponent?.Children == null)
            {
                return null;
            }

            float3 playerPosition = GetUnitPosition(playerUnit, playerUnit.GetComponent<SkillUnit>()?.ASC.As());
            Unit nearestMonster = null;
            float nearestDistanceSqr = float.MaxValue;
            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is not Unit unit || unit.Id == playerUnit.Id)
                {
                    continue;
                }

                if ((UnitType)unit.Config().Type != UnitType.Monster)
                {
                    continue;
                }

                AbilitySystemComponent asc = unit.GetComponent<SkillUnit>()?.ASC.As();
                if (asc == null)
                {
                    continue;
                }

                float health = asc.Attributes?.GetCurrentValue(AttrType.Health) ?? 0f;
                if (health <= 0f)
                {
                    continue;
                }

                float3 unitPosition = GetUnitPosition(unit, asc);
                float distanceSqr = math.distancesq(playerPosition.xy, unitPosition.xy);
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                nearestMonster = unit;
            }

            return nearestMonster;
        }

        private static float3 GetUnitPosition(Unit unit, AbilitySystemComponent asc)
        {
            if (asc?.Owner != null)
            {
                Vector3 ownerPosition = asc.Owner.transform.position;
                return new float3(ownerPosition.x, ownerPosition.y, ownerPosition.z);
            }

            return unit == null ? float3.zero : unit.Position;
        }

        private static string GetTagsText(AbilitySystemComponent asc)
        {
            if (asc?.OwnedTags == null || asc.OwnedTags.IsEmpty)
            {
                return "None";
            }

            List<string> tags = new List<string>();
            foreach (GameplayTag gameplayTag in asc.OwnedTags.Tags)
            {
                tags.Add(gameplayTag.Name);
            }

            return tags.Count > 0 ? string.Join(", ", tags) : "None";
        }

        private static string GetAttributeValueText(AbilitySystemComponent asc, AttrType attrType)
        {
            Attribute attribute = asc?.Attributes?.GetAttribute(attrType);
            if (attribute == null)
            {
                return "--";
            }

            string valueText;
            switch (attrType)
            {
                case AttrType.Health:
                    valueText = $"{FormatIntegral(attribute.CurrentValue)}/{FormatIntegral(asc.Attributes.GetCurrentValue(AttrType.MaxHealth))}";
                    break;
                case AttrType.Mana:
                    valueText = $"{FormatIntegral(attribute.CurrentValue)}/{FormatIntegral(asc.Attributes.GetCurrentValue(AttrType.MaxMana))}";
                    break;
                case AttrType.CritRate:
                case AttrType.CooldownReduction:
                    valueText = $"{attribute.CurrentValue * 100f:F1}%";
                    break;
                case AttrType.CritDamage:
                    valueText = $"{attribute.CurrentValue * 100f:F0}%";
                    break;
                case AttrType.Level:
                case AttrType.Experience:
                case AttrType.MaxHealth:
                case AttrType.MaxMana:
                    valueText = FormatIntegral(attribute.CurrentValue);
                    break;
                default:
                    valueText = $"{attribute.CurrentValue:F1}";
                    break;
            }

            float diff = attribute.CurrentValue - attribute.BaseValue;
            if (math.abs(diff) <= 0.01f)
            {
                return valueText;
            }

            string diffText = diff > 0f
                ? $"+{diff:F1}"
                : $"{diff:F1}";
            return $"{valueText} ({diffText})";
        }

        private static string FormatIntegral(float value)
        {
            return $"{value:F0}";
        }

        private static int GetCategoryCount()
        {
            return 6;
        }

        private static string GetCategoryName(int categoryIndex)
        {
            switch (categoryIndex)
            {
                case 0:
                    return "Health";
                case 1:
                    return "Mana";
                case 2:
                    return "Combat";
                case 3:
                    return "Speed";
                case 4:
                    return "Critical";
                case 5:
                    return "Other";
                default:
                    return string.Empty;
            }
        }

        private static int GetCategoryAttributeCount(int categoryIndex)
        {
            switch (categoryIndex)
            {
                case 0:
                case 1:
                    return 3;
                case 2:
                    return 4;
                case 3:
                    return 3;
                case 4:
                    return 2;
                case 5:
                    return 2;
                default:
                    return 0;
            }
        }

        private static AttrType GetCategoryAttributeType(int categoryIndex, int attributeIndex)
        {
            switch (categoryIndex)
            {
                case 0:
                    switch (attributeIndex)
                    {
                        case 0: return AttrType.Health;
                        case 1: return AttrType.MaxHealth;
                        case 2: return AttrType.HealthRegen;
                        default: return AttrType.Health;
                    }
                case 1:
                    switch (attributeIndex)
                    {
                        case 0: return AttrType.Mana;
                        case 1: return AttrType.MaxMana;
                        case 2: return AttrType.ManaRegen;
                        default: return AttrType.Mana;
                    }
                case 2:
                    switch (attributeIndex)
                    {
                        case 0: return AttrType.Attack;
                        case 1: return AttrType.Defense;
                        case 2: return AttrType.MagicPower;
                        case 3: return AttrType.MagicDefense;
                        default: return AttrType.Attack;
                    }
                case 3:
                    switch (attributeIndex)
                    {
                        case 0: return AttrType.MoveSpeed;
                        case 1: return AttrType.AttackSpeed;
                        case 2: return AttrType.CooldownReduction;
                        default: return AttrType.MoveSpeed;
                    }
                case 4:
                    switch (attributeIndex)
                    {
                        case 0: return AttrType.CritRate;
                        case 1: return AttrType.CritDamage;
                        default: return AttrType.CritRate;
                    }
                case 5:
                    switch (attributeIndex)
                    {
                        case 0: return AttrType.Level;
                        case 1: return AttrType.Experience;
                        default: return AttrType.Level;
                    }
                default:
                    return AttrType.Health;
            }
        }

        private static string GetAttributeName(AttrType attrType)
        {
            switch (attrType)
            {
                case AttrType.Health: return "Health";
                case AttrType.MaxHealth: return "Max Health";
                case AttrType.HealthRegen: return "Health Regen";
                case AttrType.Mana: return "Mana";
                case AttrType.MaxMana: return "Max Mana";
                case AttrType.ManaRegen: return "Mana Regen";
                case AttrType.Attack: return "Attack";
                case AttrType.Defense: return "Defense";
                case AttrType.MagicPower: return "Magic Power";
                case AttrType.MagicDefense: return "Magic Defense";
                case AttrType.MoveSpeed: return "Move Speed";
                case AttrType.AttackSpeed: return "Attack Speed";
                case AttrType.CooldownReduction: return "Cooldown Reduction";
                case AttrType.CritRate: return "Crit Rate";
                case AttrType.CritDamage: return "Crit Damage";
                case AttrType.Level: return "Level";
                case AttrType.Experience: return "Experience";
                default: return attrType.ToString();
            }
        }
    }
}

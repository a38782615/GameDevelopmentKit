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
                view.PlayerCategoryTemplateTextMeshProUGUI == null ||
                view.MonsterCategoryTemplateTextMeshProUGUI == null ||
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
                view.PlayerCategoryTemplateTextMeshProUGUI,
                view.PlayerItemTemplateAttributeRowTemplate,
                self.PlayerRows,
                true);
            self.BuildPanelRows(
                view.MonsterRowsRectTransform,
                view.MonsterCategoryTemplateTextMeshProUGUI,
                view.MonsterItemTemplateAttributeRowTemplate,
                self.MonsterRows,
                false);

            self.LayoutBuilt = true;
        }

        private static void BuildPanelRows(
            this UIFormUnitAttributeComponent self,
            RectTransform rowsRoot,
            Component categoryTemplate,
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
                Component categoryText = global::UnityEngine.Object.Instantiate(categoryTemplate, rowsRoot, false);
                categoryText.gameObject.name = $"Category_{categoryIndex}";
                SetText(categoryText, GetCategoryName(categoryIndex));
                categoryText.gameObject.SetActive(true);

                int attributeCount = GetCategoryAttributeCount(categoryIndex);
                for (int attributeIndex = 0; attributeIndex < attributeCount; ++attributeIndex)
                {
                    int attrType = GetCategoryAttributeType(categoryIndex, attributeIndex);
                    MonoUIUnitAttributeRow row = global::UnityEngine.Object.Instantiate(rowTemplate, rowsRoot, false);
                    row.gameObject.name = $"Row_{categoryIndex}_{attributeIndex}";
                    row.LabelTextMeshProUGUI.text = GetAttributeName(attrType);
                    row.ValueTextMeshProUGUI.text = "--";
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
                view.PlayerTitleTextMeshProUGUI,
                view.PlayerTagsTextMeshProUGUI,
                self.PlayerRows,
                playerUnit,
                "Player Attributes");
            self.RefreshPanel(
                view.MonsterPanelRectTransform,
                view.MonsterTitleTextMeshProUGUI,
                view.MonsterTagsTextMeshProUGUI,
                self.MonsterRows,
                monsterUnit,
                "Boss Attributes");
        }

        private static void RefreshPanel(
            this UIFormUnitAttributeComponent self,
            RectTransform panelRectTransform,
            Component titleText,
            Component tagsText,
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

            SetText(titleText, defaultTitle);
            SetText(tagsText, $"Tags: {GetTagsText(asc)}");

            int count = rows.Count < self.OrderedAttrTypes.Count ? rows.Count : self.OrderedAttrTypes.Count;
            for (int i = 0; i < count; ++i)
            {
                MonoUIUnitAttributeRow row = rows[i];
                if (row == null)
                {
                    continue;
                }

                row.ValueTextMeshProUGUI.text = GetAttributeValueText(asc, self.OrderedAttrTypes[i]);
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

                float health = asc.Attributes?.GetValue(global::ET.NumericType.Hp) ?? 0f;
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
            Transform ownerTransform = asc?.GetOwnerTransform();
            if (ownerTransform != null)
            {
                Vector3 ownerPosition = ownerTransform.position;
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

        private static void SetText(Component textComponent, string value)
        {
            if (textComponent == null)
            {
                return;
            }

            if (textComponent is TMPro.TMP_Text tmpText)
            {
                tmpText.text = value;
                return;
            }

            if (textComponent is Text text)
            {
                text.text = value;
            }
        }

        private static string GetAttributeValueText(AbilitySystemComponent asc, int attrType)
        {
            AttrCmp attribute = asc?.Attributes?.GetAttribute(attrType);
            if (attribute == null)
            {
                return "--";
            }

            string valueText;
            switch (attrType)
            {
                case global::ET.NumericType.Hp:
                    valueText = $"{FormatIntegral(attribute.ValueFloat)}/{FormatIntegral(asc.Attributes.GetValue(global::ET.NumericType.MaxHp))}";
                    break;
                case global::ET.NumericType.Mp:
                    valueText = $"{FormatIntegral(attribute.ValueFloat)}/{FormatIntegral(asc.Attributes.GetValue(global::ET.NumericType.MaxMp))}";
                    break;
                case global::ET.NumericType.CriticalProbability:
                case global::ET.NumericType.SkillCD:
                    valueText = $"{attribute.ValueFloat * 100f:F1}%";
                    break;
                case global::ET.NumericType.CriticalStrikeHarm:
                    valueText = $"{attribute.ValueFloat * 100f:F0}%";
                    break;
                case global::ET.NumericType.Level:
                case global::ET.NumericType.Experience:
                case global::ET.NumericType.MaxHp:
                case global::ET.NumericType.MaxMp:
                    valueText = FormatIntegral(attribute.ValueFloat);
                    break;
                default:
                    valueText = $"{attribute.ValueFloat:F1}";
                    break;
            }

            return valueText;
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

        private static int GetCategoryAttributeType(int categoryIndex, int attributeIndex)
        {
            switch (categoryIndex)
            {
                case 0:
                    switch (attributeIndex)
                    {
                        case 0: return global::ET.NumericType.Hp;
                        case 1: return global::ET.NumericType.MaxHp;
                        case 2: return global::ET.NumericType.HPRec;
                        default: return global::ET.NumericType.Hp;
                    }
                case 1:
                    switch (attributeIndex)
                    {
                        case 0: return global::ET.NumericType.Mp;
                        case 1: return global::ET.NumericType.MaxMp;
                        case 2: return global::ET.NumericType.MPRec;
                        default: return global::ET.NumericType.Mp;
                    }
                case 2:
                    switch (attributeIndex)
                    {
                        case 0: return global::ET.NumericType.Attack;
                        case 1: return global::ET.NumericType.Armor;
                        case 2: return global::ET.NumericType.MagicStrength;
                        case 3: return global::ET.NumericType.MagicResistance;
                        default: return global::ET.NumericType.Attack;
                    }
                case 3:
                    switch (attributeIndex)
                    {
                        case 0: return global::ET.NumericType.Speed;
                        case 1: return global::ET.NumericType.AttackSpeed;
                        case 2: return global::ET.NumericType.SkillCD;
                        default: return global::ET.NumericType.Speed;
                    }
                case 4:
                    switch (attributeIndex)
                    {
                        case 0: return global::ET.NumericType.CriticalProbability;
                        case 1: return global::ET.NumericType.CriticalStrikeHarm;
                        default: return global::ET.NumericType.CriticalProbability;
                    }
                case 5:
                    switch (attributeIndex)
                    {
                        case 0: return global::ET.NumericType.Level;
                        case 1: return global::ET.NumericType.Experience;
                        default: return global::ET.NumericType.Level;
                    }
                default:
                    return global::ET.NumericType.Hp;
            }
        }

        private static string GetAttributeName(int attrType)
        {
            return global::ET.NumericType.GetAttributeName(attrType);
        }
    }
}

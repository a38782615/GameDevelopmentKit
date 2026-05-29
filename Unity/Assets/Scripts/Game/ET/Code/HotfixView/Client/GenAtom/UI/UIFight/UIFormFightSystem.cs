using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    {
        [EntitySystem]
        private static void Awake(this UIFormFight self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this UIFormFight self)
        {
            
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormFight self)
        {
            self.LoadFightUnitsAsync().Forget();
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormFight self, bool isShutdown)
        {
            self.ClearFightUnits();
        }

        private static async UniTaskVoid LoadFightUnitsAsync(this UIFormFight self)
        {
            if (self.IsLoadingFightUnits)
            {
                return;
            }

            self.IsLoadingFightUnits = true;
            try
            {
                self.ClearFightUnits();

                MonoUIFormFight view = self.View;
                if (view?.L0RectTransform == null || view.R0RectTransform == null)
                {
                    Log.Warning("[UIFormFight] Missing L0 or R0 slot.");
                    return;
                }

                await self.CreateFirstHeroAsync(view.L0RectTransform);
                if (self.IsDisposed)
                {
                    return;
                }

                await self.CreateFirstMonsterAsync(view.R0RectTransform);
            }
            finally
            {
                if (!self.IsDisposed)
                {
                    self.IsLoadingFightUnits = false;
                }
            }
        }

        private static async UniTask CreateFirstHeroAsync(this UIFormFight self, RectTransform slot)
        {
            if (Tables.Instance?.DTHero?.DataList == null || Tables.Instance.DTHero.DataList.Count == 0)
            {
                Log.Warning("[UIFormFight] Missing hero config.");
                return;
            }

            DRHero config = Tables.Instance.DTHero.DataList[0];
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = IdGenerater.Instance.GenerateId();
            unitInfo.Type = config.UnitConfigId_Ref.Type;
            unitInfo.ConfigId = config.UnitConfigId;
            unitInfo.Position = float3.zero;
            unitInfo.Forward = new float2(1f, 0f).ToModeDirection();

            Unit unit = await self.CreateFightUnitAsync(unitInfo, slot);
            PlayerComponent playerComponent = self.Scene()?.Root()?.GetComponent<PlayerComponent>();
            if (unit != null && playerComponent != null)
            {
                playerComponent.MyId = unit.Id;
            }
        }

        private static async UniTask CreateFirstMonsterAsync(this UIFormFight self, RectTransform slot)
        {
            if (Tables.Instance?.DTMonster?.DataList == null || Tables.Instance.DTMonster.DataList.Count == 0)
            {
                Log.Warning("[UIFormFight] Missing monster config.");
                return;
            }

            DRMonster config = Tables.Instance.DTMonster.DataList[0];
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = IdGenerater.Instance.GenerateId();
            unitInfo.Type = config.UnitConfigId_Ref.Type;
            unitInfo.ConfigId = config.UnitConfigId;
            unitInfo.Position = float3.zero;
            unitInfo.Forward = new float2(-1f, 0f).ToModeDirection();

            await self.CreateFightUnitAsync(unitInfo, slot);
        }

        private static async UniTask<Unit> CreateFightUnitAsync(this UIFormFight self, UnitInfo unitInfo, RectTransform slot)
        {
            Scene currentScene = self.Scene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Warning("[UIFormFight] Missing UnitComponent.");
                return null;
            }

            Unit oldUnit = unitComponent.Get(unitInfo.UnitId);
            oldUnit?.Dispose();

            Unit unit = UnitFactory.Create(currentScene, unitInfo);
            if (!self.FightUnitIds.Contains(unit.Id))
            {
                self.FightUnitIds.Add(unit.Id);
            }

            UIWidgetHeadItem headItem = await self.LoadChildUIWidgetAsync<UIWidgetHeadItem>(UGFUIEntityId.UIHeadItem);
            if (self.IsDisposed)
            {
                return unit;
            }

            self.FightHeadItems.Add(headItem);
            self.AttachHeadItemToSlot(headItem, slot);
            self.BindUnitView(unit, headItem);
            headItem.TryDynamicOpen();
            return unit;
        }

        private static void AttachHeadItemToSlot(this UIFormFight self, UIWidgetHeadItem headItem, RectTransform slot)
        {
            RectTransform rectTransform = headItem?.CachedRectTransform;
            if (rectTransform == null || slot == null)
            {
                return;
            }

            rectTransform.SetParent(slot, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private static void BindUnitView(this UIFormFight self, Unit unit, UIWidgetHeadItem headItem)
        {
            if (unit == null || headItem?.CachedRectTransform == null)
            {
                return;
            }

            unit.GetOrAddComponent<EntityBody>();
            SkillUnit skillUnit = unit.GetComponent<SkillUnit>() ?? unit.AddComponent<SkillUnit>();
            GameObject viewGameObject = headItem.CachedRectTransform.gameObject;

            GameObjectComponent gameObjectComponent = unit.GetOrAddComponent<GameObjectComponent>();
            gameObjectComponent.GameObject = viewGameObject;

            AbilitySystemComponent asc = skillUnit.ASC.As();
            asc?.SetOwnerObject(viewGameObject);
        }

        private static void ClearFightUnits(this UIFormFight self)
        {
            UnitComponent unitComponent = self.Scene()?.GetComponent<UnitComponent>();
            if (unitComponent != null)
            {
                foreach (long unitId in self.FightUnitIds)
                {
                    unitComponent.Remove(unitId);
                }
            }

            self.FightUnitIds.Clear();
            self.FightHeadItems.Clear();
        }
    }
}

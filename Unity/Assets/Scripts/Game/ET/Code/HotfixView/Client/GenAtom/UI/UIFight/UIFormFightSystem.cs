using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    { 

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormFight self)
        {
            self.LPos = new RectTransform[4] { self.View.L0RectTransform, self.View.L1RectTransform, self.View.L2RectTransform, self.View.L3RectTransform };
            self.RPos = new RectTransform[4] { self.View.R0RectTransform, self.View.R1RectTransform, self.View.R2RectTransform, self.View.R3RectTransform };
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

                await self.CreateFirstHeroAsync(0);
                if (self.IsDisposed)
                {
                    return;
                }

                await self.CreateFirstMonsterAsync(0);
            }
            finally
            {
                if (!self.IsDisposed)
                {
                    self.IsLoadingFightUnits = false;
                }
            }
        }

        private static async UniTask CreateFirstHeroAsync(this UIFormFight self, int pos)
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
            unitInfo.PosIdx = pos;

            Unit unit = await self.CreateFightUnitAsync(unitInfo);
            PlayerComponent playerComponent = self.Scene()?.Root()?.GetComponent<PlayerComponent>();
            if (unit != null && playerComponent != null)
            {
                playerComponent.MyId = unit.Id;
            }
        }

        private static async UniTask CreateFirstMonsterAsync(this UIFormFight self, int pos)
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
            unitInfo.PosIdx = pos;

            await self.CreateFightUnitAsync(unitInfo);
        }

        private static async UniTask<Unit> CreateFightUnitAsync(this UIFormFight self, UnitInfo unitInfo)
        {
            Scene currentScene = self.Scene();
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

            self.AttachHeadItemToSlot(unitInfo, headItem);
            self.BindUnitView(unit, headItem);
            headItem.TryDynamicOpen();
            return unit;
        }

        private static void AttachHeadItemToSlot(this UIFormFight self, UnitInfo unit, UIWidgetHeadItem headItem)
        {
            RectTransform rectTransform = headItem?.CachedRectTransform;
            if (rectTransform == null)
            {
                return;
            }
            var slotRect = (unit.Type == (int)UnitType.Player) ? self.LPos[unit.PosIdx] : self.RPos[unit.PosIdx];
            
            rectTransform.SetParent(slotRect, false);
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
            UnitComponent unitComponent = self.Root().CurrentScene()?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }
            foreach (long unitId in self.FightUnitIds)
            {
                unitComponent.Remove(unitId);
            }
            self.FightUnitIds.Clear();
        }
    }
}

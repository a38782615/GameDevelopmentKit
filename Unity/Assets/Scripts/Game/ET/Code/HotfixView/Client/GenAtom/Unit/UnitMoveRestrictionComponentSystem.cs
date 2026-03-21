using System;

namespace ET.Client
{
    [EntitySystemOf(typeof(UnitMoveRestrictionComponent))]
    [FriendOf(typeof(UnitMoveRestrictionComponent))]
    public static partial class UnitMoveRestrictionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UnitMoveRestrictionComponent self)
        {
            self.Bind();
        }

        [EntitySystem]
        private static void Destroy(this UnitMoveRestrictionComponent self)
        {
            self.UnregisterTagListeners();
            self.ASC = default;
        }

        public static void Bind(this UnitMoveRestrictionComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null)
            {
                return;
            }

            AbilitySystemComponent asc = unit.GetComponent<SkillUnit>()?.ASC.As();
            if (!ReferenceEquals(self.ASC.As(), asc))
            {
                self.UnregisterTagListeners();
                self.ASC = asc;
            }

            self.RegisterTagListeners();
            self.RefreshMoveRestriction();
        }

        private static void RegisterTagListeners(this UnitMoveRestrictionComponent self)
        {
            if (self.IsListening)
            {
                return;
            }

            AbilitySystemComponent asc = self.ASC.As();
            if (asc?.OwnedTags == null)
            {
                return;
            }

            asc.OwnedTags.OnTagAdded += self.OnTagAdded;
            asc.OwnedTags.OnTagRemoved += self.OnTagRemoved;
            self.IsListening = true;
        }

        private static void UnregisterTagListeners(this UnitMoveRestrictionComponent self)
        {
            if (!self.IsListening)
            {
                return;
            }

            AbilitySystemComponent asc = self.ASC.As();
            if (asc?.OwnedTags != null)
            {
                asc.OwnedTags.OnTagAdded -= self.OnTagAdded;
                asc.OwnedTags.OnTagRemoved -= self.OnTagRemoved;
            }

            self.IsListening = false;
        }

        private static void OnTagAdded(this UnitMoveRestrictionComponent self, GameplayTag tag)
        {
            if (tag != GameplayTagLibrary.Buff_DeBuff_Stun)
            {
                return;
            }

            self.RefreshMoveRestriction();
        }

        private static void OnTagRemoved(this UnitMoveRestrictionComponent self, GameplayTag tag)
        {
            if (tag != GameplayTagLibrary.Buff_DeBuff_Stun)
            {
                return;
            }

            self.RefreshMoveRestriction();
        }

        private static void RefreshMoveRestriction(this UnitMoveRestrictionComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            AbilitySystemComponent asc = self.ASC.As();
            bool isBlocked = asc?.OwnedTags?.HasTag(GameplayTagLibrary.Buff_DeBuff_Stun) ?? false;

            MoveRestrictionComponent restrictionComponent = unit?.GetComponent<MoveRestrictionComponent>();
            if (unit != null && restrictionComponent == null)
            {
                restrictionComponent = unit.AddComponent<MoveRestrictionComponent>();
            }

            bool changed = restrictionComponent != null && restrictionComponent.SetBlocked(isBlocked);
            if (!isBlocked || unit == null)
            {
                return;
            }

            if (changed || !unit.IsMoveArrived())
            {
                unit.StopMove(false);
                self.TryNotifyServerStop(unit);
            }
        }

        private static void TryNotifyServerStop(this UnitMoveRestrictionComponent self, Unit unit)
        {
            Scene currentScene = unit.Root()?.CurrentScene();
            if (currentScene == null)
            {
                return;
            }

            Unit playerUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (playerUnit == null || playerUnit.Id != unit.Id)
            {
                return;
            }

            unit.Root().GetComponent<ClientSenderComponent>()?.Send(C2M_Stop.Create());
        }
    }
}

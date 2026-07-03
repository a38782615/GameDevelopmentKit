using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormFight self)
        {
            self.OpenAllUIWidgets();
            FightComponent fightComponent = self.Root().CurrentScene()?.GetComponent<FightComponent>();
            if (fightComponent == null)
            {
                return;
            }

            fightComponent.CreateLocalUnitsFromTables().Forget();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormFight self, bool isShutdown)
        {
        }
    }
}

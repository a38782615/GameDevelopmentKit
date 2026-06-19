namespace ET.Client
{
    public static partial class UnitHelper
    {
        public static Unit GetMyUnitFromClientScene(Scene root)
        {
            return GetMyUnitFromCurrentScene(root.CurrentScene());
        }

        public static Unit GetMyUnitFromCurrentScene(Scene currentScene)
        {
            if (currentScene == null || currentScene.IsDisposed)
            {
                return null;
            }

            PlayerComponent playerComponent = currentScene.Root().GetComponent<PlayerComponent>();
            if (playerComponent == null)
            {
                return null;
            }

            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return null;
            }

            return unitComponent.Get(playerComponent.MyId);
        }
    }
}

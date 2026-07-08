namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class PlayerDataComponent : Entity, IAwake, IDestroy
    {
        public PlayerData PlayerData;
    }
}

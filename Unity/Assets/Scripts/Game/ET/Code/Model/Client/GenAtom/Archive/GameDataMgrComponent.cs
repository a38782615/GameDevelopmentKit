namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class GameDataMgrComponent : Entity, IAwake, IDestroy
    {
        public PlayerData PlayerData;
    }
}

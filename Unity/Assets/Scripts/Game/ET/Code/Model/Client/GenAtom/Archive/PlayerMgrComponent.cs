namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class PlayerMgrComponent : Entity, IAwake, IDestroy
    {
        public PlayerData PlayerData;
    }
}

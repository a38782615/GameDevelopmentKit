namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class FightComponent : Entity, IAwake, IDestroy
    {
        public int CurrentMap = 0;
        public int CurrentLevel = 0;
    }
}

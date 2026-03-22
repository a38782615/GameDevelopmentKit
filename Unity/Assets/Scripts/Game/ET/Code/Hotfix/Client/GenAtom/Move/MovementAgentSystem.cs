namespace ET.Client
{
    [EntitySystemOf(typeof(MovementAgent))]
    [FriendOf(typeof(MovementAgent))]
    public static partial class MovementAgentSystem
    {
        [EntitySystem]
        private static void Awake(this MovementAgent self)
        {
            self.Initialize();
            self.Scene()?.GetComponent<MovementSimulationComponent>()?.Register(self);
        }

        [EntitySystem]
        private static void Destroy(this MovementAgent self)
        {
            self.Scene()?.GetComponent<MovementSimulationComponent>()?.Unregister(self);
            self.AgentNo = -1;
            self.SetVelocity(Unity.Mathematics.float2.zero);
        }
    }
}

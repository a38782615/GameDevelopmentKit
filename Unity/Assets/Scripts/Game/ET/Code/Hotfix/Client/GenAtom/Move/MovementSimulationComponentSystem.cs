using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(MovementSimulationComponent))]
    [FriendOf(typeof(MovementSimulationComponent))]
    [FriendOf(typeof(MovementAgent))]
    [FriendOf(typeof(global::ET.Move2DComponent))]
    public static partial class MovementSimulationComponentSystem
    {
        private const float MinReachDistance = 0.05f;
        private const float ReachDistanceFactor = 0.4f;

        [Invoke(TimerInvokeType.MovementSimulationTimer)]
        public class MovementSimulationTimer : ATimer<MovementSimulationComponent>
        {
            protected override void Run(MovementSimulationComponent self)
            {
                try
                {
                    self.Tick();
                }
                catch (System.Exception e)
                {
                    Log.Error($"movement simulation timer error: {self.Id}\n{e}");
                }
            }
        }

        [EntitySystem]
        private static void Awake(this MovementSimulationComponent self)
        {
            self.FixedTimeStep = 1f / 30f;
            self.MaxDeltaTime = 0.1f;
            self.Accumulator = 0f;
            self.LastStepTime = TimeInfo.Instance.ClientNow();
            self.ResetSimulator();
            self.Timer = self.Root().GetComponent<TimerComponent>().NewFrameTimer(TimerInvokeType.MovementSimulationTimer, self);
        }

        [EntitySystem]
        private static void Destroy(this MovementSimulationComponent self)
        {
            self.Root().GetComponent<TimerComponent>()?.Remove(ref self.Timer);
            self.Clear();
        }

        public static void Register(this MovementSimulationComponent self, MovementAgent agent)
        {
            if (self == null || agent == null || agent.IsDisposed)
            {
                return;
            }

            Unit unit = agent.GetUnit();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            self.Agents[unit.Id] = agent;
            self.IsAgentDirty = true;
        }

        public static void Unregister(this MovementSimulationComponent self, MovementAgent agent)
        {
            if (self == null || agent == null)
            {
                return;
            }

            Unit unit = agent.GetUnit();
            if (unit != null)
            {
                self.Agents.Remove(unit.Id);
            }

            agent.AgentNo = -1;
            self.IsAgentDirty = true;
        }

        public static void Tick(this MovementSimulationComponent self)
        {
            if (self == null)
            {
                return;
            }

            long now = TimeInfo.Instance.ClientNow();
            if (self.LastStepTime == 0)
            {
                self.LastStepTime = now;
                return;
            }

            float deltaTime = math.min((now - self.LastStepTime) / 1000f, self.MaxDeltaTime);
            self.LastStepTime = now;
            if (deltaTime <= 0f)
            {
                return;
            }

            self.Accumulator += deltaTime;
            int safeStepCount = 0;
            while (self.Accumulator >= self.FixedTimeStep && safeStepCount < 4)
            {
                self.StepSimulation();
                self.Accumulator -= self.FixedTimeStep;
                ++safeStepCount;
            }

            if (safeStepCount >= 4)
            {
                self.Accumulator = 0f;
            }
        }

        public static void Clear(this MovementSimulationComponent self)
        {
            if (self == null)
            {
                return;
            }

            foreach (EntityRef<MovementAgent> agentRef in self.IndexedAgents)
            {
                MovementAgent agent = agentRef.As();
                if (agent != null)
                {
                    agent.AgentNo = -1;
                    agent.Velocity = float2.zero;
                }
            }

            self.Agents.Clear();
            self.IndexedAgents.Clear();
            self.Accumulator = 0f;
            self.LastStepTime = 0;
            self.ResetSimulator();
        }

        private static void StepSimulation(this MovementSimulationComponent self)
        {
            self.EnsureSimulatorBuilt();
            if (self.IndexedAgents.Count == 0)
            {
                return;
            }

            for (int i = 0; i < self.IndexedAgents.Count; ++i)
            {
                MovementAgent agent = self.IndexedAgents[i].As();
                Unit unit = agent?.GetUnit();
                if (agent == null || unit == null || unit.IsDisposed || agent.AgentNo < 0)
                {
                    self.IsAgentDirty = true;
                    continue;
                }

                agent.RefreshDynamicSettings();
                self.Simulator.setAgentPosition(agent.AgentNo, ToRvoVector(agent.GetPosition()));
                self.Simulator.setAgentVelocity(agent.AgentNo, ToRvoVector(agent.Velocity));
                self.Simulator.setAgentRadius(agent.AgentNo, agent.Radius);
                self.Simulator.setAgentNeighborDist(agent.AgentNo, agent.NeighborDist);
                self.Simulator.setAgentMaxNeighbors(agent.AgentNo, agent.MaxNeighbors);
                self.Simulator.setAgentTimeHorizon(agent.AgentNo, agent.TimeHorizon);
                self.Simulator.setAgentTimeHorizonObst(agent.AgentNo, agent.TimeHorizonObst);
                self.Simulator.setAgentMaxSpeed(agent.AgentNo, agent.MaxSpeed);
                self.Simulator.setAgentPrefVelocity(agent.AgentNo, ToRvoVector(self.GetPreferredVelocity(agent)));
            }

            if (self.IsAgentDirty)
            {
                self.EnsureSimulatorBuilt();
                if (self.IndexedAgents.Count == 0)
                {
                    return;
                }
            }

            self.Simulator.doStep();

            for (int i = 0; i < self.IndexedAgents.Count; ++i)
            {
                MovementAgent agent = self.IndexedAgents[i].As();
                Unit unit = agent?.GetUnit();
                if (agent == null || unit == null || unit.IsDisposed || agent.AgentNo < 0)
                {
                    self.IsAgentDirty = true;
                    continue;
                }

                float2 nextPosition = ToFloat2(self.Simulator.getAgentPosition(agent.AgentNo));
                float2 velocity = ToFloat2(self.Simulator.getAgentVelocity(agent.AgentNo));
                agent.Velocity = velocity;

                if (math.distancesq(unit.Position.ToPlanar(), nextPosition) > 0.000001f)
                {
                    unit.Position = nextPosition.ToModePosition();
                }

                if (math.abs(velocity.x) > 0.001f)
                {
                    unit.Rotation = velocity.x < 0f ? quaternion.RotateY(math.PI) : quaternion.identity;
                }

                self.PostStep(agent, unit);
            }
        }

        private static void EnsureSimulatorBuilt(this MovementSimulationComponent self)
        {
            if (self == null || !self.IsAgentDirty)
            {
                return;
            }

            self.IndexedAgents.Clear();
            foreach (EntityRef<MovementAgent> agentRef in self.Agents.Values)
            {
                MovementAgent agent = agentRef.As();
                if (agent == null || agent.IsDisposed || agent.GetUnit() == null)
                {
                    continue;
                }

                self.IndexedAgents.Add(agent);
            }

            self.IndexedAgents.Sort(CompareMovementAgent);
            self.ResetSimulator();

            for (int i = 0; i < self.IndexedAgents.Count; ++i)
            {
                MovementAgent agent = self.IndexedAgents[i].As();
                if (agent == null)
                {
                    continue;
                }

                agent.RefreshDynamicSettings();
                agent.AgentNo = self.Simulator.addAgent(
                    ToRvoVector(agent.GetPosition()),
                    agent.NeighborDist,
                    agent.MaxNeighbors,
                    agent.TimeHorizon,
                    agent.TimeHorizonObst,
                    agent.Radius,
                    agent.MaxSpeed,
                    ToRvoVector(agent.Velocity));
            }

            self.IsAgentDirty = false;
        }

        private static void PostStep(this MovementSimulationComponent self, MovementAgent agent, Unit unit)
        {
            global::ET.Move2DComponent moveComponent = unit.GetComponent<global::ET.Move2DComponent>();
            if (moveComponent == null || moveComponent.StartTime == 0 || moveComponent.Targets.Count == 0)
            {
                return;
            }

            self.TryAdvanceTarget(moveComponent, unit, agent.Radius, true);
        }

        private static float2 GetPreferredVelocity(this MovementSimulationComponent self, MovementAgent agent)
        {
            Unit unit = agent.GetUnit();
            if (unit == null)
            {
                return float2.zero;
            }

            global::ET.Move2DComponent moveComponent = unit.GetComponent<global::ET.Move2DComponent>();
            if (moveComponent == null || moveComponent.StartTime == 0 || moveComponent.Targets.Count == 0 || !global::ET.UnitMoveExtensions.IsMoveAllowed(unit))
            {
                return float2.zero;
            }

            if (!self.TryAdvanceTarget(moveComponent, unit, agent.Radius, false))
            {
                return float2.zero;
            }

            float2 toTarget = moveComponent.NextTarget - unit.Position.ToPlanar();
            float distanceSq = math.lengthsq(toTarget);
            if (distanceSq < 0.000001f)
            {
                return float2.zero;
            }

            float speed = moveComponent.Speed > 0.0001f ? moveComponent.Speed : agent.MaxSpeed;
            return math.normalize(toTarget) * speed;
        }

        private static bool TryAdvanceTarget(this MovementSimulationComponent self, global::ET.Move2DComponent moveComponent, Unit unit, float radius, bool snapFinalTarget)
        {
            float reachDistance = math.max(radius * ReachDistanceFactor, MinReachDistance);
            float reachDistanceSq = reachDistance * reachDistance;
            float2 currentPosition = unit.Position.ToPlanar();

            while (moveComponent.StartTime != 0 && moveComponent.N < moveComponent.Targets.Count)
            {
                float2 target = moveComponent.Targets[moveComponent.N];
                if (math.distancesq(currentPosition, target) > reachDistanceSq)
                {
                    return true;
                }

                if (moveComponent.N >= moveComponent.Targets.Count - 1)
                {
                    if (snapFinalTarget && math.distancesq(currentPosition, target) > 0.000001f)
                    {
                        unit.Position = target.ToModePosition();
                    }

                    global::ET.Move2DComponentSystem.Stop(moveComponent, true);
                    return false;
                }

                ++moveComponent.N;
                float2 faceV = global::ET.Move2DComponentSystem.GetFaceV(moveComponent);
                moveComponent.From = unit.Rotation;
                moveComponent.To = global::ET.Move2DComponentSystem.GetFacingRotation(moveComponent, faceV, unit.Rotation);
                unit.Rotation = moveComponent.To;
            }

            return moveComponent.StartTime != 0 && moveComponent.N < moveComponent.Targets.Count;
        }

        private static void ResetSimulator(this MovementSimulationComponent self)
        {
            self.Simulator.Clear();
            self.Simulator.setTimeStep(self.FixedTimeStep > 0f ? self.FixedTimeStep : 1f / 30f);
            self.Simulator.SetNumWorkers(1);
        }

        private static int CompareMovementAgent(EntityRef<MovementAgent> aRef, EntityRef<MovementAgent> bRef)
        {
            MovementAgent a = aRef.As();
            MovementAgent b = bRef.As();
            long aId = a?.GetUnit()?.Id ?? 0;
            long bId = b?.GetUnit()?.Id ?? 0;
            return aId.CompareTo(bId);
        }

        private static RVO.Vector2 ToRvoVector(float2 value)
        {
            return new RVO.Vector2(value.x, value.y);
        }

        private static float2 ToFloat2(RVO.Vector2 value)
        {
            return new float2(value.x(), value.y());
        }
    }
}

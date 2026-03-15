using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(Move2DComponent))]
    [FriendOf(typeof(Move2DComponent))]
    public static partial class Move2DComponentSystem
    {
        [Invoke(TimerInvokeType.Move2DTimer)]
        public class MoveTimer : ATimer<Move2DComponent>
        {
            protected override void Run(Move2DComponent self)
            {
                try
                {
                    self.MoveForward(true);
                }
                catch (Exception e)
                {
                    Log.Error($"move 2d timer error: {self.Id}\n{e}");
                }
            }
        }

        [EntitySystem]
        private static void Destroy(this Move2DComponent self)
        {
            self.MoveFinish(false);
        }

        [EntitySystem]
        private static void Awake(this Move2DComponent self)
        {
            self.StartTime = 0;
            self.StartPos = float2.zero;
            self.NeedTime = 0;
            self.MoveTimer = 0;
            self.tcs = null;
            self.Targets.Clear();
            self.Speed = 0;
            self.N = 0;
            self.TurnTime = 0;
            self.From = quaternion.identity;
            self.To = quaternion.identity;
        }

        public static bool IsArrived(this Move2DComponent self)
        {
            return self.Targets.Count == 0;
        }

        public static bool ChangeSpeed(this Move2DComponent self, float speed)
        {
            if (self.IsArrived())
            {
                return false;
            }

            if (speed < 0.0001f)
            {
                return false;
            }

            using ListComponent<float3> path = ListComponent<float3>.Create();

            self.MoveForward(false);
            self.GetRemainingPath(path);
            self.MoveToAsync(path, speed).Forget();
            return true;
        }

        public static void GetRemainingPath(this Move2DComponent self, List<float3> results)
        {
            Unit unit = self.GetParent<Unit>();
            results.Add(unit.Position);
            for (int i = self.N; i < self.Targets.Count; ++i)
            {
                results.Add(self.Targets[i].ToModePosition());
            }
        }

        public static async UniTask<bool> MoveToAsync(this Move2DComponent self, List<float3> target, float speed, int turnTime = 100)
        {
            self.Stop(false);

            foreach (float3 value in target)
            {
                self.Targets.Add(value.ToPlanar());
            }

            self.TurnTime = turnTime;
            self.Speed = speed;
            self.tcs = AutoResetUniTaskCompletionSource<bool>.Create();

            EventSystem.Instance.Publish(self.Scene(), new MoveStart() { Unit = self.GetParent<Unit>() });

            self.StartMove();

            bool moveRet = await self.tcs.Task;

            if (moveRet)
            {
                EventSystem.Instance.Publish(self.Scene(), new MoveStop() { Unit = self.GetParent<Unit>() });
            }

            return moveRet;
        }

        private static void MoveForward(this Move2DComponent self, bool ret)
        {
            Unit unit = self.GetParent<Unit>();

            long timeNow = TimeInfo.Instance.ClientNow();
            long moveTime = timeNow - self.StartTime;

            while (true)
            {
                if (moveTime <= 0)
                {
                    return;
                }

                if (moveTime >= self.NeedTime)
                {
                    unit.Position = self.NextTarget.ToModePosition();
                    if (self.TurnTime > 0)
                    {
                        unit.Rotation = self.To;
                    }
                }
                else
                {
                    float amount = moveTime * 1f / self.NeedTime;
                    if (amount > 0)
                    {
                        float2 newPos = math.lerp(self.StartPos, self.NextTarget, amount);
                        unit.Position = newPos.ToModePosition();
                    }

                    if (self.TurnTime > 0)
                    {
                        amount = moveTime * 1f / self.TurnTime;
                        if (amount > 1f)
                        {
                            amount = 1f;
                        }

                        unit.Rotation = math.slerp(self.From, self.To, amount);
                    }
                }

                moveTime -= self.NeedTime;

                if (moveTime < 0)
                {
                    return;
                }

                if (self.N >= self.Targets.Count - 1)
                {
                    unit.Position = self.NextTarget.ToModePosition();
                    unit.Rotation = self.To;

                    self.MoveFinish(ret);
                    return;
                }

                self.SetNextTarget();
            }
        }

        private static void StartMove(this Move2DComponent self)
        {
            self.BeginTime = TimeInfo.Instance.ClientNow();
            self.StartTime = self.BeginTime;
            self.SetNextTarget();

            self.MoveTimer = self.Root().GetComponent<TimerComponent>().NewFrameTimer(TimerInvokeType.Move2DTimer, self);
        }

        private static void SetNextTarget(this Move2DComponent self)
        {
            Unit unit = self.GetParent<Unit>();

            ++self.N;

            float2 faceV = self.GetFaceV();
            float distance = math.length(faceV);

            self.StartPos = unit.Position.ToPlanar();
            self.StartTime += self.NeedTime;
            self.NeedTime = (long)(distance / self.Speed * 1000);

            if (self.TurnTime > 0)
            {
                if (math.lengthsq(faceV) < 0.0001f)
                {
                    return;
                }

                self.From = unit.Rotation;
                self.To = faceV.ToPlanarRotation();
                return;
            }

            if (self.TurnTime == 0 && (Math.Abs(faceV.x) > 0.01f || Math.Abs(faceV.y) > 0.01f))
            {
                self.To = faceV.ToPlanarRotation();
                unit.Rotation = self.To;
            }
        }

        private static float2 GetFaceV(this Move2DComponent self)
        {
            return self.NextTarget - self.PreTarget;
        }

        public static bool FlashTo(this Move2DComponent self, float3 target)
        {
            Unit unit = self.GetParent<Unit>();
            unit.Position = new float2(target.x, target.y).ToModePosition();
            return true;
        }

        public static void Stop(this Move2DComponent self, bool ret)
        {
            if (self.Targets.Count > 0)
            {
                self.MoveForward(ret);
            }

            self.MoveFinish(ret);
        }

        private static void MoveFinish(this Move2DComponent self, bool ret)
        {
            if (self.StartTime == 0)
            {
                return;
            }

            self.StartTime = 0;
            self.StartPos = float2.zero;
            self.BeginTime = 0;
            self.NeedTime = 0;
            self.Targets.Clear();
            self.Speed = 0;
            self.N = 0;
            self.TurnTime = 0;
            self.From = quaternion.identity;
            self.To = quaternion.identity;
            self.Root().GetComponent<TimerComponent>()?.Remove(ref self.MoveTimer);

            if (self.tcs != null)
            {
                AutoResetUniTaskCompletionSource<bool> tcs = self.tcs;
                self.tcs = null;
                tcs.TrySetResult(ret);
            }
        }
    }
}

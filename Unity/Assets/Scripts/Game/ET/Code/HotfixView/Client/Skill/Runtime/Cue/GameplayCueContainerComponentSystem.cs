namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayCueContainerComponent))]
    [FriendOf(typeof(GameplayCueContainerComponent))]
    [FriendOf(typeof(GameplayCueSpec))]
    public static partial class GameplayCueContainerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this GameplayCueContainerComponent self)
        {
            self.ActiveCues.Clear();
            self.PendingRemove.Clear();
            self.IsUpdating = false;
        }

        [EntitySystem]
        private static void Update(this GameplayCueContainerComponent self)
        {
            self.Tick(UnityEngine.Time.deltaTime);
        }

        [EntitySystem]
        private static void Destroy(this GameplayCueContainerComponent self)
        {
            self.Clear();
        }

        public static void AddCue(this GameplayCueContainerComponent self, GameplayCueSpec cue)
        {
            if (cue == null || self.ActiveCues.Contains(cue))
            {
                return;
            }

            self.ActiveCues.Add(cue);
        }

        public static bool RemoveCue(this GameplayCueContainerComponent self, GameplayCueSpec cue)
        {
            if (cue == null || !self.ActiveCues.Contains(cue))
            {
                return false;
            }

            if (self.IsUpdating)
            {
                if (!self.PendingRemove.Contains(cue))
                {
                    self.PendingRemove.Add(cue);
                }
            }
            else
            {
                self.RemoveCueInternal(cue);
            }

            return true;
        }

        public static void Tick(this GameplayCueContainerComponent self, float deltaTime)
        {
            _ = deltaTime;
            self.IsUpdating = true;

            for (int i = 0; i < self.ActiveCues.Count; i++)
            {
                GameplayCueSpec cue = self.ActiveCues[i].As();
                if (cue == null)
                {
                    continue;
                }

                if (!cue.IsRunning && !self.PendingRemove.Contains(cue))
                {
                    self.PendingRemove.Add(cue);
                }
            }

            self.IsUpdating = false;
            self.ActiveCues.RemoveAll(cueRef => cueRef.As() == null);
            self.PendingRemove.RemoveAll(cueRef => cueRef.As() == null);

            while (self.PendingRemove.Count > 0)
            {
                GameplayCueSpec cue = self.PendingRemove[self.PendingRemove.Count - 1].As();
                self.RemoveCueInternal(cue);
            }
        }

        public static void Clear(this GameplayCueContainerComponent self)
        {
            while (self.ActiveCues.Count > 0)
            {
                GameplayCueSpec cue = self.ActiveCues[self.ActiveCues.Count - 1].As();
                self.RemoveCueInternal(cue);
            }

            self.ActiveCues.Clear();
            self.PendingRemove.Clear();
        }

        private static void RemoveCueInternal(this GameplayCueContainerComponent self, GameplayCueSpec cue)
        {
            if (cue == null)
            {
                self.ActiveCues.RemoveAll(cueRef => cueRef.As() == null);
                self.PendingRemove.RemoveAll(cueRef => cueRef.As() == null);
                return;
            }

            if (cue.IsRunning)
            {
                self.StopCue(cue, true);
            }

            self.ActiveCues.Remove(cue);
            self.PendingRemove.Remove(cue);
            if (!cue.IsDisposed)
            {
                cue.Dispose();
            }
        }

        private static void StopCue(this GameplayCueContainerComponent self, GameplayCueSpec cueSpec, bool cancelled)
        {
            if (cueSpec == null || string.IsNullOrEmpty(cueSpec.HandName))
            {
                return;
            }

            ACueHandler handler = CueDispatcherComponent.Instance.Get(cueSpec.HandName);
            if (handler == null)
            {
                Log.Error($"CueHandler not found: {cueSpec.HandName}");
                return;
            }

            handler.Spec = cueSpec;
            handler.NodeData = cueSpec.GetCueNodeData();
            cueSpec.IsCancelled = cancelled;
            cueSpec.IsRunning = false;
            handler.StopCue();
        }
    }
}

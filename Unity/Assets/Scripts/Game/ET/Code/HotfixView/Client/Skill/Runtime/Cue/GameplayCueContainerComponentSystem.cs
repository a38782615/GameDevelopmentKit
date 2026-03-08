namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayCueContainerComponent))]
    [FriendOf(typeof(GameplayCueContainerComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayCueSpec))]

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

        // ============ Cue管理 ============

        public static void AddCue(this GameplayCueContainerComponent self, GameplayCueSpec cue)
        {
            if (cue == null || self.ActiveCues.Contains(cue)) return;
            self.ActiveCues.Add(cue);
        }

        public static bool RemoveCue(this GameplayCueContainerComponent self, GameplayCueSpec cue)
        {
            if (cue == null || !self.ActiveCues.Contains(cue)) return false;

            if (self.IsUpdating)
            {
                if (!self.PendingRemove.Contains(cue))
                    self.PendingRemove.Add(cue);
            }
            else
            {
                self.RemoveCueInternal(cue);
            }
            return true;
        }

        private static void RemoveCueInternal(this GameplayCueContainerComponent self, GameplayCueSpec cue)
        {
            if (cue.IsRunning)
                self.StopCue(cue, true);
            self.ActiveCues.Remove(cue);
            if (!cue.IsDisposed)
                cue.Dispose();
        }

        // ============ 更新 ============

        public static void Tick(this GameplayCueContainerComponent self, float deltaTime)
        {
            self.IsUpdating = true;

            for (int i = 0; i < self.ActiveCues.Count; i++)
            {
                var cue = self.ActiveCues[i].As();
                if (cue == null) continue;

                self.TickCue(cue);

                if (!cue.IsRunning && !self.PendingRemove.Contains(cue))
                    self.PendingRemove.Add(cue);
            }

            self.IsUpdating = false;

            if (self.PendingRemove.Count > 0)
            {
                foreach (var cue in self.PendingRemove)
                    self.RemoveCueInternal(cue);
                self.PendingRemove.Clear();
            }
        }

        public static void Clear(this GameplayCueContainerComponent self)
        {
            for (int i = self.ActiveCues.Count - 1; i >= 0; i--)
                self.RemoveCueInternal(self.ActiveCues[i]);
            self.ActiveCues.Clear();
            self.PendingRemove.Clear();
        }

        private static void TickCue(this GameplayCueContainerComponent self, GameplayCueSpec cueSpec)
        {
            if (!cueSpec.IsRunning || cueSpec.ActiveCue == null)
            {
                return;
            }

            if (cueSpec.ActiveCue.IsExpired)
            {
                cueSpec.IsRunning = false;
                cueSpec.ActiveCue = null;
            }
        }

        private static void StopCue(this GameplayCueContainerComponent self, GameplayCueSpec cueSpec, bool cancelled)
        {
            if (cueSpec == null || string.IsNullOrEmpty(cueSpec.HandName))
            {
                return;
            }

            var handler = CueDispatcherComponent.Instance.Get(cueSpec.HandName);
            if (handler == null)
            {
                Log.Error($"CueHandler not found: {cueSpec.HandName}");
                return;
            }

            handler.Spec = cueSpec;
            handler.NodeData = cueSpec.CueNodeData;
            cueSpec.IsCancelled = cancelled;
            cueSpec.IsRunning = false;
            handler.StopCue();
        }
    }
}

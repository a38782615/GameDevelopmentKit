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
                cue.CancelCue();
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

                cue.TickCue(deltaTime);

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
    }
}

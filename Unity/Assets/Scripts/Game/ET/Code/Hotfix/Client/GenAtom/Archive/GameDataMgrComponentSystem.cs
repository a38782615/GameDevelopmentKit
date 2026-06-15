using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameDataMgrComponent))]
    [FriendOf(typeof(GameDataMgrComponent))]
    public static partial class GameDataMgrComponentSystem
    {
        [EntitySystem]
        private static void Awake(this GameDataMgrComponent self)
        {
            self.EnsureDataComponents();
        }

        [EntitySystem]
        private static void Destroy(this GameDataMgrComponent self)
        {
            self.PlayerDataComponent = null;
            self.TaskDataComponent = null;
        }

        public static async UniTask LoadAllData(this GameDataMgrComponent self)
        {
            ArchiveComponent archiveComponent = self.GetArchiveComponent();
            if (archiveComponent == null)
            {
                return;
            }

            self.EnsureDataComponents();
            await self.GetPlayerDataComponent().LoadPlayerData(archiveComponent);
            await self.GetTaskDataComponent().LoadTaskData(archiveComponent);
        }

        public static async UniTask SaveAllData(this GameDataMgrComponent self)
        {
            ArchiveComponent archiveComponent = self.GetArchiveComponent();
            if (archiveComponent == null)
            {
                return;
            }

            self.EnsureDataComponents();
            await self.GetPlayerDataComponent().SavePlayerData(archiveComponent);
            await self.GetTaskDataComponent().SaveTaskData(archiveComponent);
        }

        public static PlayerDataComponent GetPlayerDataComponent(this GameDataMgrComponent self)
        {
            PlayerDataComponent playerDataComponent = self.PlayerDataComponent;
            if (playerDataComponent == null)
            {
                playerDataComponent = self.GetOrAddComponent<PlayerDataComponent>();
                self.PlayerDataComponent = playerDataComponent;
            }

            return playerDataComponent;
        }

        public static TaskDataComponent GetTaskDataComponent(this GameDataMgrComponent self)
        {
            TaskDataComponent taskDataComponent = self.TaskDataComponent;
            if (taskDataComponent == null)
            {
                taskDataComponent = self.GetOrAddComponent<TaskDataComponent>();
                self.TaskDataComponent = taskDataComponent;
            }

            return taskDataComponent;
        }

        public static async UniTask SavePlayerData(this GameDataMgrComponent self)
        {
            ArchiveComponent archiveComponent = self.GetArchiveComponent();
            if (archiveComponent == null)
            {
                return;
            }

            await self.GetPlayerDataComponent().SavePlayerData(archiveComponent);
        }

        public static async UniTask SaveTaskData(this GameDataMgrComponent self)
        {
            ArchiveComponent archiveComponent = self.GetArchiveComponent();
            if (archiveComponent == null)
            {
                return;
            }

            await self.GetTaskDataComponent().SaveTaskData(archiveComponent);
        }

        private static void EnsureDataComponents(this GameDataMgrComponent self)
        {
            self.GetPlayerDataComponent();
            self.GetTaskDataComponent();
        }

        private static ArchiveComponent GetArchiveComponent(this GameDataMgrComponent self)
        {
            ArchiveMgrComponent archiveMgrComponent = self.Root().GetComponent<ArchiveMgrComponent>();
            if (archiveMgrComponent == null)
            {
                Log.Error("GameDataMgrComponent load failed: ArchiveMgrComponent is missing.");
                return null;
            }

            ArchiveComponent archiveComponent = archiveMgrComponent.GetCurrentArchive();
            if (archiveComponent == null)
            {
                Log.Error("GameDataMgrComponent load failed: current archive is null.");
                return null;
            }

            return archiveComponent;
        }
    }
}

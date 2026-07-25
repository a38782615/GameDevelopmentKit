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
            self.PlayerSkillDataComponent = null;
            self.TaskDataComponent = null;
            self.InventoryDataComponent = null;
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
            await self.GetPlayerSkillDataComponent().LoadPlayerSkillData(archiveComponent);
            await self.GetTaskDataComponent().LoadTaskData(archiveComponent);
            await self.GetInventoryDataComponent().LoadInventoryData(archiveComponent);
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
            await self.GetPlayerSkillDataComponent().SavePlayerSkillData(archiveComponent);
            await self.GetTaskDataComponent().SaveTaskData(archiveComponent);
            await self.GetInventoryDataComponent().SaveInventoryData();
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

        public static PlayerSkillDataComponent GetPlayerSkillDataComponent(this GameDataMgrComponent self)
        {
            PlayerSkillDataComponent playerSkillDataComponent = self.PlayerSkillDataComponent;
            if (playerSkillDataComponent == null)
            {
                playerSkillDataComponent = self.GetOrAddComponent<PlayerSkillDataComponent>();
                self.PlayerSkillDataComponent = playerSkillDataComponent;
            }

            return playerSkillDataComponent;
        }

        public static InventoryDataComponent GetInventoryDataComponent(this GameDataMgrComponent self)
        {
            InventoryDataComponent inventoryDataComponent = self.InventoryDataComponent;
            if (inventoryDataComponent == null)
            {
                inventoryDataComponent = self.GetOrAddComponent<InventoryDataComponent>();
                self.InventoryDataComponent = inventoryDataComponent;
            }

            return inventoryDataComponent;
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

        public static async UniTask SavePlayerSkillData(this GameDataMgrComponent self)
        {
            ArchiveComponent archiveComponent = self.GetArchiveComponent();
            if (archiveComponent == null)
            {
                return;
            }

            await self.GetPlayerSkillDataComponent().SavePlayerSkillData(archiveComponent);
        }

        private static void EnsureDataComponents(this GameDataMgrComponent self)
        {
            self.GetPlayerDataComponent();
            self.GetPlayerSkillDataComponent();
            self.GetTaskDataComponent();
            self.GetInventoryDataComponent();
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

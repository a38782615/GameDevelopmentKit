using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(TaskDataComponent))]
    [FriendOf(typeof(TaskDataComponent))]
    public static partial class TaskDataComponentSystem
    {
        private const string TaskDataDocumentId = nameof(TaskData);

        [EntitySystem]
        private static void Awake(this TaskDataComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this TaskDataComponent self)
        {
            self.TaskData = null;
        }

        public static async UniTask LoadTaskData(this TaskDataComponent self, ArchiveComponent archiveComponent)
        {
            TaskData taskData = await archiveComponent.QueryById<TaskData>(TaskDataDocumentId);
            if (taskData == null)
            {
                taskData = CreateDefaultTaskData();
                await archiveComponent.Save(TaskDataDocumentId, taskData);
            }

            EnsureTaskData(taskData);
            self.TaskData = taskData;
        }

        public static async UniTask SaveTaskData(this TaskDataComponent self, ArchiveComponent archiveComponent)
        {
            if (self.TaskData == null)
            {
                return;
            }

            EnsureTaskData(self.TaskData);
            await archiveComponent.Save(TaskDataDocumentId, self.TaskData);
        }

        public static int GetTaskState(this TaskDataComponent self, int taskId)
        {
            TaskData taskData = self.GetOrCreateTaskData();
            return taskData.TaskStates.TryGetValue(taskId, out int state) ? state : 0;
        }

        public static void SetTaskState(this TaskDataComponent self, int taskId, int state)
        {
            TaskData taskData = self.GetOrCreateTaskData();
            taskData.TaskStates[taskId] = state;
        }

        public static long GetTaskProgress(this TaskDataComponent self, int taskId)
        {
            TaskData taskData = self.GetOrCreateTaskData();
            return taskData.TaskProgresses.TryGetValue(taskId, out long progress) ? progress : 0;
        }

        public static void SetTaskProgress(this TaskDataComponent self, int taskId, long progress)
        {
            TaskData taskData = self.GetOrCreateTaskData();
            taskData.TaskProgresses[taskId] = progress;
        }

        public static void AddTaskProgress(this TaskDataComponent self, int taskId, long delta)
        {
            TaskData taskData = self.GetOrCreateTaskData();
            taskData.TaskProgresses[taskId] = self.GetTaskProgress(taskId) + delta;
        }

        public static void RemoveTask(this TaskDataComponent self, int taskId)
        {
            TaskData taskData = self.GetOrCreateTaskData();
            taskData.TaskStates.Remove(taskId);
            taskData.TaskProgresses.Remove(taskId);
        }

        private static TaskData GetOrCreateTaskData(this TaskDataComponent self)
        {
            if (self.TaskData == null)
            {
                self.TaskData = CreateDefaultTaskData();
            }

            EnsureTaskData(self.TaskData);
            return self.TaskData;
        }

        private static TaskData CreateDefaultTaskData()
        {
            return new TaskData();
        }

        private static void EnsureTaskData(TaskData taskData)
        {
            taskData.TaskStates ??= new();
            taskData.TaskProgresses ??= new();
        }
    }
}

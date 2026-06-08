using Cysharp.Threading.Tasks;

namespace ET.Client
{
	[MessageHandler(SceneType.GenAtom)]
	public class M2C_CreateUnitsHandler: MessageHandler<Scene, M2C_CreateUnits>
	{
		protected override async UniTask Run(Scene root, M2C_CreateUnits message)
		{
			Scene currentScene = root.CurrentScene();
			UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
			using ListComponent<UniTask> afterCreateTasks = ListComponent<UniTask>.Create();
			
			foreach (UnitInfo unitInfo in message.Units)
			{
				if (unitComponent.Get(unitInfo.UnitId) != null)
				{
					continue;
				}

				Unit unit = UnitFactory.Create(currentScene, unitInfo);
				afterCreateTasks.Add(EventSystem.Instance.PublishAsync(currentScene, new AfterUnitCreate() { Unit = unit }));
			}

			if (afterCreateTasks.Count > 0)
			{
				await UniTask.WhenAll(afterCreateTasks);
				currentScene.TriggerGameAIChecks();
			}
		}
	}
}

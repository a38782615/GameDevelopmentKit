using Cysharp.Threading.Tasks;

namespace ET.Client
{
	[Event(SceneType.GenAtom)]
	public class LoginFinish_RemoveLoginUI : AEvent<Scene, LoginFinish>
	{
		protected override async UniTask Run(Scene scene, LoginFinish args)
		{
			scene.GetComponent<UIComponent>().RemoveComponent<UIFormLoginComponent>();
			await EventSystem.Instance.PublishAsync(scene, new GoMap2d());
		}
	}
}

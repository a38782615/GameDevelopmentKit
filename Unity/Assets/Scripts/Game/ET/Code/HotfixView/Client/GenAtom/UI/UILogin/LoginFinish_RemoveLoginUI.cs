using Cysharp.Threading.Tasks;

namespace ET.Client
{
	[Event(SceneType.GenAtom)]
	public class LoginFinish_RemoveLoginUI : AEvent<Scene, LoginFinish>
	{
		protected override async UniTask Run(Scene scene, LoginFinish args)
		{
			scene.GetComponent<UIComponent>().RemoveComponent<UIFormLoginComponent>();
			// 等待 UIManager 在后续帧的 Update 中完成 Close/Recycle，避免 UILogin 运行时节点被切场景过程带进新场景。
			await UniTask.DelayFrame(2);
			await EventSystem.Instance.PublishAsync(scene, new GoScene()
			{
				SceneId = Tables.Instance.DTGameConfig.SceneMain,
				UI = UGFUIFormId.UIMain
			});
		}
	}
}

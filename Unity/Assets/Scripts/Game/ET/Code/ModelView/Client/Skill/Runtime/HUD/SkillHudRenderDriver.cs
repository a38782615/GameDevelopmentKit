using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public class SkillHudRenderDriver : MonoBehaviour
    {
        private void LateUpdate()
        {
            SkillHudManager manager = SkillHudManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.Tick(UnityEngine.Time.deltaTime);
        }
    }
}

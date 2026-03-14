using CodeBind;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [EnableClass]
    [MonoCodeBind]
    public partial class MonoUIFormSkill : AETMonoUGFUIForm
    {
        private Button m_CloseButton;
        private CommonLoopScrollRect m_SkillLoopScrollRect;

        public Button CloseButton => this.FindComponent(ref m_CloseButton, "Panel/CloseButton");

        public CommonLoopScrollRect SkillLoopScrollRect => this.FindComponent(ref m_SkillLoopScrollRect, "Panel/SkillLoopScrollRect");

        private T FindComponent<T>(ref T cache, string path) where T : Component
        {
            if (cache != null)
            {
                return cache;
            }

            Transform child = this.CachedTransform.Find(path);
            if (child == null)
            {
                return null;
            }

            cache = child.GetComponent<T>();
            return cache;
        }
    }
}

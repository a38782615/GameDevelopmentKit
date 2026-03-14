using Game;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [EnableClass]
    public class MonoUISkillItem : MonoBehaviour
    {
        private const string SkillIconCollectionPath = "Assets/Res/UI/UIAtlas/SkillIcon.asset";
        private Button m_CastButton;
        private Image m_IconImage;
        private Text m_NameText;
        private Text m_StateText;
        private string m_IconPath;

        public Button CastButton => m_CastButton ??= this.GetComponent<Button>();

        public Image IconImage => m_IconImage ??= this.transform.Find("IconImage")?.GetComponent<Image>();

        public Text NameText => m_NameText ??= this.transform.Find("NameText")?.GetComponent<Text>();

        public Text StateText => m_StateText ??= this.transform.Find("StateText")?.GetComponent<Text>();

        public void SetIcon(string iconPath)
        {
            Image iconImage = this.IconImage;
            if (iconImage == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(iconPath))
            {
                iconImage.enabled = false;
                m_IconPath = null;
                return;
            }

            iconImage.enabled = true;
            if (m_IconPath == iconPath)
            {
                return;
            }

            m_IconPath = iconPath;
            string spritePath = GetSkillIconSpritePath(iconPath);
            iconImage.SetSprite(SkillIconCollectionPath, spritePath);
        }

        private static string GetSkillIconSpritePath(string iconPath)
        {
            string normalizedPath = iconPath.Replace('\\', '/');
            if (normalizedPath.StartsWith("Assets/"))
            {
                return normalizedPath;
            }

            string iconName = Path.GetFileNameWithoutExtension(normalizedPath);
            return $"Assets/Res/UI/UISprite/SkillIcon/{iconName}.png";
        }
    }
}

using System;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GFEntityHeadItem))]
    [EntitySystemOf(typeof(GFEntityHeadItem))]
    public static partial class GFEntityHeadItemSystem
    {
        public const string PreHead = "Main/head/";
        [EntitySystem]
        private static void Awake(this GFEntityHeadItem self)
        {
           
        }

        [EntitySystem]
        private static void Destroy(this GFEntityHeadItem self)
        {
            
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this GFEntityHeadItem self)
        {
           
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this GFEntityHeadItem self, bool isShutdown)
        {
           
        }

        public static async UniTask SetHeadIconAsync(this GFEntityHeadItem self, Unit unit)
        {
            SpriteRenderer headRenderer = self?.View?.HeadSpriteRenderer;
            if (headRenderer == null || unit == null)
            {
                return;
            }

            string headIcon = unit.Config()?.HeadIcon;
            if (string.IsNullOrEmpty(headIcon))
            {
                headRenderer.sprite = null;
                return;
            }

            await headRenderer.SetSpriteAsync(AssetUtility.GetUISpriteAsset(PreHead+headIcon));
        }
 
    }
}

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Extension;

namespace Game
{
    public static partial class AssetSetExtension
    {
        public static UniTask SetSpriteAsync(this Image image, string spritePath)
        {
            AutoResetUniTaskCompletionSource tcs = AutoResetUniTaskCompletionSource.Create();
            GameEntry.AssetSet.SetByResource(WaitableImageSet.Create(image, spritePath, tcs));
            return tcs.Task;
        }

        public static UniTask SetSpriteAsync(this UXImage uxImage, string spritePath)
        {
            AutoResetUniTaskCompletionSource tcs = AutoResetUniTaskCompletionSource.Create();
            GameEntry.AssetSet.SetByResource(WaitableUXImageSet.Create(uxImage, spritePath, tcs));
            return tcs.Task;
        }

        public static UniTask SetSpriteAsync(this SpriteRenderer spriteRenderer, string spritePath)
        {
            AutoResetUniTaskCompletionSource tcs = AutoResetUniTaskCompletionSource.Create();
            GameEntry.AssetSet.SetByResource(WaitableSpriteRendererSet.Create(spriteRenderer, spritePath, tcs));
            return tcs.Task;
        }

        public static void SetSprite(this Image image, string spritePath)
        {
            GameEntry.AssetSet.SetByResource(ImageSet.Create(image, spritePath));
        }

        public static void SetSprite(this UXImage uxImage, string spritePath)
        {
            GameEntry.AssetSet.SetByResource(UXImageSet.Create(uxImage, spritePath));
        }

        public static void SetSprite(this SpriteRenderer spriteRenderer, string spritePath)
        {
            GameEntry.AssetSet.SetByResource(SpriteRendererSet.Create(spriteRenderer, spritePath));
        }

        public static UniTask SetTextureByResourceAsync(this RawImage rawImage, string texturePath)
        {
            AutoResetUniTaskCompletionSource tcs = AutoResetUniTaskCompletionSource.Create();
            GameEntry.AssetSet.SetByResource(WaitableRawImageSet.Create(rawImage, texturePath, false, tcs));
            return tcs.Task;
        }

        public static void SetTextureByResource(this RawImage rawImage, string texturePath)
        {
            GameEntry.AssetSet.SetByResource(RawImageSet.Create(rawImage, texturePath, false));
        }

        public static UniTask SetTextureByFileSystemAsync(this RawImage rawImage, string texturePath)
        {
            AutoResetUniTaskCompletionSource tcs = AutoResetUniTaskCompletionSource.Create();
            GameEntry.AssetSet.SetByFileSystem(WaitableRawImageSet.Create(rawImage, texturePath, false, tcs));
            return tcs.Task;
        }

        public static void SetTextureByFileSystem(this RawImage rawImage, string texturePath)
        {
            GameEntry.AssetSet.SetByFileSystem(RawImageSet.Create(rawImage, texturePath, false));
        }

        public static UniTask SetTextureByWebRequestAsync(this RawImage rawImage, string file)
        {
            AutoResetUniTaskCompletionSource tcs = AutoResetUniTaskCompletionSource.Create();
            GameEntry.AssetSet.SetByWebRequest(WaitableRawImageSet.Create(rawImage, file, true, tcs));
            return tcs.Task;
        }

        public static void SetTextureByWebRequest(this RawImage rawImage, string file)
        {
            GameEntry.AssetSet.SetByWebRequest(RawImageSet.Create(rawImage, file, true));
        }
    }
}

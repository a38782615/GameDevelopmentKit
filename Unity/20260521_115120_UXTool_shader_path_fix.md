# 20260521 UXTool shader path fix

- Root cause: `Assets/Res/UI/UXTool/UXToolAssetCollection.asset` had no collection path configured, so `AssetCollection.GetAsset` could not resolve `Assets/Res/UI/UXTool/GUI/Shader/UXImage.shader`.
- Fix 1: configured `UXToolAssetCollection` to collect `Assets/Res/UI/UXTool/GUI` and refreshed AssetCollection in Unity.
- Fix 2: updated `Assets/Scripts/Library/UXTool/Runtime/Common/UnityExtension/ResourceManager.cs` so editor-time loads bypass stale `AssetCollection` and read directly from `AssetDatabase` when not playing or when `ResourceComponent` is unavailable.
- Verification: used AIBridge `focus`, `editor stop`, `menu_item Game Framework/Refresh AssetCollection`, and `compile unity --raw --timeout 120000`; compile returned success with 0 errors and 0 warnings.
- Note: `get_logs --raw` still shows the original missing-shader stack trace as historical console content from before the fix.

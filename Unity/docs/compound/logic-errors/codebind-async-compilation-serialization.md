# CodeBind 异步编译后自动序列化问题

## 问题症状

- 点击 "Generate Bind Code" 后只生成了 `.Bind.cs` 文件
- 序列化数据没有自动填充到 Inspector 中
- 需要手动再点击 "Generate Serialization" 按钮才能完成绑定

## 根因分析

1. **Unity 代码编译是异步过程**：生成 `.Bind.cs` 文件后，Unity 会触发重新编译
2. **编译完成前字段不存在**：新生成的字段（如 `m_XXXButton`）在运行时还不存在
3. **直接调用失败**：`TrySetSerialization()` 使用反射查找字段，编译前会失败
4. **回调被清除**：`CompilationPipeline.compilationFinished` 回调在域重载后会丢失

### 失败的方案

```csharp
// 方案1：直接调用 - 失败，字段还不存在
codeBinder.TryGenerateBindCode();
codeBinder.TrySetSerialization(); // 反射找不到字段

// 方案2：编译完成回调 - 失败，域重载后回调丢失
CompilationPipeline.compilationFinished += OnCompilationFinished;
```

## 解决方案

使用 `SessionState` + `DidReloadScripts` 组合：

```csharp
internal sealed class MonoCodeBindPropertyProcessor<T> : OdinPropertyProcessor<T, MonoCodeBindAttribute>
{
    private const string PendingSerializationKey = "CodeBind_PendingSerializationGO";

    private void TryGenerateBindCode()
    {
        foreach (T t in ValueEntry.Values)
        {
            // ... 生成代码 ...
            codeBinder.TryGenerateBindCode();
            
            // 标记需要在编译后序列化
            SessionState.SetBool(PendingSerializationKey, true);
        }
    }
}

internal static class CodeBindAutoSerializer
{
    private const string PendingSerializationKey = "CodeBind_PendingSerializationGO";

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (!SessionState.GetBool(PendingSerializationKey, false))
            return;

        SessionState.SetBool(PendingSerializationKey, false);
        EditorApplication.delayCall += TryAutoSerialization;
    }

    private static void TryAutoSerialization()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) return;

        MonoBehaviour[] monos = go.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mono in monos)
        {
            if (mono == null) continue;
            
            Type monoType = mono.GetType();
            object[] attrs = monoType.GetCustomAttributes(typeof(MonoCodeBindAttribute), true);
            if (attrs.Length == 0) continue;

            MonoCodeBindAttribute attribute = (MonoCodeBindAttribute)attrs[0];
            try
            {
                MonoScript script = MonoScript.FromMonoBehaviour(mono);
                MonoCodeBinder codeBinder = new MonoCodeBinder(script, mono.transform, attribute.SeparatorChar);
                codeBinder.TrySetSerialization();
                Debug.Log($"Auto serialization completed for {mono.GetType().Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Auto serialization failed: {e.Message}");
            }
        }
    }
}
```

### 关键点

| 技术 | 作用 |
|------|------|
| `SessionState` | 在编辑器会话期间保持状态，编译后不丢失 |
| `[DidReloadScripts]` | 编译完成、域重载后的可靠回调点 |
| `Selection.activeGameObject` | 获取用户当前选中的对象 |
| `EditorApplication.delayCall` | 确保在编辑器完全就绪后执行 |

## 涉及文件

- `Assets/Plugins/me.xw.codebind/Editor/MonoCodeBindPropertyProcessor.cs`

## 预防策略

1. **考虑异步编译**：Unity 编辑器中涉及代码生成后需要使用生成结果的场景，都要考虑异步编译问题
2. **使用 SessionState**：跨编译传递状态的首选方案
3. **使用 DidReloadScripts**：编译完成后最可靠的回调点
4. **Selection 获取对象**：简单有效的获取当前操作对象的方式

## 标签

`Unity` `CodeBind` `异步编译` `DidReloadScripts` `SessionState` `代码生成` `反射`

## 相关链接

- [Unity SessionState 文档](https://docs.unity3d.com/ScriptReference/SessionState.html)
- [DidReloadScripts 文档](https://docs.unity3d.com/ScriptReference/Callbacks.DidReloadScripts.html)

# Windows 存档 DataPath

- Windows / Windows Editor 下，Archive 传入相对存档名时改为解析到 Unity `dataPath` 下。
- `GameConst` 增加运行时 `DataPath` 字段，由 `CodeLoader` 在 Model 程序集加载后反射写入 `Application.dataPath`，避免 Hotfix 直接引用 UnityEngine。
- 保留绝对存档路径原行为，不影响非 Windows 平台路径解析。
- 已通过 AIBridge 执行 Unity 编译，结果无错误、无警告。

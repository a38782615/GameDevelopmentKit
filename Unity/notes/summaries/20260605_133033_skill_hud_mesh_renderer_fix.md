# 血条改为 MeshRenderer

- 结论：运行时 `Graphics.DrawMesh` 路径始终不可见，但验证预制体里的 `TextBillboard` `MeshRenderer` 可见，说明 shader 本身正常，问题更集中在运行时绘制命令这层。
- 调整：
  - `SkillHudManager` 的血条改为在场景里生成真实 `MeshRenderer` quad，不再走 `Graphics.DrawMesh`。
  - 仍然保留自绘 quad，不使用 `HPBar`。
  - 背景条和前景条分别作为独立 quad，颜色通过 `MaterialPropertyBlock` 设置。
- 验证：执行 `AIBridgeCLI.exe focus --raw`、`editor stop --raw`、`compile unity --raw --timeout 120000`，编译通过，`errorCount=0`。

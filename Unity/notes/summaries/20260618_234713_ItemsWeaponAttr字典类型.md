# Items Weapon Attr 字典类型

## 修改内容

- 将 `Design/Excel/ET/Datas/Game/Items.xlsx` 中 `Weapon` 页签 `Attr` 列的 `##type` 行设置为 `map,int,long`。

## 验证

- 已通过读取 Excel 确认 `Weapon!J2` 为 `map,int,long`。
- 已通过 Unity 菜单 `Game/Tool/ExcelExporter` 导表。
- Unity Console 已出现 `Luban excel export success!` 与 `Export cost 1225 Milliseconds!`。
- 已检查相关中文单元格读取正常，未发现乱码。

## 备注

- 当前 `Design/Excel/ET/Datas/__tables__.xlsx` 未注册 Items/Weapon 相关表，因此导表成功但不会生成 Items/Weapon 对应 C# 产物。

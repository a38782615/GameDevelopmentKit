# Items Weapon Attr 分隔符类型

## 修改内容

- 将 `Design/Excel/ET/Datas/Game/Items.xlsx` 中 `Weapon` 页签 `Attr` 列 `##type` 从 `map,int,long` 修正为 `map,int,int#sep=,;`。
- 当前数据 `10061,10;10081,10` 会按 `,` 和 `;` 切分为 key/value 序列，匹配 `map<int, int>`。

## 验证

- 已读取 Excel 确认 `Weapon!J2` 为 `map,int,int#sep=,;`，`Weapon!J4` 为 `10061,10;10081,10`。
- 已通过 Unity 菜单 `Game/Tool/ExcelExporter` 导表。
- Unity Console 已出现 `Luban excel export success!` 与 `Export cost 1271 Milliseconds!`。
- 已检查相关中文单元格读取正常，未发现乱码。

## 备注

- `Design/Excel/ET/Datas/__tables__.xlsx` 当前未注册 Items/Weapon 相关表，因此本次不会产生 Items/Weapon 对应生成代码差异。

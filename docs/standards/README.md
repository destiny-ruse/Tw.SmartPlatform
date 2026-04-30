# 工程规范目录

本目录是项目工程规范的统一入口，用于存放业务无关的研发标准、协作约定、质量要求和治理资料。

## 使用方式

每篇规范应是可直接阅读的 Markdown 正文。新增或修改规范时，遵循 [规范撰写规范](processes/standard-authoring.md)，并在变更记录中说明版本和原因。

## 当前规范

- [规则正文](rules/)
- [流程](processes/)
- [决策记录](decisions/)
- [参考资料](references/)

## 同步检查

修改本目录后，应从仓库根目录运行：

```powershell
python tools\tw-memory\tw_memory.py generate --format brief
python tools\tw-memory\tw_memory.py check --format brief
```

当前仓库没有独立的标准索引脚本，规范资料不维护生成索引文件或生成目录。

## 治理入口

- [RFC 流程](processes/rfc-flow.md)
- [决策记录](decisions/)

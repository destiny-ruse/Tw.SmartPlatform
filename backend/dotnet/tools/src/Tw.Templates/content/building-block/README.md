# Tw building block 模板

该模板用于在 `backend/dotnet/BuildingBlocks` 中创建具有 capability 目录、运行时项目、测试项目和 package charter 的共享包骨架。

从仓库的 `backend/dotnet/BuildingBlocks` 目录执行：

```powershell
dotnet new tw-building-block `
  --name Tw.Example `
  --output . `
  --capability Example `
  --owner dotnet-framework `
  --responsibility "提供示例公共能力" `
  --inScope "示例能力的公共契约与默认实现" `
  --outOfScope "特定基础设施提供方实现" `
  --publicCapability Tw.Example
```

生成结果位于 `src/Example/Tw.Example` 和 `tests/Example/Tw.Example.Tests`。测试项目会引用运行时项目；生成项目继承 BuildingBlocks 根目录的中央包版本与仓库质量门禁。

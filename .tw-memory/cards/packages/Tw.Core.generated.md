# Package: Tw.Core

标识：Tw.Core / backend/dotnet/BuildingBlocks/src/Tw.Core / platform-team
职责：跨服务复用的基础原语与无框架依赖工具：值对象、命名对象、领域异常、 配置异常、类型查找、反射缓存、加密哈希、通用扩展方法与一次性资源工具。

适用范围：
- 基础值对象与命名对象原语
- 通用领域异常与配置异常
- 类型查找、反射缓存与类型扩展
- 加密、哈希与安全随机
- 通用集合、字符串、时间、数字等扩展方法

不适用范围：
- HTTP、中间件、过滤器、ASP.NET Core 集成
- 数据访问、ORM、仓储实现
- 具体业务领域模型

依赖边界：
- forbid: Microsoft.AspNetCore.*, Microsoft.EntityFrameworkCore*
- allow: 

稳定性：stable
兼容性：semver-minor 内向后兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Core

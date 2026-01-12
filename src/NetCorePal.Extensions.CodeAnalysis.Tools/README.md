# NetCorePal.Extensions.CodeAnalysis.Tools

基于 NetCorePal 代码分析框架的命令行工具，用于从 .NET 项目生成交互式架构可视化 HTML 文件。

## 功能特性

- **智能发现**：自动发现并分析解决方案或项目
- **依赖递归分析**：自动分析项目依赖关系，确保完整的架构视图
- **多类型图表**：生成架构总览图、处理流程图、聚合关系图等
- **交互式HTML**：提供完整的导航、图表切换和在线编辑功能
- **Mermaid Live集成**：一键跳转到 Mermaid Live Editor 进行在线编辑
- **.NET 10 单文件执行**：利用 .NET 10 的单文件运行能力，动态生成和执行分析代码

## 快速开始

### 安装

```bash
dotnet tool install -g NetCorePal.Extensions.CodeAnalysis.Tools
```

安装预览版：

```bash
dotnet tool install -g NetCorePal.Extensions.CodeAnalysis.Tools --prerelease  --source https://www.myget.org/F/netcorepal/api/v3/index.json
```

### 使用

```bash
# 进入项目目录
cd MyApp

# 自动发现并分析当前目录下的解决方案或项目
netcorepal-codeanalysis generate

# 指定解决方案文件
netcorepal-codeanalysis generate --solution MySolution.sln

# 指定项目文件
netcorepal-codeanalysis generate --project MyProject.csproj

# 自定义输出和标题
netcorepal-codeanalysis generate --output my-architecture.html --title "我的架构图"

# 启用详细输出
netcorepal-codeanalysis generate --verbose
```

### 自动发现行为

- 默认策略：当未提供 `--solution` 与 `--project` 时，工具会在“当前目录（顶层）”自动发现分析目标。
- 发现优先级：
	1) 优先使用 `.slnx`
	2) 其次使用 `.sln`
	3) 若无解决方案文件，则收集当前目录顶层的 `*.csproj`
- 非递归：不递归扫描子目录，仅检查当前目录的顶层文件。
- 运行时提示：选择 `.slnx/.sln` 时将明确打印“Using solution (.slnx/.sln): <文件名>”。

如需显式指定，推荐：

```bash
# 指定解决方案
netcorepal-codeanalysis generate --solution MySolution.slnx

# 或指定若干项目
netcorepal-codeanalysis generate --project A.csproj --project B.csproj
```

## 前提条件

- .NET 8.0 或更高版本（推荐 .NET 10.0 以获得最佳性能）
- 目标项目必须引用 `NetCorePal.Extensions.CodeAnalysis` 包
- 该包包含源生成器，在编译时自动生成代码分析元数据

## 工作原理

该工具采用基于 .NET 10 单文件执行能力的全新架构：

1. **项目发现**：自动发现目标解决方案或项目文件
2. **依赖分析**：递归分析项目引用，获取所有相关项目
3. **动态代码生成**：生成包含 `#:project` 指令的临时 C# 文件
4. **单文件执行**：使用 `dotnet run app.cs` 执行分析
5. **结果生成**：生成交互式 HTML 可视化文件
6. **自动清理**：删除临时文件

## 输出内容

生成的HTML文件包含：

- **统计信息**：各类型组件的数量统计和分布情况
- **架构总览图**：系统中所有类型及其关系的完整视图
- **处理流程图集合**：每个独立业务链路的流程图（如命令处理链路）
- **聚合关系图集合**：每个聚合根相关的关系图
- **交互式导航**：左侧树形菜单，支持图表类型切换
- **在线编辑功能**：每个图表右上角的"View in Mermaid Live"按钮

## 完整文档

详细的使用说明、命令行选项、集成方式和故障排除，请参阅：

- [中文文档](https://netcorepal.github.io/netcorepal-cloud-framework/zh/code-analysis/code-analysis-tools/)
- [English Documentation](https://netcorepal.github.io/netcorepal-cloud-framework/en/code-analysis/code-analysis-tools/)

## 相关包

- [`NetCorePal.Extensions.CodeAnalysis`](../NetCorePal.Extensions.CodeAnalysis/)：核心分析框架
- [`NetCorePal.Extensions.CodeAnalysis.SourceGenerators`](../NetCorePal.Extensions.CodeAnalysis.SourceGenerators/)：用于自动分析的源生成器

## 本地开发

```bash
cd src/NetCorePal.Extensions.CodeAnalysis.Tools
dotnet pack -o .
dotnet tool uninstall -g NetCorePal.Extensions.CodeAnalysis.Tools
dotnet tool install -g NetCorePal.Extensions.CodeAnalysis.Tools --add-source .
```

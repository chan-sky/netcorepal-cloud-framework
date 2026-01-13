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

### 前提条件

- **需要安装 .NET 10 SDK**：单文件执行依赖 .NET 10 特性
- **SDK 与目标框架的区别**：虽然工具需要 .NET 10 SDK 来运行，但它可以分析使用 `net8.0`、`net9.0` 或 `net10.0` 作为目标框架的项目
- 目标项目必须引用 `NetCorePal.Extensions.CodeAnalysis` 包
- 该包包含源生成器，在编译时自动生成代码分析元数据

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

### 命令参数

| 选项 | 别名 | 类型 | 默认值 | 说明 |
|---|---|---|---|---|
| `--solution <solution>` | `-s` | 文件路径 | 无 | 要分析的解决方案文件，支持 `.sln`/`.slnx` |
| `--project <project>` | `-p` | 文件路径（可多次） | 无 | 要分析的项目文件（`.csproj`），可重复指定多个 |
| `--output <output>` | `-o` | 文件路径 | `architecture-visualization.html` | 输出的 HTML 文件路径 |
| `--title <title>` | `-t` | 字符串 | `架构可视化` | 生成页面的标题 |
| `--verbose` | `-v` | 开关 | `false` | 启用详细日志输出 |
| `--include-tests` | 无 | 开关 | `false` | 包含测试项目（默认不包含；规则见下文“测试项目识别规则”） |

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

### 测试项目识别规则

- 默认行为：测试项目会被排除在分析之外（除非显式传入 `--include-tests`）。
- 判定规则（满足任一即视为测试项目）：
  - 项目文件所在路径的任一父级目录名为 `test` 或 `tests`（不区分大小写）。
  - 项目文件（.csproj）中包含 `<IsTestProject>true</IsTestProject>`。

若需包含测试项目，请使用：

```bash
netcorepal-codeanalysis generate --include-tests
```

## 工作原理

该工具采用基于 .NET 10 单文件执行能力的全新架构：

1. **项目发现**：自动发现目标解决方案或项目文件
2. **依赖分析**：递归分析项目引用，获取所有相关项目
3. **动态代码生成**：生成包含 `#:project` 指令的临时 C# 文件
4. **单文件执行**：使用 `dotnet run app.cs` 执行分析
5. **结果生成**：生成交互式 HTML 可视化文件
6. **自动清理**：删除临时文件

## 完整文档

详细的使用说明、命令行选项、集成方式和故障排除，请参阅：

- [中文文档](https://netcorepal.github.io/netcorepal-cloud-framework/zh/code-analysis/code-analysis-tools/)
- [English Documentation](https://netcorepal.github.io/netcorepal-cloud-framework/en/code-analysis/code-analysis-tools/)

## 本地开发

```bash
cd src/NetCorePal.Extensions.CodeAnalysis.Tools
dotnet pack -o .
dotnet tool uninstall -g NetCorePal.Extensions.CodeAnalysis.Tools
dotnet tool install -g NetCorePal.Extensions.CodeAnalysis.Tools --add-source .

# test
dotnet test test/NetCorePal.Extensions.CodeAnalysis.Tools.UnitTests/NetCorePal.Extensions.CodeAnalysis.Tools.UnitTests.csproj
```

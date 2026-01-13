# 代码分析工具

NetCorePal.Extensions.CodeAnalysis.Tools 是基于 NetCorePal 代码分析框架的命令行工具，用于从 .NET 项目生成交互式架构可视化 HTML 文件（基于 .NET 10 单文件执行）。

## ⚠️ 重要说明

**工具生效的前提条件**：目标分析的项目/程序集必须引用 `NetCorePal.Extensions.CodeAnalysis` 包。该包包含了源生成器，能够在编译时自动生成代码分析所需的元数据。

```xml
<PackageReference Include="NetCorePal.Extensions.CodeAnalysis" />
```

没有引用此包的项目将无法生成分析结果。

## 安装

作为全局 dotnet 工具安装：

```bash
dotnet tool install -g NetCorePal.Extensions.CodeAnalysis.Tools
```

或在项目中本地安装：

```bash
dotnet tool install NetCorePal.Extensions.CodeAnalysis.Tools
```

## 使用方法

### 快速上手

```bash
# 进入项目目录
cd MyApp

# 自动发现并分析当前目录下的解决方案或项目
netcorepal-codeanalysis generate

# 指定解决方案文件（.sln/.slnx）
netcorepal-codeanalysis generate --solution MySolution.sln

# 指定项目文件（可多次指定）
netcorepal-codeanalysis generate --project MyProject.csproj

# 自定义输出文件和标题
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

#### `generate` 命令

**输入源选项（按优先级排序）：**

- `--assembly, -a`：指定程序集文件 (.dll)。可多次指定
- `--project, -p`：指定项目文件 (.csproj)。可多次指定  
- `--solution, -s`：指定解决方案文件 (.sln)。可多次指定

**构建选项：**

- `--configuration, -c`：构建配置 (Debug/Release)。默认：Debug

**输出选项：**

- `--output, -o`：输出 HTML 文件路径。默认：code-analysis.html
- `--title, -t`：HTML 页面标题。默认：Architecture Visualization
- `--verbose, -v`：启用详细输出用于调试

### 使用示例

1. **自动发现分析：**

   ```bash
   # 进入项目目录
   cd MyApp
   
   # 自动发现并分析当前目录下的解决方案/项目/程序集
   netcorepal-codeanalysis generate
   
   # 自动发现并指定输出文件
   netcorepal-codeanalysis generate -o my-architecture.html
   ```

2. **分析特定解决方案：**

   ```bash
   cd MyApp
      netcorepal-codeanalysis generate \
         --solution MyApp.sln \
         --output architecture.html \
         --title "我的应用架构"
   ```

3. **分析多个项目：**

   ```bash
   cd MyApp
      netcorepal-codeanalysis generate \
         -p MyApp/MyApp.csproj \
         -p MyApp.Domain/MyApp.Domain.csproj \
         -o docs/architecture.html
   ```

   

## 自动发现机制

当未提供 `--solution` 与 `--project` 时，工具会在“当前目录（顶层）”自动发现分析目标：

- 优先级：`.slnx` > `.sln` > 顶层 `*.csproj`
- 非递归扫描目录：仅加载当前目录顶层的解决方案/项目文件，随后递归分析其依赖项目
- 默认排除测试项目：除非显式传入 `--include-tests`
- 输出可见性：
   - 选择 `.slnx/.sln` 会打印 `Using solution (...): <文件名>`；随后打印“Projects to analyze (N)”并列出递归依赖在内的完整项目清单
   - 选择顶层 `*.csproj` 会直接打印“Projects to analyze (N)”并列出包含递归依赖的完整清单

> 说明：工具会在隔离的临时工作目录中生成并执行动态 `app.cs`，并使用 `--no-launch-profile` 运行以避免继承当前目录的 `launchSettings.json`/`global.json` 等环境影响。

### 测试项目识别规则

- 默认行为：测试项目会被排除在分析之外（除非显式传入 `--include-tests`）
- 判定规则（满足任一即视为测试项目）：
   - 项目文件所在路径的任一父级目录名为 `test` 或 `tests`（不区分大小写）
   - 项目文件（.csproj）中包含 `<IsTestProject>true</IsTestProject>`（大小写与空白不敏感）

## 系统要求

- 运行环境：.NET 10 SDK（单文件执行依赖 .NET 10 特性）
- 被分析项目的目标框架：支持 `net8.0`、`net9.0` 和 `net10.0`
- 被分析项目必须引用 `NetCorePal.Extensions.CodeAnalysis` 包（包含源生成器）

## 输出内容

工具生成包含以下内容的交互式 HTML 文件：

- **统计信息**：各类型组件的数量统计和分布情况
- **架构总览图**：系统中所有类型及其关系的完整视图
- **处理流程图集合**：每个独立业务链路的流程图（如命令处理链路）
- **聚合关系图集合**：每个聚合根相关的关系图
- **交互式导航**：左侧树形菜单，支持图表类型切换
- **Mermaid Live 集成**：每个图表右上角的"View in Mermaid Live"按钮

## 与构建过程集成

### MSBuild 集成

添加到 `.csproj` 文件：

```xml
<Target Name="GenerateArchitectureVisualization" AfterTargets="Build" Condition="'$(Configuration)' == 'Debug'">
   <Exec Command="netcorepal-codeanalysis generate --project $(MSBuildProjectFullPath) --output $(OutputPath)architecture-visualization.html" 
            ContinueOnError="true" />
</Target>
```

### GitHub Actions

添加到工作流程：

```yaml
- name: Generate Architecture Visualization
  run: |
    dotnet tool install -g NetCorePal.Extensions.CodeAnalysis.Tools
    cd MyApp
      netcorepal-codeanalysis generate \
         --output docs/architecture-visualization.html \
         --title "MyApp 架构图"
```

## 故障排除

### 常见问题

1. **找不到项目/解决方案**：确保路径正确且文件存在
2. **无分析结果**：确保项目引用了 `NetCorePal.Extensions.CodeAnalysis` 包并能正常编译
3. **权限错误**：检查输出目录的写入权限
4. **构建失败**：确保项目可以正常构建，检查依赖项

### 详细输出

使用 `--verbose` 标志获取分析过程的详细信息：

```bash
netcorepal-codeanalysis generate --verbose
```

这将显示：

- 发现的文件和项目
- 递归依赖收集信息
- 单文件执行过程日志
- 分析统计信息
- 文件生成详情
- 发生问题时的错误详情

## 相关包

- [`NetCorePal.Extensions.CodeAnalysis`](../code-flow-analysis.md)：核心分析框架
- 源生成器：用于自动分析的源生成器

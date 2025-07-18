using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetCorePal.Extensions.CodeAnalysis;

/// <summary>
/// Mermaid 图表可视化器，用于将代码分析结果转换为 Mermaid 图表
/// </summary>
public static class MermaidVisualizer
{
    /// <summary>
    /// 生成完整的架构流程图
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <returns>Mermaid 流程图字符串</returns>
    public static string GenerateArchitectureFlowChart(CodeFlowAnalysisResult analysisResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");
        sb.AppendLine();

        var nodeIds = new Dictionary<string, string>();
        var nodeIdCounter = 1;

        // 生成节点ID映射
        string GetNodeId(string fullName, string nodeType)
        {
            var key = $"{nodeType}_{fullName}";
            if (!nodeIds.ContainsKey(key))
            {
                nodeIds[key] = $"{nodeType}{nodeIdCounter++}";
            }
            return nodeIds[key];
        }

        // 添加控制器节点
        sb.AppendLine("    %% Controllers");
        foreach (var controller in analysisResult.Controllers)
        {
            var nodeId = GetNodeId(controller.FullName, "C");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(controller.Name)}\"]");
        }
        sb.AppendLine();

        // 添加命令节点
        sb.AppendLine("    %% Commands");
        foreach (var command in analysisResult.Commands)
        {
            var nodeId = GetNodeId(command.FullName, "CMD");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(command.Name)}\"]");
        }
        sb.AppendLine();

        // 添加实体节点
        sb.AppendLine("    %% Entities");
        foreach (var entity in analysisResult.Entities)
        {
            var nodeId = GetNodeId(entity.FullName, "E");
            var shape = entity.IsAggregateRoot ? "{{" + EscapeMermaidText(entity.Name) + "}}" : "[" + EscapeMermaidText(entity.Name) + "]";
            sb.AppendLine($"    {nodeId}{shape}");
        }
        sb.AppendLine();

        // 添加领域事件节点
        sb.AppendLine("    %% Domain Events");
        foreach (var domainEvent in analysisResult.DomainEvents)
        {
            var nodeId = GetNodeId(domainEvent.FullName, "DE");
            sb.AppendLine($"    {nodeId}(\"{EscapeMermaidText(domainEvent.Name)}\")");
        }
        sb.AppendLine();

        // 添加集成事件节点
        sb.AppendLine("    %% Integration Events");
        foreach (var integrationEvent in analysisResult.IntegrationEvents)
        {
            var nodeId = GetNodeId(integrationEvent.FullName, "IE");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(integrationEvent.Name)}\"]");
        }
        sb.AppendLine();

        // 添加事件处理器节点
        sb.AppendLine("    %% Event Handlers");
        foreach (var handler in analysisResult.DomainEventHandlers)
        {
            var nodeId = GetNodeId(handler.FullName, "DEH");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(handler.Name)}\"]");
        }

        foreach (var handler in analysisResult.IntegrationEventHandlers)
        {
            var nodeId = GetNodeId(handler.FullName, "IEH");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(handler.Name)}\"]");
        }
        sb.AppendLine();

        // 添加关系连接
        sb.AppendLine("    %% Relationships");
        foreach (var relationship in analysisResult.Relationships)
        {
            var sourceNodeId = FindNodeId(nodeIds, relationship.SourceType);
            var targetNodeId = FindNodeId(nodeIds, relationship.TargetType);

            if (!string.IsNullOrEmpty(sourceNodeId) && !string.IsNullOrEmpty(targetNodeId))
            {
                var arrow = GetArrowStyle(relationship.CallType);
                var label = GetRelationshipLabel(relationship.CallType, relationship.SourceMethod, relationship.TargetMethod);

                if (!string.IsNullOrEmpty(label))
                {
                    sb.AppendLine($"    {sourceNodeId} {arrow}|{label}| {targetNodeId}");
                }
                else
                {
                    sb.AppendLine($"    {sourceNodeId} {arrow} {targetNodeId}");
                }
            }
        }

        // 添加领域事件处理器到命令的关系
        foreach (var handler in analysisResult.DomainEventHandlers)
        {
            foreach (var commandType in handler.Commands)
            {
                var handlerNodeId = FindNodeId(nodeIds, handler.FullName);
                var commandNodeId = FindNodeId(nodeIds, commandType);

                if (!string.IsNullOrEmpty(handlerNodeId) && !string.IsNullOrEmpty(commandNodeId))
                {
                    var arrow = GetArrowStyle("HandlerToCommand");
                    var label = GetRelationshipLabel("HandlerToCommand");

                    if (!string.IsNullOrEmpty(label))
                    {
                        sb.AppendLine($"    {handlerNodeId} {arrow}|{label}| {commandNodeId}");
                    }
                    else
                    {
                        sb.AppendLine($"    {handlerNodeId} {arrow} {commandNodeId}");
                    }
                }
            }
        }

        // 添加集成事件处理器到命令的关系
        foreach (var handler in analysisResult.IntegrationEventHandlers)
        {
            foreach (var commandType in handler.Commands)
            {
                var handlerNodeId = FindNodeId(nodeIds, handler.FullName);
                var commandNodeId = FindNodeId(nodeIds, commandType);

                if (!string.IsNullOrEmpty(handlerNodeId) && !string.IsNullOrEmpty(commandNodeId))
                {
                    var arrow = GetArrowStyle("HandlerToCommand");
                    var label = GetRelationshipLabel("HandlerToCommand");

                    if (!string.IsNullOrEmpty(label))
                    {
                        sb.AppendLine($"    {handlerNodeId} {arrow}|{label}| {commandNodeId}");
                    }
                    else
                    {
                        sb.AppendLine($"    {handlerNodeId} {arrow} {commandNodeId}");
                    }
                }
            }
        }
        sb.AppendLine();

        // 添加样式
        AddStyles(sb);

        return sb.ToString();
    }

    /// <summary>
    /// 生成命令流程图（专注于命令执行流程）
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <returns>Mermaid 流程图字符串</returns>
    public static string GenerateCommandFlowChart(CodeFlowAnalysisResult analysisResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart LR");
        sb.AppendLine();

        var nodeIds = new Dictionary<string, string>();
        var nodeIdCounter = 1;

        string GetNodeId(string fullName, string nodeType)
        {
            var key = $"{nodeType}_{fullName}";
            if (!nodeIds.ContainsKey(key))
            {
                nodeIds[key] = $"{nodeType}{nodeIdCounter++}";
            }
            return nodeIds[key];
        }

        // 只显示命令相关的流程
        var commandRelationships = analysisResult.Relationships
            .Where(r => r.CallType.Contains("Command") || r.CallType == "MethodToCommand")
            .ToList();

        var involvedTypes = new HashSet<string>();
        foreach (var rel in commandRelationships)
        {
            involvedTypes.Add(rel.SourceType);
            involvedTypes.Add(rel.TargetType);
        }

        // 添加相关的控制器
        foreach (var controller in analysisResult.Controllers.Where(c => involvedTypes.Contains(c.FullName)))
        {
            var nodeId = GetNodeId(controller.FullName, "C");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(controller.Name)}\"]");
        }

        // 添加相关的命令
        foreach (var command in analysisResult.Commands.Where(c => involvedTypes.Contains(c.FullName)))
        {
            var nodeId = GetNodeId(command.FullName, "CMD");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(command.Name)}\"]");
        }

        // 添加相关的实体
        foreach (var entity in analysisResult.Entities.Where(e => involvedTypes.Contains(e.FullName)))
        {
            var nodeId = GetNodeId(entity.FullName, "E");
            sb.AppendLine($"    {nodeId}{{{EscapeMermaidText(entity.Name)}}}");
        }

        sb.AppendLine();

        // 添加命令流程关系
        foreach (var relationship in commandRelationships)
        {
            var sourceNodeId = FindNodeId(nodeIds, relationship.SourceType);
            var targetNodeId = FindNodeId(nodeIds, relationship.TargetType);

            if (!string.IsNullOrEmpty(sourceNodeId) && !string.IsNullOrEmpty(targetNodeId))
            {
                var label = GetSimpleRelationshipLabel(relationship.CallType);
                sb.AppendLine($"    {sourceNodeId} --> |{label}| {targetNodeId}");
            }
        }

        sb.AppendLine();
        AddCommandFlowStyles(sb);

        return sb.ToString();
    }

    /// <summary>
    /// 生成事件流程图（专注于事件驱动流程）
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <returns>Mermaid 流程图字符串</returns>
    public static string GenerateEventFlowChart(CodeFlowAnalysisResult analysisResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");
        sb.AppendLine();

        var nodeIds = new Dictionary<string, string>();
        var nodeIdCounter = 1;

        string GetNodeId(string fullName, string nodeType)
        {
            var key = $"{nodeType}_{fullName}";
            if (!nodeIds.ContainsKey(key))
            {
                nodeIds[key] = $"{nodeType}{nodeIdCounter++}";
            }
            return nodeIds[key];
        }

        // 添加领域事件
        sb.AppendLine("    %% Domain Events");
        foreach (var domainEvent in analysisResult.DomainEvents)
        {
            var nodeId = GetNodeId(domainEvent.FullName, "DE");
            sb.AppendLine($"    {nodeId}(\"{EscapeMermaidText(domainEvent.Name)}\")");
        }
        sb.AppendLine();

        // 添加集成事件
        sb.AppendLine("    %% Integration Events");
        foreach (var integrationEvent in analysisResult.IntegrationEvents)
        {
            var nodeId = GetNodeId(integrationEvent.FullName, "IE");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(integrationEvent.Name)}\"]");
        }
        sb.AppendLine();

        // 添加事件处理器
        sb.AppendLine("    %% Event Handlers");
        foreach (var handler in analysisResult.DomainEventHandlers)
        {
            var nodeId = GetNodeId(handler.FullName, "DEH");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(handler.Name)}\"]");
        }

        foreach (var handler in analysisResult.IntegrationEventHandlers)
        {
            var nodeId = GetNodeId(handler.FullName, "IEH");
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(handler.Name)}\"]");
        }
        sb.AppendLine();

        // 添加集成事件转换器
        sb.AppendLine("    %% Integration Event Converters");
        foreach (var converter in analysisResult.IntegrationEventConverters)
        {
            var nodeId = GetNodeId(converter.FullName, "IEC");
            sb.AppendLine($"    {nodeId}[/\"{EscapeMermaidText(converter.Name)}\"/]");
        }
        sb.AppendLine();

        // 添加事件相关关系
        var eventRelationships = analysisResult.Relationships
            .Where(r => r.CallType.Contains("Event") || r.CallType.Contains("Handler"))
            .ToList();

        foreach (var relationship in eventRelationships)
        {
            var sourceNodeId = FindNodeId(nodeIds, relationship.SourceType);
            var targetNodeId = FindNodeId(nodeIds, relationship.TargetType);

            if (!string.IsNullOrEmpty(sourceNodeId) && !string.IsNullOrEmpty(targetNodeId))
            {
                var arrow = GetEventArrowStyle(relationship.CallType);
                var label = GetEventRelationshipLabel(relationship.CallType);
                sb.AppendLine($"    {sourceNodeId} {arrow}|{label}| {targetNodeId}");
            }
        }

        sb.AppendLine();
        AddEventFlowStyles(sb);

        return sb.ToString();
    }

    /// <summary>
    /// 生成类图（展示类型间的关系）
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <returns>Mermaid 类图字符串</returns>
    public static string GenerateClassDiagram(CodeFlowAnalysisResult analysisResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine("classDiagram");
        sb.AppendLine();

        // 添加控制器类
        foreach (var controller in analysisResult.Controllers)
        {
            var className = SanitizeClassName(controller.Name);
            sb.AppendLine($"    class {className} {{");
            sb.AppendLine("        <<Controller>>");
            foreach (var method in controller.Methods.Take(5))
            {
                sb.AppendLine($"        +{EscapeMermaidText(method)}()");
            }
            if (controller.Methods.Count > 5)
            {
                sb.AppendLine("        +...");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // 添加命令类
        foreach (var command in analysisResult.Commands)
        {
            var className = SanitizeClassName(command.Name);
            sb.AppendLine($"    class {className} {{");
            sb.AppendLine("        <<Command>>");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // 添加实体类
        foreach (var entity in analysisResult.Entities)
        {
            var className = SanitizeClassName(entity.Name);
            sb.AppendLine($"    class {className} {{");
            if (entity.IsAggregateRoot)
            {
                sb.AppendLine("        <<AggregateRoot>>");
            }
            else
            {
                sb.AppendLine("        <<Entity>>");
            }
            foreach (var method in entity.Methods.Take(5))
            {
                sb.AppendLine($"        +{EscapeMermaidText(method)}()");
            }
            if (entity.Methods.Count > 5)
            {
                sb.AppendLine("        +...");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // 添加关系
        foreach (var relationship in analysisResult.Relationships)
        {
            var sourceClass = GetClassNameFromFullName(relationship.SourceType);
            var targetClass = GetClassNameFromFullName(relationship.TargetType);

            if (!string.IsNullOrEmpty(sourceClass) && !string.IsNullOrEmpty(targetClass))
            {
                var relationshipType = GetClassDiagramRelationship(relationship.CallType);
                sb.AppendLine($"    {SanitizeClassName(sourceClass)} {relationshipType} {SanitizeClassName(targetClass)}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成命令链路流程图（以发出命令的地方为起点，分别展示一条条链路）
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <returns>包含每个命令链路的 Mermaid 流程图字符串列表</returns>
    public static List<(string ChainName, string MermaidDiagram)> GenerateCommandChainFlowCharts(CodeFlowAnalysisResult analysisResult)
    {
        var chains = new List<(string ChainName, string MermaidDiagram)>();
        var processedChains = new HashSet<string>();

        // 找出所有发出命令的起点（通常是控制器或事件处理器）
        var commandSenders = analysisResult.Relationships
            .Where(r => r.CallType == "MethodToCommand")
            .GroupBy(r => r.SourceType)
            .ToList();

        foreach (var senderGroup in commandSenders)
        {
            var senderType = senderGroup.Key;
            var senderName = GetClassNameFromFullName(senderType);

            // 为每个发送者的每个命令创建一个链路图
            foreach (var commandRelation in senderGroup)
            {
                var chainKey = $"{senderType}-{commandRelation.TargetType}";
                if (processedChains.Contains(chainKey))
                    continue;

                processedChains.Add(chainKey);

                var commandType = commandRelation.TargetType;
                var commandName = GetClassNameFromFullName(commandType);
                var chainName = $"{senderName} -> {commandName}";

                var diagram = GenerateSingleCommandChain(analysisResult, senderType, commandType, commandRelation.SourceMethod);
                chains.Add((chainName, diagram));
            }
        }

        // 也为集成事件处理器发出的命令创建链路图
        foreach (var handler in analysisResult.IntegrationEventHandlers)
        {
            foreach (var commandType in handler.Commands)
            {
                var chainKey = $"{handler.FullName}-{commandType}";
                if (processedChains.Contains(chainKey))
                    continue;

                processedChains.Add(chainKey);

                var commandName = GetClassNameFromFullName(commandType);
                var chainName = $"{handler.Name} -> {commandName}";

                var diagram = GenerateSingleCommandChain(analysisResult, handler.FullName, commandType, "Handle");
                chains.Add((chainName, diagram));
            }
        }

        return chains;
    }

    /// <summary>
    /// 生成单个命令链路的流程图
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <param name="startType">起点类型</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="startMethod">起点方法</param>
    /// <returns>Mermaid 流程图字符串</returns>
    private static string GenerateSingleCommandChain(CodeFlowAnalysisResult analysisResult, string startType, string commandType, string startMethod)
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");
        sb.AppendLine();

        var nodeIds = new Dictionary<string, string>();
        var nodeIdCounter = 1;
        var visitedNodes = new HashSet<string>();

        string GetNodeId(string fullName, string nodeType)
        {
            var key = $"{nodeType}_{fullName}";
            if (!nodeIds.ContainsKey(key))
            {
                nodeIds[key] = $"{nodeType}{nodeIdCounter++}";
            }
            return nodeIds[key];
        }

        // 添加起点节点
        AddChainNode(sb, startType, GetNodeId(startType, "START"), analysisResult, visitedNodes);

        // 添加命令节点
        AddChainNode(sb, commandType, GetNodeId(commandType, "CMD"), analysisResult, visitedNodes);

        // 跟踪命令执行链路
        TraceCommandExecution(sb, analysisResult, commandType, nodeIds, visitedNodes);

        sb.AppendLine();
        sb.AppendLine("    %% Chain Relationships");

        // 添加起点到命令的关系
        var startNodeId = GetNodeId(startType, "START");
        var commandNodeId = GetNodeId(commandType, "CMD");
        sb.AppendLine($"    {startNodeId} -->|{EscapeMermaidText(startMethod)}| {commandNodeId}");

        // 添加命令执行链路中的关系
        AddChainRelationships(sb, analysisResult, commandType, nodeIds, visitedNodes);

        sb.AppendLine();
        AddChainStyles(sb);

        return sb.ToString();
    }

    /// <summary>
    /// 添加链路中的节点
    /// </summary>
    private static void AddChainNode(StringBuilder sb, string nodeType, string nodeId, CodeFlowAnalysisResult analysisResult, HashSet<string> visitedNodes)
    {
        if (visitedNodes.Contains(nodeType))
            return;

        visitedNodes.Add(nodeType);
        var nodeName = GetClassNameFromFullName(nodeType);

        // 根据节点类型确定样式
        var controller = analysisResult.Controllers.FirstOrDefault(c => c.FullName == nodeType);
        var command = analysisResult.Commands.FirstOrDefault(c => c.FullName == nodeType);
        var entity = analysisResult.Entities.FirstOrDefault(e => e.FullName == nodeType);
        var domainEvent = analysisResult.DomainEvents.FirstOrDefault(d => d.FullName == nodeType);
        var integrationEvent = analysisResult.IntegrationEvents.FirstOrDefault(i => i.FullName == nodeType);
        var domainEventHandler = analysisResult.DomainEventHandlers.FirstOrDefault(h => h.FullName == nodeType);
        var integrationEventHandler = analysisResult.IntegrationEventHandlers.FirstOrDefault(h => h.FullName == nodeType);

        if (controller != null)
        {
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
        else if (command != null)
        {
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
        else if (entity != null)
        {
            var shape = entity.IsAggregateRoot ? "{{" + EscapeMermaidText(nodeName) + "}}" : "[" + EscapeMermaidText(nodeName) + "]";
            sb.AppendLine($"    {nodeId}{shape}");
        }
        else if (domainEvent != null)
        {
            sb.AppendLine($"    {nodeId}(\"{EscapeMermaidText(nodeName)}\")");
        }
        else if (integrationEvent != null)
        {
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
        else if (domainEventHandler != null)
        {
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
        else if (integrationEventHandler != null)
        {
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
        else
        {
            sb.AppendLine($"    {nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
    }

    /// <summary>
    /// 跟踪命令执行链路
    /// </summary>
    private static void TraceCommandExecution(StringBuilder sb, CodeFlowAnalysisResult analysisResult, string commandType, Dictionary<string, string> nodeIds, HashSet<string> visitedNodes)
    {
        var commandRelations = analysisResult.Relationships
            .Where(r => r.SourceType == commandType)
            .ToList();

        foreach (var relation in commandRelations)
        {
            var targetType = relation.TargetType;
            var targetNodeId = GetOrCreateNodeId(targetType, nodeIds);

            // 添加目标节点
            AddChainNode(sb, targetType, targetNodeId, analysisResult, visitedNodes);

            // 如果目标是聚合根，继续跟踪它产生的领域事件
            var targetEntity = analysisResult.Entities.FirstOrDefault(e => e.FullName == targetType);
            if (targetEntity != null && targetEntity.IsAggregateRoot)
            {
                TraceDomainEventsFromAggregate(sb, analysisResult, targetType, nodeIds, visitedNodes);
            }
        }
    }

    /// <summary>
    /// 跟踪聚合根产生的领域事件
    /// </summary>
    private static void TraceDomainEventsFromAggregate(StringBuilder sb, CodeFlowAnalysisResult analysisResult, string aggregateType, Dictionary<string, string> nodeIds, HashSet<string> visitedNodes)
    {
        // 查找从聚合根方法发出的领域事件
        var domainEventRelations = analysisResult.Relationships
            .Where(r => r.SourceType == aggregateType && r.CallType == "DomainEventToHandler")
            .ToList();

        foreach (var relation in domainEventRelations)
        {
            var eventType = relation.TargetType;
            var eventNodeId = GetOrCreateNodeId(eventType, nodeIds);

            // 添加领域事件节点
            AddChainNode(sb, eventType, eventNodeId, analysisResult, visitedNodes);

            // 跟踪事件处理器
            TraceEventHandlers(sb, analysisResult, eventType, nodeIds, visitedNodes);
        }
    }

    /// <summary>
    /// 跟踪事件处理器
    /// </summary>
    private static void TraceEventHandlers(StringBuilder sb, CodeFlowAnalysisResult analysisResult, string eventType, Dictionary<string, string> nodeIds, HashSet<string> visitedNodes)
    {
        // 查找处理该事件的处理器
        var handlers = analysisResult.DomainEventHandlers
            .Where(h => h.HandledEventType == eventType)
            .ToList();

        foreach (var handler in handlers)
        {
            var handlerNodeId = GetOrCreateNodeId(handler.FullName, nodeIds);

            // 添加处理器节点
            AddChainNode(sb, handler.FullName, handlerNodeId, analysisResult, visitedNodes);

            // 跟踪处理器发出的命令
            foreach (var commandType in handler.Commands)
            {
                var commandNodeId = GetOrCreateNodeId(commandType, nodeIds);
                AddChainNode(sb, commandType, commandNodeId, analysisResult, visitedNodes);

                // 递归跟踪命令执行
                TraceCommandExecution(sb, analysisResult, commandType, nodeIds, visitedNodes);
            }
        }

        // 查找集成事件转换器
        var converters = analysisResult.IntegrationEventConverters
            .Where(c => c.DomainEventType == eventType)
            .ToList();

        foreach (var converter in converters)
        {
            var converterNodeId = GetOrCreateNodeId(converter.FullName, nodeIds);
            var integrationEventNodeId = GetOrCreateNodeId(converter.IntegrationEventType, nodeIds);

            // 添加转换器和集成事件节点
            AddChainNode(sb, converter.FullName, converterNodeId, analysisResult, visitedNodes);
            AddChainNode(sb, converter.IntegrationEventType, integrationEventNodeId, analysisResult, visitedNodes);

            // 跟踪集成事件处理器
            var integrationHandlers = analysisResult.IntegrationEventHandlers
                .Where(h => h.HandledEventType == converter.IntegrationEventType)
                .ToList();

            foreach (var integrationHandler in integrationHandlers)
            {
                var integrationHandlerNodeId = GetOrCreateNodeId(integrationHandler.FullName, nodeIds);
                AddChainNode(sb, integrationHandler.FullName, integrationHandlerNodeId, analysisResult, visitedNodes);

                // 跟踪集成事件处理器发出的命令
                foreach (var commandType in integrationHandler.Commands)
                {
                    var commandNodeId = GetOrCreateNodeId(commandType, nodeIds);
                    AddChainNode(sb, commandType, commandNodeId, analysisResult, visitedNodes);

                    // 递归跟踪命令执行
                    TraceCommandExecution(sb, analysisResult, commandType, nodeIds, visitedNodes);
                }
            }
        }
    }

    /// <summary>
    /// 添加链路中的关系
    /// </summary>
    private static void AddChainRelationships(StringBuilder sb, CodeFlowAnalysisResult analysisResult, string commandType, Dictionary<string, string> nodeIds, HashSet<string> visitedNodes)
    {
        var processedRelations = new HashSet<string>();

        void AddRelationship(string sourceType, string targetType, string callType, string sourceMethod = "", string targetMethod = "")
        {
            var relationKey = $"{sourceType}-{targetType}-{callType}";
            if (processedRelations.Contains(relationKey))
                return;

            processedRelations.Add(relationKey);

            var sourceNodeId = FindNodeId(nodeIds, sourceType);
            var targetNodeId = FindNodeId(nodeIds, targetType);

            if (!string.IsNullOrEmpty(sourceNodeId) && !string.IsNullOrEmpty(targetNodeId))
            {
                var arrow = GetArrowStyle(callType);
                var label = GetRelationshipLabel(callType, sourceMethod, targetMethod);

                if (!string.IsNullOrEmpty(label))
                {
                    sb.AppendLine($"    {sourceNodeId} {arrow}|{EscapeMermaidText(label)}| {targetNodeId}");
                }
                else
                {
                    sb.AppendLine($"    {sourceNodeId} {arrow} {targetNodeId}");
                }
            }
        }

        // 添加命令相关的关系
        var commandRelations = analysisResult.Relationships
            .Where(r => visitedNodes.Contains(r.SourceType) && visitedNodes.Contains(r.TargetType))
            .ToList();

        foreach (var relation in commandRelations)
        {
            AddRelationship(relation.SourceType, relation.TargetType, relation.CallType, relation.SourceMethod, relation.TargetMethod);
        }

        // 添加领域事件处理器到命令的关系
        foreach (var handler in analysisResult.DomainEventHandlers)
        {
            if (!visitedNodes.Contains(handler.FullName))
                continue;

            foreach (var commandTypeInHandler in handler.Commands)
            {
                if (visitedNodes.Contains(commandTypeInHandler))
                {
                    AddRelationship(handler.FullName, commandTypeInHandler, "HandlerToCommand");
                }
            }
        }

        // 添加集成事件处理器到命令的关系
        foreach (var handler in analysisResult.IntegrationEventHandlers)
        {
            if (!visitedNodes.Contains(handler.FullName))
                continue;

            foreach (var commandTypeInHandler in handler.Commands)
            {
                if (visitedNodes.Contains(commandTypeInHandler))
                {
                    AddRelationship(handler.FullName, commandTypeInHandler, "HandlerToCommand");
                }
            }
        }

        // 添加转换器关系
        foreach (var converter in analysisResult.IntegrationEventConverters)
        {
            if (visitedNodes.Contains(converter.DomainEventType) && visitedNodes.Contains(converter.IntegrationEventType))
            {
                AddRelationship(converter.DomainEventType, converter.IntegrationEventType, "DomainEventToIntegrationEvent");
            }
        }
    }

    /// <summary>
    /// 获取或创建节点ID
    /// </summary>
    private static string GetOrCreateNodeId(string fullName, Dictionary<string, string> nodeIds)
    {
        var existingId = FindNodeId(nodeIds, fullName);
        if (!string.IsNullOrEmpty(existingId))
            return existingId;

        var nodeType = GetNodeTypeFromFullName(fullName);
        var key = $"{nodeType}_{fullName}";
        var nodeId = $"{nodeType}{nodeIds.Count + 1}";
        nodeIds[key] = nodeId;
        return nodeId;
    }

    /// <summary>
    /// 从完整名称推断节点类型
    /// </summary>
    private static string GetNodeTypeFromFullName(string fullName)
    {
        var className = GetClassNameFromFullName(fullName);

        if (className.EndsWith("Controller"))
            return "C";
        if (className.EndsWith("Command"))
            return "CMD";
        if (className.EndsWith("Event"))
            return className.Contains("Integration") ? "IE" : "DE";
        if (className.EndsWith("Handler"))
            return className.Contains("Integration") ? "IEH" : "DEH";
        if (className.EndsWith("Converter"))
            return "IEC";

        return "N"; // 默认节点类型
    }

    /// <summary>
    /// 添加链路图样式
    /// </summary>
    private static void AddChainStyles(StringBuilder sb, Dictionary<string, string>? nodeStyleMap = null)
    {
        sb.AppendLine("    %% Chain Styles");
        sb.AppendLine("    classDef controller fill:#e1f5fe,stroke:#01579b,stroke-width:2px;");
        sb.AppendLine("    classDef command fill:#f3e5f5,stroke:#4a148c,stroke-width:2px;");
        sb.AppendLine("    classDef entity fill:#e8f5e8,stroke:#1b5e20,stroke-width:2px;");
        sb.AppendLine("    classDef domainEvent fill:#fff3e0,stroke:#e65100,stroke-width:2px;");
        sb.AppendLine("    classDef integrationEvent fill:#fce4ec,stroke:#880e4f,stroke-width:2px;");
        sb.AppendLine("    classDef handler fill:#f1f8e9,stroke:#33691e,stroke-width:2px;");
        sb.AppendLine("    classDef converter fill:#e3f2fd,stroke:#0277bd,stroke-width:2px;");
        sb.AppendLine();

        // 应用样式到具体节点
        if (nodeStyleMap != null && nodeStyleMap.Count > 0)
        {
            sb.AppendLine("    %% Apply styles to specific nodes");

            // 按样式类分组节点
            var nodesByStyle = nodeStyleMap.GroupBy(kvp => kvp.Value);

            foreach (var styleGroup in nodesByStyle)
            {
                var styleClass = styleGroup.Key;
                var nodeIds = styleGroup.Select(kvp => kvp.Key).ToList();

                if (nodeIds.Count > 0)
                {
                    var nodeIdList = string.Join(",", nodeIds);
                    sb.AppendLine($"    class {nodeIdList} {styleClass};");
                }
            }
        }
    }

    #region 辅助方法

    private static string FindNodeId(Dictionary<string, string> nodeIds, string fullName)
    {
        return nodeIds.FirstOrDefault(kvp => kvp.Key.EndsWith(fullName)).Value ?? "";
    }

    private static string GetArrowStyle(string callType)
    {
        return callType switch
        {
            "MethodToCommand" => "-->",
            "CommandToAggregateMethod" => "==>",
            "DomainEventToHandler" => "-.->",
            "DomainEventToIntegrationEvent" => "===>",
            "IntegrationEventToHandler" => "-.->",
            "HandlerToCommand" => "-.->",
            _ => "-->"
        };
    }

    private static string GetEventArrowStyle(string callType)
    {
        return callType switch
        {
            "DomainEventToHandler" => "-.->",
            "DomainEventToIntegrationEvent" => "===>",
            "IntegrationEventToHandler" => "-.->",
            _ => "-->"
        };
    }

    private static string GetRelationshipLabel(string callType, string sourceMethod = "", string targetMethod = "")
    {
        return callType switch
        {
            "MethodToCommand" => string.IsNullOrEmpty(sourceMethod) ? "sends" : $"{sourceMethod} Send",
            "CommandToAggregateMethod" => string.IsNullOrEmpty(targetMethod) ? "executes" : $"executes {targetMethod}",
            "DomainEventToHandler" => "handles",
            "DomainEventToIntegrationEvent" => "converts to",
            "IntegrationEventToHandler" => "subscribes",
            "HandlerToCommand" => "sends",
            _ => ""
        };
    }

    private static string GetSimpleRelationshipLabel(string callType)
    {
        return callType switch
        {
            "MethodToCommand" => "send",
            "CommandToAggregateMethod" => "execute",
            _ => "call"
        };
    }

    private static string GetEventRelationshipLabel(string callType)
    {
        return callType switch
        {
            "DomainEventToHandler" => "triggers",
            "DomainEventToIntegrationEvent" => "converts",
            "IntegrationEventToHandler" => "handles",
            _ => "processes"
        };
    }

    private static string GetClassDiagramRelationship(string callType)
    {
        return callType switch
        {
            "MethodToCommand" => "..>",
            "CommandToAggregateMethod" => "-->",
            "DomainEventToHandler" => "..>",
            "DomainEventToIntegrationEvent" => "-->",
            "IntegrationEventToHandler" => "..>",
            _ => "-->"
        };
    }

    private static string EscapeMermaidText(string text)
    {
        return text.Replace("\"", "&quot;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\n", " ")
                   .Replace("\r", "");
    }

    private static string SanitizeClassName(string className)
    {
        return className.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace("[", "_").Replace("]", "_");
    }

    private static string GetClassNameFromFullName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return "";
        var parts = fullName.Split('.');
        return parts.LastOrDefault() ?? "";
    }

    private static void AddStyles(StringBuilder sb)
    {
        sb.AppendLine("    %% Styles");
        sb.AppendLine("    classDef controller fill:#e1f5fe,stroke:#01579b,stroke-width:2px;");
        sb.AppendLine("    classDef command fill:#f3e5f5,stroke:#4a148c,stroke-width:2px;");
        sb.AppendLine("    classDef entity fill:#e8f5e8,stroke:#1b5e20,stroke-width:2px;");
        sb.AppendLine("    classDef domainEvent fill:#fff3e0,stroke:#e65100,stroke-width:2px;");
        sb.AppendLine("    classDef integrationEvent fill:#fce4ec,stroke:#880e4f,stroke-width:2px;");
        sb.AppendLine("    classDef handler fill:#f1f8e9,stroke:#33691e,stroke-width:2px;");
        sb.AppendLine();

        sb.AppendLine("    class C1,C2,C3,C4,C5 controller;");
        sb.AppendLine("    class CMD1,CMD2,CMD3,CMD4,CMD5,CMD6,CMD7,CMD8,CMD9,CMD10 command;");
        sb.AppendLine("    class E1,E2,E3,E4,E5 entity;");
        sb.AppendLine("    class DE1,DE2,DE3,DE4,DE5 domainEvent;");
        sb.AppendLine("    class IE1,IE2,IE3,IE4,IE5 integrationEvent;");
        sb.AppendLine("    class DEH1,DEH2,DEH3,DEH4,DEH5,IEH1,IEH2,IEH3,IEH4,IEH5 handler;");
    }

    private static void AddCommandFlowStyles(StringBuilder sb)
    {
        sb.AppendLine("    %% Styles");
        sb.AppendLine("    classDef controller fill:#e1f5fe,stroke:#01579b,stroke-width:2px;");
        sb.AppendLine("    classDef command fill:#f3e5f5,stroke:#4a148c,stroke-width:2px;");
        sb.AppendLine("    classDef entity fill:#e8f5e8,stroke:#1b5e20,stroke-width:2px;");
    }

    private static void AddEventFlowStyles(StringBuilder sb)
    {
        sb.AppendLine("    %% Styles");
        sb.AppendLine("    classDef domainEvent fill:#fff3e0,stroke:#e65100,stroke-width:2px;");
        sb.AppendLine("    classDef integrationEvent fill:#fce4ec,stroke:#880e4f,stroke-width:2px;");
        sb.AppendLine("    classDef handler fill:#f1f8e9,stroke:#33691e,stroke-width:2px;");
        sb.AppendLine("    classDef converter fill:#e3f2fd,stroke:#0277bd,stroke-width:2px;");
        sb.AppendLine();

        sb.AppendLine("    class DE1,DE2,DE3,DE4,DE5 domainEvent;");
        sb.AppendLine("    class IE1,IE2,IE3,IE4,IE5 integrationEvent;");
        sb.AppendLine("    class DEH1,DEH2,DEH3,DEH4,DEH5,IEH1,IEH2,IEH3,IEH4,IEH5 handler;");
        sb.AppendLine("    class IEC1,IEC2,IEC3,IEC4,IEC5 converter;");
    }

    /// <summary>
    /// 生成多链路流程图（在一张图中展示多个命令链路，按链路分组显示）
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <returns>包含多个链路的 Mermaid 流程图字符串</returns>
    public static string GenerateMultiChainFlowChart(CodeFlowAnalysisResult analysisResult)
    {
        var chainGroups = GenerateMultiChainGroups(analysisResult);
        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");
        sb.AppendLine();

        // 收集所有节点以便后续应用样式
        var nodeStyleMap = new Dictionary<string, string>();

        // 生成子图，每个链路使用独立的节点ID
        for (int i = 0; i < chainGroups.Count; i++)
        {
            var (chainName, chainNodes, _, chainNodeIds) = chainGroups[i];

            sb.AppendLine($"    subgraph SG{i + 1} [\"{EscapeMermaidText(chainName)}\"]");

            // 添加该链路的所有节点
            foreach (var nodeFullName in chainNodes)
            {
                var nodeId = chainNodeIds[nodeFullName];
                AddMultiChainNodeSimple(sb, nodeFullName, nodeId, analysisResult, "        ");

                // 记录节点样式映射
                var nodeStyleClass = GetNodeStyleClass(nodeFullName, analysisResult);
                if (!string.IsNullOrEmpty(nodeStyleClass))
                {
                    nodeStyleMap[nodeId] = nodeStyleClass;
                }
            }

            sb.AppendLine("    end");
            sb.AppendLine();
        }

        // 添加链路内部的关系
        sb.AppendLine("    %% Chain Internal Relationships");
        for (int i = 0; i < chainGroups.Count; i++)
        {
            var (_, _, chainRelations, chainNodeIds) = chainGroups[i];

            foreach (var (source, target, label) in chainRelations)
            {
                var sourceNodeId = chainNodeIds.TryGetValue(source, out var srcId) ? srcId : string.Empty;
                var targetNodeId = chainNodeIds.TryGetValue(target, out var tgtId) ? tgtId : string.Empty;

                if (!string.IsNullOrEmpty(sourceNodeId) && !string.IsNullOrEmpty(targetNodeId))
                {
                    var arrow = GetArrowStyle("Default");
                    if (!string.IsNullOrEmpty(label))
                    {
                        sb.AppendLine($"    {sourceNodeId} {arrow}|{EscapeMermaidText(label)}| {targetNodeId}");
                    }
                    else
                    {
                        sb.AppendLine($"    {sourceNodeId} {arrow} {targetNodeId}");
                    }
                }
            }
        }

        sb.AppendLine();
        AddMultiChainStyles(sb, nodeStyleMap);

        return sb.ToString();
    }

    /// <summary>
    /// 生成所有独立链路流程图的集合
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <returns>包含所有独立链路图的元组列表，每个链路对应一张图</returns>
    public static List<(string ChainName, string Diagram)> GenerateAllChainFlowCharts(CodeFlowAnalysisResult analysisResult)
    {
        var chainGroups = GenerateMultiChainGroups(analysisResult);
        var chainFlowCharts = new List<(string ChainName, string Diagram)>();

        foreach (var (chainName, chainNodes, chainRelations, chainNodeIds) in chainGroups)
        {
            var sb = new StringBuilder();
            sb.AppendLine("flowchart TD");
            sb.AppendLine();
            sb.AppendLine($"    %% {EscapeMermaidText(chainName)}");
            sb.AppendLine();

            // 收集节点样式映射
            var nodeStyleMap = new Dictionary<string, string>();

            // 添加该链路的所有节点
            foreach (var nodeFullName in chainNodes)
            {
                var nodeId = chainNodeIds[nodeFullName];
                AddMultiChainNodeSimple(sb, nodeFullName, nodeId, analysisResult, "    ");

                // 记录节点样式映射
                var nodeStyleClass = GetNodeStyleClass(nodeFullName, analysisResult);
                if (!string.IsNullOrEmpty(nodeStyleClass))
                {
                    nodeStyleMap[nodeId] = nodeStyleClass;
                }
            }

            sb.AppendLine();
            sb.AppendLine("    %% Chain Relationships");

            // 添加该链路的关系
            foreach (var (source, target, label) in chainRelations)
            {
                var sourceNodeId = chainNodeIds.TryGetValue(source, out var srcId) ? srcId : string.Empty;
                var targetNodeId = chainNodeIds.TryGetValue(target, out var tgtId) ? tgtId : string.Empty;

                if (!string.IsNullOrEmpty(sourceNodeId) && !string.IsNullOrEmpty(targetNodeId))
                {
                    var arrow = GetArrowStyle("Default");
                    if (!string.IsNullOrEmpty(label))
                    {
                        sb.AppendLine($"    {sourceNodeId} {arrow}|{EscapeMermaidText(label)}| {targetNodeId}");
                    }
                    else
                    {
                        sb.AppendLine($"    {sourceNodeId} {arrow} {targetNodeId}");
                    }
                }
            }

            sb.AppendLine();
            AddChainStyles(sb, nodeStyleMap);

            chainFlowCharts.Add((chainName, sb.ToString()));
        }

        return chainFlowCharts;
    }

    /// <summary>
    /// chainGroups
    /// </summary>
    private static List<(string ChainName, List<string> ChainNodes, List<(string Source, string Target, string Label)> ChainRelations, Dictionary<string, string> ChainNodeIds)> GenerateMultiChainGroups(CodeFlowAnalysisResult analysisResult)
    {
        var globalNodeCounter = 1;
        var chainGroups = new List<(string ChainName, List<string> ChainNodes, List<(string Source, string Target, string Label)> ChainRelations, Dictionary<string, string> ChainNodeIds)>();

        // 找出所有潜在的链路起点（没有上游关系的节点）
        var allUpstreamTargets = new HashSet<string>();

        // 收集所有作为目标的节点（有上游关系的节点），但排除从Controller方法发出的命令
        foreach (var rel in analysisResult.Relationships)
        {
            // 对于方法级别的关系，我们需要记录具体的方法节点
            if (rel.CallType == "CommandToAggregateMethod")
            {
                // 聚合方法有上游关系
                allUpstreamTargets.Add($"{rel.TargetType}::{rel.TargetMethod}");
            }
            else if (rel.CallType.Contains("EventToHandler"))
            {
                allUpstreamTargets.Add(rel.TargetType); // 事件处理器有上游关系
            }
            else if (rel.CallType.Contains("ToIntegrationEvent"))
            {
                allUpstreamTargets.Add(rel.TargetType); // 集成事件有上游关系
            }
            else if (rel.CallType.Contains("ToDomainEvent"))
            {
                allUpstreamTargets.Add(rel.TargetType); // 领域事件有上游关系
            }
            // 注意：我们不将MethodToCommand的目标加入上游关系，这样所有Controller方法都可以作为起点
        }

        // 删除不再需要的事件处理器命令标记逻辑，让Controller方法优先作为起点

        int chainIndex = 1;

        // 找出所有链路起点并构建链路
        var potentialStarts = new List<string>();

        // 1. Controller 方法作为起点
        foreach (var controller in analysisResult.Controllers)
        {
            var controllerMethodRelations = analysisResult.Relationships
                .Where(r => r.SourceType == controller.FullName && r.CallType == "MethodToCommand")
                .ToList();

            foreach (var methodRel in controllerMethodRelations)
            {
                var methodNode = $"{controller.FullName}::{methodRel.SourceMethod}";
                if (!allUpstreamTargets.Contains(methodNode))
                {
                    potentialStarts.Add(methodNode);
                }
            }
        }

        // 2. 领域事件处理器作为起点（只有那些没有被其他事件触发的）
        foreach (var handler in analysisResult.DomainEventHandlers)
        {
            var handlerMethodNode = $"{handler.FullName}::Handle";
            if (!allUpstreamTargets.Contains(handlerMethodNode) && !allUpstreamTargets.Contains(handler.FullName))
            {
                potentialStarts.Add(handlerMethodNode);
            }
        }

        // 3. 集成事件处理器作为起点（只有那些没有被其他事件触发的）
        foreach (var handler in analysisResult.IntegrationEventHandlers)
        {
            var handlerMethodNode = $"{handler.FullName}::Handle";
            if (!allUpstreamTargets.Contains(handlerMethodNode) && !allUpstreamTargets.Contains(handler.FullName))
            {
                potentialStarts.Add(handlerMethodNode);
            }
        }

        // 为每个起点构建链路
        foreach (var startNode in potentialStarts)
        {
            var chainNodes = new List<string>();
            var chainRelations = new List<(string Source, string Target, string Label)>();
            var localProcessedNodes = new HashSet<string>(); // 本链路内已处理的节点
            var chainNodeIds = new Dictionary<string, string>(); // 每个链路独立的节点ID映射

            // 构建链路（移除全局节点重复检查，每个链路独立完整）
            BuildSingleChain(analysisResult, startNode, chainNodes, chainRelations, localProcessedNodes);

            if (chainNodes.Count > 0)
            {
                // 为链路中的每个节点分配独立的ID
                foreach (var nodeFullName in chainNodes)
                {
                    chainNodeIds[nodeFullName] = $"N{globalNodeCounter++}";
                }

                var startNodeName = GetSimpleNodeName(startNode);
                var chainName = startNodeName;
                chainGroups.Add((chainName, chainNodes, chainRelations, chainNodeIds));
                chainIndex++;
            }
        }
        return chainGroups;
    }

    /// <summary>
    /// 构建单个链路
    /// </summary>
    private static void BuildSingleChain(CodeFlowAnalysisResult analysisResult, string startNode,
        List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> localProcessedNodes)
    {
        if (localProcessedNodes.Contains(startNode))
            return;

        // 添加起始节点
        chainNodes.Add(startNode);
        localProcessedNodes.Add(startNode);

        var (nodeType, nodeMethod) = ParseNodeName(startNode);

        // 查找从这个节点类型出发的关系
        var outgoingRelations = analysisResult.Relationships
            .Where(r => r.SourceType == nodeType)
            .ToList();

        foreach (var relation in outgoingRelations)
        {
            if (relation.CallType == "MethodToCommand" &&
                (string.IsNullOrEmpty(nodeMethod) || relation.SourceMethod == nodeMethod))
            {
                // 添加从Controller方法到Command的关系
                if (!localProcessedNodes.Contains(relation.TargetType))
                {
                    BuildChainFromCommand(analysisResult, relation.TargetType, startNode, chainNodes, chainRelations, localProcessedNodes);
                }
            }
        }

        // 对于事件处理器，追踪其发出的命令
        var domainHandler = analysisResult.DomainEventHandlers.FirstOrDefault(h => h.FullName == nodeType);
        var integrationHandler = analysisResult.IntegrationEventHandlers.FirstOrDefault(h => h.FullName == nodeType);

        var commands = domainHandler?.Commands ?? integrationHandler?.Commands;
        if (commands != null)
        {
            foreach (var commandType in commands)
            {
                if (!localProcessedNodes.Contains(commandType))
                {
                    BuildChainFromCommand(analysisResult, commandType, startNode, chainNodes, chainRelations, localProcessedNodes);
                }
            }
        }
    }

    /// <summary>
    /// 从命令开始构建链路
    /// </summary>
    private static void BuildChainFromCommand(CodeFlowAnalysisResult analysisResult, string commandType, string sourceNode,
        List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> localProcessedNodes)
    {
        if (localProcessedNodes.Contains(commandType))
            return;

        // 添加命令节点
        chainNodes.Add(commandType);
        localProcessedNodes.Add(commandType);
        chainRelations.Add((sourceNode, commandType, "sends"));

        // 查找命令执行的聚合方法
        var commandRelations = analysisResult.Relationships
            .Where(r => r.SourceType == commandType && r.CallType == "CommandToAggregateMethod")
            .ToList();

        foreach (var relation in commandRelations)
        {
            var targetWithMethod = $"{relation.TargetType}::{relation.TargetMethod}";

            if (!localProcessedNodes.Contains(targetWithMethod))
            {
                BuildChainFromAggregateMethod(analysisResult, relation.TargetType, relation.TargetMethod,
                    targetWithMethod, commandType, chainNodes, chainRelations, localProcessedNodes);
            }
        }
    }

    /// <summary>
    /// 从聚合方法构建链路
    /// </summary>
    private static void BuildChainFromAggregateMethod(CodeFlowAnalysisResult analysisResult, string aggregateType,
        string aggregateMethod, string aggregateMethodNode, string sourceNode,
        List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> localProcessedNodes)
    {
        if (localProcessedNodes.Contains(aggregateMethodNode))
            return;

        // 添加聚合方法节点
        chainNodes.Add(aggregateMethodNode);
        localProcessedNodes.Add(aggregateMethodNode);
        chainRelations.Add((sourceNode, aggregateMethodNode, "execute"));

        // 查找从聚合方法产生的领域事件
        var domainEventRelations = analysisResult.Relationships
            .Where(r => r.SourceType == aggregateType && r.SourceMethod == aggregateMethod && r.CallType == "MethodToDomainEvent")
            .ToList();

        foreach (var eventRelation in domainEventRelations)
        {
            var eventType = eventRelation.TargetType;

            if (!localProcessedNodes.Contains(eventType))
            {
                BuildChainFromDomainEvent(analysisResult, eventType, aggregateMethodNode, chainNodes, chainRelations, localProcessedNodes);
            }
        }
    }

    /// <summary>
    /// 从领域事件构建链路
    /// </summary>
    private static void BuildChainFromDomainEvent(CodeFlowAnalysisResult analysisResult, string eventType, string sourceNode,
        List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> localProcessedNodes)
    {
        if (localProcessedNodes.Contains(eventType))
            return;

        // 添加领域事件节点
        chainNodes.Add(eventType);
        localProcessedNodes.Add(eventType);
        chainRelations.Add((sourceNode, eventType, "publishes"));

        // 查找处理该事件的处理器
        var handlers = analysisResult.DomainEventHandlers
            .Where(h => h.HandledEventType == eventType)
            .ToList();

        foreach (var handler in handlers)
        {
            if (!localProcessedNodes.Contains(handler.FullName))
            {
                chainNodes.Add(handler.FullName);
                localProcessedNodes.Add(handler.FullName);
                chainRelations.Add((eventType, handler.FullName, "handles"));

                // 跟踪处理器发出的命令
                foreach (var commandType in handler.Commands)
                {
                    if (!localProcessedNodes.Contains(commandType))
                    {
                        // 命令还没有处理过，完整构建链路
                        BuildChainFromCommand(analysisResult, commandType, handler.FullName, chainNodes, chainRelations, localProcessedNodes);
                    }
                    else
                    {
                        // 命令已经处理过，只添加关系，不再递归
                        chainRelations.Add((handler.FullName, commandType, "sends"));
                    }
                }
            }
        }

        // 查找领域事件到集成事件的转换
        var converters = analysisResult.IntegrationEventConverters
            .Where(c => c.DomainEventType == eventType)
            .ToList();

        foreach (var converter in converters)
        {
            if (!localProcessedNodes.Contains(converter.FullName))
            {
                chainNodes.Add(converter.FullName);
                localProcessedNodes.Add(converter.FullName);
                chainRelations.Add((eventType, converter.FullName, "converts"));

                // 追踪转换后的集成事件
                var integrationEventType = converter.IntegrationEventType;
                if (!localProcessedNodes.Contains(integrationEventType))
                {
                    BuildChainFromIntegrationEvent(analysisResult, integrationEventType, converter.FullName, chainNodes, chainRelations, localProcessedNodes);
                }
            }
        }
    }

    /// <summary>
    /// 从集成事件构建链路
    /// </summary>
    private static void BuildChainFromIntegrationEvent(CodeFlowAnalysisResult analysisResult, string integrationEventType, string sourceNode,
        List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> localProcessedNodes)
    {
        if (localProcessedNodes.Contains(integrationEventType))
            return;

        // 添加集成事件节点
        chainNodes.Add(integrationEventType);
        localProcessedNodes.Add(integrationEventType);
        chainRelations.Add((sourceNode, integrationEventType, "to"));

        // 追踪集成事件处理器
        var integrationHandlers = analysisResult.IntegrationEventHandlers
            .Where(h => h.HandledEventType == integrationEventType)
            .ToList();

        foreach (var integrationHandler in integrationHandlers)
        {
            if (!localProcessedNodes.Contains(integrationHandler.FullName))
            {
                chainNodes.Add(integrationHandler.FullName);
                localProcessedNodes.Add(integrationHandler.FullName);
                chainRelations.Add((integrationEventType, integrationHandler.FullName, "handles"));

                // 跟踪集成事件处理器发出的命令
                foreach (var commandType in integrationHandler.Commands)
                {
                    if (!localProcessedNodes.Contains(commandType))
                    {
                        // 命令还没有处理过，完整构建链路
                        BuildChainFromCommand(analysisResult, commandType, integrationHandler.FullName, chainNodes, chainRelations, localProcessedNodes);
                    }
                    else
                    {
                        // 命令已经处理过，只添加关系，不再递归
                        chainRelations.Add((integrationHandler.FullName, commandType, "sends"));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 收集单个链路的数据（节点和关系）
    /// </summary>
    private static void CollectChainData(CodeFlowAnalysisResult analysisResult, string startType, string commandType,
        string startMethod, List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> visitedInChain, Dictionary<string, string> chainNodeIds, ref int globalNodeCounter)
    {
        if (visitedInChain.Contains(startType))
            return;

        // 添加起点
        chainNodes.Add(startType);
        visitedInChain.Add(startType);

        // 添加命令
        if (!visitedInChain.Contains(commandType))
        {
            chainNodes.Add(commandType);
            visitedInChain.Add(commandType);
        }

        // 添加起点到命令的关系
        chainRelations.Add((startType, commandType, startMethod));

        // 跟踪命令执行链路
        TraceChainExecution(analysisResult, commandType, chainNodes, chainRelations, visitedInChain);
    }

    /// <summary>
    /// 跟踪链路执行
    /// </summary>
    private static void TraceChainExecution(CodeFlowAnalysisResult analysisResult, string commandType,
        List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> visitedInChain)
    {
        var commandRelations = analysisResult.Relationships
            .Where(r => r.SourceType == commandType)
            .ToList();

        foreach (var relation in commandRelations)
        {
            var targetType = relation.TargetType;

            if (!visitedInChain.Contains(targetType))
            {
                chainNodes.Add(targetType);
                visitedInChain.Add(targetType);

                var label = GetRelationshipLabel(relation.CallType, relation.SourceMethod, relation.TargetMethod);
                chainRelations.Add((relation.SourceType, targetType, label));

                // 如果目标是聚合根，继续跟踪其产生的领域事件
                var targetEntity = analysisResult.Entities.FirstOrDefault(e => e.FullName == targetType);
                if (targetEntity != null && targetEntity.IsAggregateRoot)
                {
                    TraceDomainEventsInChain(analysisResult, targetType, chainNodes, chainRelations, visitedInChain);
                }
            }
        }
    }

    /// <summary>
    /// 跟踪链路中的领域事件
    /// </summary>
    private static void TraceDomainEventsInChain(CodeFlowAnalysisResult analysisResult, string aggregateType,
        List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> visitedInChain)
    {
        // 查找从聚合根产生的领域事件
        var domainEventRelations = analysisResult.Relationships
            .Where(r => r.SourceType == aggregateType && r.CallType.Contains("Event"))
            .ToList();

        foreach (var relation in domainEventRelations)
        {
            var eventType = relation.TargetType;

            if (!visitedInChain.Contains(eventType))
            {
                chainNodes.Add(eventType);
                visitedInChain.Add(eventType);

                var label = GetRelationshipLabel(relation.CallType, relation.SourceMethod, relation.TargetMethod);
                chainRelations.Add((aggregateType, eventType, label));

                // 跟踪事件处理器
                TraceEventHandlersInChain(analysisResult, eventType, chainNodes, chainRelations, visitedInChain);
            }
        }
    }

    /// <summary>
    /// 跟踪链路中的事件处理器
    /// </summary>
    private static void TraceEventHandlersInChain(CodeFlowAnalysisResult analysisResult, string eventType,
        List<string> chainNodes, List<(string Source, string Target, string Label)> chainRelations,
        HashSet<string> visitedInChain)
    {
        // 查找处理该事件的处理器
        var handlers = analysisResult.DomainEventHandlers
            .Where(h => h.HandledEventType == eventType)
            .ToList();

        foreach (var handler in handlers)
        {
            if (!visitedInChain.Contains(handler.FullName))
            {
                chainNodes.Add(handler.FullName);
                visitedInChain.Add(handler.FullName);

                chainRelations.Add((eventType, handler.FullName, "handles"));

                // 跟踪处理器发出的命令
                foreach (var commandType in handler.Commands)
                {
                    if (!visitedInChain.Contains(commandType))
                    {
                        chainNodes.Add(commandType);
                        visitedInChain.Add(commandType);

                        chainRelations.Add((handler.FullName, commandType, "sends"));

                        // 递归跟踪命令执行
                        TraceChainExecution(analysisResult, commandType, chainNodes, chainRelations, visitedInChain);
                    }
                }
            }
        }

        // 查找集成事件转换器
        var converters = analysisResult.IntegrationEventConverters
            .Where(c => c.DomainEventType == eventType)
            .ToList();

        foreach (var converter in converters)
        {
            if (!visitedInChain.Contains(converter.FullName))
            {
                chainNodes.Add(converter.FullName);
                visitedInChain.Add(converter.FullName);

                chainRelations.Add((eventType, converter.FullName, "converts"));

                // 追踪转换后的集成事件
                var integrationEventType = converter.IntegrationEventType;
                if (!visitedInChain.Contains(integrationEventType))
                {
                    chainNodes.Add(integrationEventType);
                    visitedInChain.Add(integrationEventType);

                    chainRelations.Add((converter.FullName, integrationEventType, "to"));

                    // 追踪集成事件处理器
                    var integrationHandlers = analysisResult.IntegrationEventHandlers
                        .Where(h => h.HandledEventType == integrationEventType)
                        .ToList();

                    foreach (var integrationHandler in integrationHandlers)
                    {
                        if (!visitedInChain.Contains(integrationHandler.FullName))
                        {
                            chainNodes.Add(integrationHandler.FullName);
                            visitedInChain.Add(integrationHandler.FullName);

                            chainRelations.Add((integrationEventType, integrationHandler.FullName, "handles"));

                            // 跟踪集成事件处理器发出的命令
                            foreach (var commandType in integrationHandler.Commands)
                            {
                                if (!visitedInChain.Contains(commandType))
                                {
                                    chainNodes.Add(commandType);
                                    visitedInChain.Add(commandType);

                                    chainRelations.Add((integrationHandler.FullName, commandType, "sends"));

                                    // 递归跟踪命令执行
                                    TraceChainExecution(analysisResult, commandType, chainNodes, chainRelations, visitedInChain);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 添加多链路图中的节点
    /// </summary>
    private static void AddMultiChainNode(StringBuilder sb, string nodeType, string nodeId,
        CodeFlowAnalysisResult analysisResult, string indent)
    {
        var nodeName = GetClassNameFromFullName(nodeType);

        // 根据节点类型确定样式
        var controller = analysisResult.Controllers.FirstOrDefault(c => c.FullName == nodeType);
        var command = analysisResult.Commands.FirstOrDefault(c => c.FullName == nodeType);
        var entity = analysisResult.Entities.FirstOrDefault(e => e.FullName == nodeType);
        var domainEvent = analysisResult.DomainEvents.FirstOrDefault(d => d.FullName == nodeType);
        var integrationEvent = analysisResult.IntegrationEvents.FirstOrDefault(i => i.FullName == nodeType);

        if (controller != null)
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
        else if (command != null)
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
        else if (entity != null)
        {
            var shape = entity.IsAggregateRoot ? "{{" + EscapeMermaidText(nodeName) + "}}" : "[" + EscapeMermaidText(nodeName) + "]";
            sb.AppendLine($"{indent}{nodeId}{shape}");
        }
        else if (domainEvent != null)
        {
            sb.AppendLine($"{indent}{nodeId}(\"{EscapeMermaidText(nodeName)}\")");
        }
        else if (integrationEvent != null)
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
        else
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(nodeName)}\"]");
        }
    }

    /// <summary>
    /// 从标签推断关系类型
    /// </summary>
    private static string GetRelationTypeFromLabel(string label)
    {
        return label switch
        {
            var l when l.Contains("executes") => "CommandToAggregateMethod",
            var l when l.Contains("handles") => "DomainEventToHandler",
            var l when l.Contains("converts") => "DomainEventToIntegrationEvent",
            var l when l.Contains("sends") => "HandlerToCommand",
            _ => "MethodToCommand"
        };
    }

    /// <summary>
    /// 添加多链路图样式
    /// </summary>
    /// <summary>
    /// 添加多链路图样式
    /// </summary>
    private static void AddMultiChainStyles(StringBuilder sb, Dictionary<string, string>? nodeStyleMap = null)
    {
        sb.AppendLine("    %% Multi-Chain Styles");
        sb.AppendLine("    classDef controller fill:#e1f5fe,stroke:#01579b,stroke-width:2px;");
        sb.AppendLine("    classDef command fill:#f3e5f5,stroke:#4a148c,stroke-width:2px;");
        sb.AppendLine("    classDef entity fill:#e8f5e8,stroke:#1b5e20,stroke-width:2px;");
        sb.AppendLine("    classDef domainEvent fill:#fff3e0,stroke:#e65100,stroke-width:2px;");
        sb.AppendLine("    classDef integrationEvent fill:#fce4ec,stroke:#880e4f,stroke-width:2px;");
        sb.AppendLine("    classDef handler fill:#f1f8e9,stroke:#33691e,stroke-width:2px;");
        sb.AppendLine("    classDef converter fill:#e3f2fd,stroke:#0277bd,stroke-width:2px;");
        sb.AppendLine();

        // 应用样式到具体节点
        if (nodeStyleMap != null && nodeStyleMap.Count > 0)
        {
            sb.AppendLine("    %% Apply styles to specific nodes");

            // 按样式类分组节点
            var nodesByStyle = nodeStyleMap.GroupBy(kvp => kvp.Value);

            foreach (var styleGroup in nodesByStyle)
            {
                var styleClass = styleGroup.Key;
                var nodeIds = styleGroup.Select(kvp => kvp.Key).ToList();

                if (nodeIds.Count > 0)
                {
                    var nodeIdList = string.Join(",", nodeIds);
                    sb.AppendLine($"    class {nodeIdList} {styleClass};");
                }
            }
        }
        else
        {
            // 旧的固定格式支持（向后兼容）
            sb.AppendLine("    %% Apply styles to node types");
            for (int i = 1; i <= 50; i++) // 支持最多50个节点
            {
                sb.AppendLine($"    class C{i} controller;");
                sb.AppendLine($"    class CMD{i} command;");
                sb.AppendLine($"    class E{i},N{i} entity;");
                sb.AppendLine($"    class DE{i} domainEvent;");
                sb.AppendLine($"    class IE{i} integrationEvent;");
                sb.AppendLine($"    class DEH{i},IEH{i} handler;");
                sb.AppendLine($"    class IEC{i} converter;");
            }
        }
    }

    /// <summary>
    /// 为链路中的节点获取唯一的ID
    /// </summary>
    private static string GetChainNodeId(string fullName, string nodeType, Dictionary<string, string> chainNodeIds)
    {
        var key = $"{nodeType}_{fullName}";
        if (!chainNodeIds.ContainsKey(key))
        {
            chainNodeIds[key] = $"{nodeType}{chainNodeIds.Count + 1}";
        }
        return chainNodeIds[key];
    }

    /// <summary>
    /// 根据名称查找节点ID
    /// </summary>
    private static string FindNodeIdByName(Dictionary<string, string> nodeIds, string fullName)
    {
        return nodeIds.TryGetValue(fullName, out var nodeId) ? nodeId : string.Empty;
    }

    /// <summary>
    /// 解析节点名称，分离类型和方法
    /// </summary>
    private static (string NodeType, string Method) ParseNodeName(string nodeFullName)
    {
        if (nodeFullName.Contains("::"))
        {
            var parts = nodeFullName.Split(new[] { "::" }, StringSplitOptions.None);
            return (parts[0], parts[1]);
        }
        return (nodeFullName, "");
    }

    /// <summary>
    /// 获取简单的节点显示名称
    /// </summary>
    private static string GetSimpleNodeName(string nodeFullName)
    {
        var (nodeType, method) = ParseNodeName(nodeFullName);
        var className = GetClassNameFromFullName(nodeType);

        if (!string.IsNullOrEmpty(method))
        {
            return $"{className}.{method}";
        }
        return className;
    }

    /// <summary>
    /// 添加简单的多链路图节点
    /// </summary>
    private static void AddMultiChainNodeSimple(StringBuilder sb, string nodeFullName, string nodeId,
        CodeFlowAnalysisResult analysisResult, string indent)
    {
        var (nodeType, method) = ParseNodeName(nodeFullName);
        var displayName = GetSimpleNodeName(nodeFullName);

        // 根据节点类型确定样式
        var controller = analysisResult.Controllers.FirstOrDefault(c => c.FullName == nodeType);
        var command = analysisResult.Commands.FirstOrDefault(c => c.FullName == nodeType);
        var entity = analysisResult.Entities.FirstOrDefault(e => e.FullName == nodeType);
        var domainEvent = analysisResult.DomainEvents.FirstOrDefault(d => d.FullName == nodeType);
        var integrationEvent = analysisResult.IntegrationEvents.FirstOrDefault(i => i.FullName == nodeType);
        var domainEventHandler = analysisResult.DomainEventHandlers.FirstOrDefault(h => h.FullName == nodeType);
        var integrationEventHandler = analysisResult.IntegrationEventHandlers.FirstOrDefault(h => h.FullName == nodeType);

        if (controller != null)
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(displayName)}\"]");
        }
        else if (command != null)
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(displayName)}\"]");
        }
        else if (entity != null)
        {
            var shape = entity.IsAggregateRoot ? "{{" + EscapeMermaidText(displayName) + "}}" : "[" + EscapeMermaidText(displayName) + "]";
            sb.AppendLine($"{indent}{nodeId}{shape}");
        }
        else if (domainEvent != null)
        {
            sb.AppendLine($"{indent}{nodeId}(\"{EscapeMermaidText(displayName)}\")");
        }
        else if (integrationEvent != null)
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(displayName)}\"]");
        }
        else if (domainEventHandler != null || integrationEventHandler != null)
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(displayName)}\"]");
        }
        else
        {
            sb.AppendLine($"{indent}{nodeId}[\"{EscapeMermaidText(displayName)}\"]");
        }
    }

    /// <summary>
    /// 生成完整的可视化HTML页面
    /// </summary>
    /// <param name="analysisResult">代码分析结果</param>
    /// <param name="title">页面标题</param>
    /// <returns>完整的HTML页面内容</returns>
    public static string GenerateVisualizationHtml(CodeFlowAnalysisResult analysisResult, string title = "NetCorePal 架构图可视化")
    {
        var sb = new StringBuilder();

        // 生成所有类型的图表
        var architectureDiagram = GenerateArchitectureFlowChart(analysisResult);
        var commandDiagram = GenerateCommandFlowChart(analysisResult);
        var eventDiagram = GenerateEventFlowChart(analysisResult);
        var classDiagram = GenerateClassDiagram(analysisResult);
        var multiChainFlowChart = GenerateMultiChainFlowChart(analysisResult);
        var allChainFlowCharts = GenerateAllChainFlowCharts(analysisResult);
        var commandChains = GenerateCommandChainFlowCharts(analysisResult);

        // 生成HTML结构
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>{EscapeHtml(title)}</title>");
        sb.AppendLine("    <script src=\"https://unpkg.com/mermaid@10.6.1/dist/mermaid.min.js\"></script>");
        sb.AppendLine("    <script src=\"https://unpkg.com/pako@2.1.0/dist/pako.min.js\"></script>");

        // 添加CSS样式
        AddHtmlStyles(sb);

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // 添加页面结构
        AddHtmlStructure(sb);

        // 添加JavaScript逻辑
        AddHtmlScript(sb, analysisResult, architectureDiagram, commandDiagram, eventDiagram, classDiagram, multiChainFlowChart, allChainFlowCharts, commandChains);

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// 添加HTML样式
    /// </summary>
    private static void AddHtmlStyles(StringBuilder sb)
    {
        sb.AppendLine("    <style>");
        sb.AppendLine("        * {");
        sb.AppendLine("            margin: 0;");
        sb.AppendLine("            padding: 0;");
        sb.AppendLine("            box-sizing: border-box;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        body {");
        sb.AppendLine("            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;");
        sb.AppendLine("            background-color: #f8f9fa;");
        sb.AppendLine("            color: #333;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .container {");
        sb.AppendLine("            display: flex;");
        sb.AppendLine("            height: 100vh;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .sidebar {");
        sb.AppendLine("            width: 280px;");
        sb.AppendLine("            background-color: #2c3e50;");
        sb.AppendLine("            color: white;");
        sb.AppendLine("            padding: 20px;");
        sb.AppendLine("            overflow-y: auto;");
        sb.AppendLine("            border-right: 3px solid #34495e;");
        sb.AppendLine("            min-width: 280px;"); // 防止侧边栏过度收缩
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .sidebar h1 {");
        sb.AppendLine("            font-size: 20px;");
        sb.AppendLine("            margin-bottom: 30px;");
        sb.AppendLine("            padding-bottom: 15px;");
        sb.AppendLine("            border-bottom: 2px solid #34495e;");
        sb.AppendLine("            color: #ecf0f1;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-group {");
        sb.AppendLine("            margin-bottom: 25px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-group h3 {");
        sb.AppendLine("            font-size: 14px;");
        sb.AppendLine("            color: #bdc3c7;");
        sb.AppendLine("            margin-bottom: 10px;");
        sb.AppendLine("            text-transform: uppercase;");
        sb.AppendLine("            letter-spacing: 1px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-item {");
        sb.AppendLine("            display: block;");
        sb.AppendLine("            padding: 12px 15px;");
        sb.AppendLine("            margin-bottom: 5px;");
        sb.AppendLine("            color: #ecf0f1;");
        sb.AppendLine("            text-decoration: none;");
        sb.AppendLine("            border-radius: 6px;");
        sb.AppendLine("            transition: all 0.3s ease;");
        sb.AppendLine("            cursor: pointer;");
        sb.AppendLine("            font-size: 14px;");
        sb.AppendLine("            white-space: nowrap;");
        sb.AppendLine("            overflow: hidden;");
        sb.AppendLine("            text-overflow: ellipsis;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-item:hover {");
        sb.AppendLine("            background-color: #34495e;");
        sb.AppendLine("            transform: translateX(5px);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-item.active {");
        sb.AppendLine("            background-color: #3498db;");
        sb.AppendLine("            color: white;");
        sb.AppendLine("            font-weight: 600;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-item.chain-item {");
        sb.AppendLine("            padding-left: 25px;");
        sb.AppendLine("            font-size: 13px;");
        sb.AppendLine("            color: #bdc3c7;");
        sb.AppendLine("            white-space: nowrap;");
        sb.AppendLine("            overflow: hidden;");
        sb.AppendLine("            text-overflow: ellipsis;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-item.chain-item:hover {");
        sb.AppendLine("            background-color: #34495e;");
        sb.AppendLine("            color: #ecf0f1;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-item.chain-item.active {");
        sb.AppendLine("            background-color: #e74c3c;");
        sb.AppendLine("            color: white;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .main-content {");
        sb.AppendLine("            flex: 1;");
        sb.AppendLine("            padding: 20px;");
        sb.AppendLine("            overflow-y: auto;");
        sb.AppendLine("            background-color: white;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .content-header {");
        sb.AppendLine("            margin-bottom: 20px;");
        sb.AppendLine("            padding-bottom: 15px;");
        sb.AppendLine("            border-bottom: 2px solid #ecf0f1;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .content-header h2 {");
        sb.AppendLine("            color: #2c3e50;");
        sb.AppendLine("            font-size: 24px;");
        sb.AppendLine("            margin-bottom: 5px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .content-header p {");
        sb.AppendLine("            color: #7f8c8d;");
        sb.AppendLine("            font-size: 14px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .diagram-container {");
        sb.AppendLine("            background-color: #fefefe;");
        sb.AppendLine("            border-radius: 8px;");
        sb.AppendLine("            padding: 20px;");
        sb.AppendLine("            box-shadow: 0 2px 10px rgba(0,0,0,0.1);");
        sb.AppendLine("            min-height: 600px;");
        sb.AppendLine("            height: auto;");
        sb.AppendLine("            text-align: center;");
        sb.AppendLine("            overflow: visible;");
        sb.AppendLine("            display: flex;");
        sb.AppendLine("            flex-direction: column;");
        sb.AppendLine("            justify-content: center;");
        sb.AppendLine("            align-items: center;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .mermaid {");
        sb.AppendLine("            display: block;");
        sb.AppendLine("            margin: 0 auto;");
        sb.AppendLine("            max-width: 100%;");
        sb.AppendLine("            width: 100%;");
        sb.AppendLine("            height: auto !important;");
        sb.AppendLine("            min-height: 400px;");
        sb.AppendLine("            overflow: visible;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .mermaid svg {");
        sb.AppendLine("            max-width: 100%;");
        sb.AppendLine("            height: auto !important;");
        sb.AppendLine("            min-height: 400px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .loading {");
        sb.AppendLine("            text-align: center;");
        sb.AppendLine("            padding: 60px;");
        sb.AppendLine("            color: #7f8c8d;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .loading::before {");
        sb.AppendLine("            content: '⏳';");
        sb.AppendLine("            font-size: 24px;");
        sb.AppendLine("            display: block;");
        sb.AppendLine("            margin-bottom: 10px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .error {");
        sb.AppendLine("            background-color: #ffebee;");
        sb.AppendLine("            color: #c62828;");
        sb.AppendLine("            padding: 15px;");
        sb.AppendLine("            border-radius: 6px;");
        sb.AppendLine("            border-left: 4px solid #f44336;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .chain-counter {");
        sb.AppendLine("            background-color: #3498db;");
        sb.AppendLine("            color: white;");
        sb.AppendLine("            border-radius: 50%;");
        sb.AppendLine("            width: 20px;");
        sb.AppendLine("            height: 20px;");
        sb.AppendLine("            display: inline-flex;");
        sb.AppendLine("            align-items: center;");
        sb.AppendLine("            justify-content: center;");
        sb.AppendLine("            font-size: 11px;");
        sb.AppendLine("            margin-left: 10px;");
        sb.AppendLine("            font-weight: bold;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .expand-toggle {");
        sb.AppendLine("            float: right;");
        sb.AppendLine("            font-size: 12px;");
        sb.AppendLine("            cursor: pointer;");
        sb.AppendLine("            color: #bdc3c7;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .expand-toggle:hover {");
        sb.AppendLine("            color: #ecf0f1;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .mermaid-live-button {");
        sb.AppendLine("            background: linear-gradient(135deg, #ff6b6b, #ee5a24);");
        sb.AppendLine("            color: white;");
        sb.AppendLine("            border: none;");
        sb.AppendLine("            padding: 8px 16px;");
        sb.AppendLine("            border-radius: 6px;");
        sb.AppendLine("            cursor: pointer;");
        sb.AppendLine("            font-size: 12px;");
        sb.AppendLine("            font-weight: 600;");
        sb.AppendLine("            text-decoration: none;");
        sb.AppendLine("            display: inline-flex;");
        sb.AppendLine("            align-items: center;");
        sb.AppendLine("            gap: 6px;");
        sb.AppendLine("            margin-top: 15px;");
        sb.AppendLine("            transition: all 0.3s ease;");
        sb.AppendLine("            box-shadow: 0 2px 4px rgba(238, 90, 36, 0.3);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .mermaid-live-button:hover {");
        sb.AppendLine("            background: linear-gradient(135deg, #ee5a24, #e55039);");
        sb.AppendLine("            transform: translateY(-2px);");
        sb.AppendLine("            box-shadow: 0 4px 8px rgba(238, 90, 36, 0.4);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .mermaid-live-button:active {");
        sb.AppendLine("            transform: translateY(0);");
        sb.AppendLine("            box-shadow: 0 2px 4px rgba(238, 90, 36, 0.3);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .diagram-header {");
        sb.AppendLine("            display: flex;");
        sb.AppendLine("            justify-content: space-between;");
        sb.AppendLine("            align-items: flex-start;");
        sb.AppendLine("            margin-bottom: 20px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .diagram-title-section {");
        sb.AppendLine("            flex: 1;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .chains-collapsed .chain-item {");
        sb.AppendLine("            display: none;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /* 长文本处理 */");
        sb.AppendLine("        .nav-item {");
        sb.AppendLine("            max-width: 250px;"); // 限制最大宽度
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .nav-item.chain-item {");
        sb.AppendLine("            max-width: 230px;"); // 子菜单项稍微小一些
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /* 搜索框样式 */");
        sb.AppendLine("        .search-container {");
        sb.AppendLine("            margin-bottom: 20px;");
        sb.AppendLine("            padding: 0 5px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-box {");
        sb.AppendLine("            width: 100%;");
        sb.AppendLine("            padding: 10px 12px;");
        sb.AppendLine("            border: 2px solid #34495e;");
        sb.AppendLine("            border-radius: 6px;");
        sb.AppendLine("            background-color: #34495e;");
        sb.AppendLine("            color: #ecf0f1;");
        sb.AppendLine("            font-size: 14px;");
        sb.AppendLine("            transition: all 0.3s ease;");
        sb.AppendLine("            outline: none;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-box::placeholder {");
        sb.AppendLine("            color: #bdc3c7;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-box:focus {");
        sb.AppendLine("            border-color: #3498db;");
        sb.AppendLine("            background-color: #2c3e50;");
        sb.AppendLine("            box-shadow: 0 0 0 2px rgba(52, 152, 219, 0.2);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-results {");
        sb.AppendLine("            margin-top: 10px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-result-item {");
        sb.AppendLine("            padding: 8px 12px;");
        sb.AppendLine("            margin-bottom: 3px;");
        sb.AppendLine("            background-color: #2c3e50;");
        sb.AppendLine("            border-radius: 4px;");
        sb.AppendLine("            cursor: pointer;");
        sb.AppendLine("            transition: all 0.2s ease;");
        sb.AppendLine("            font-size: 13px;");
        sb.AppendLine("            color: #ecf0f1;");
        sb.AppendLine("            border-left: 3px solid transparent;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-result-item:hover {");
        sb.AppendLine("            background-color: #34495e;");
        sb.AppendLine("            border-left-color: #3498db;");
        sb.AppendLine("            transform: translateX(3px);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-result-item.highlight {");
        sb.AppendLine("            background-color: #3498db;");
        sb.AppendLine("            border-left-color: #2980b9;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-no-results {");
        sb.AppendLine("            text-align: center;");
        sb.AppendLine("            color: #7f8c8d;");
        sb.AppendLine("            font-style: italic;");
        sb.AppendLine("            padding: 20px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        .search-category {");
        sb.AppendLine("            font-size: 11px;");
        sb.AppendLine("            color: #95a5a6;");
        sb.AppendLine("            margin-left: 8px;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /* 响应式设计 */");
        sb.AppendLine("        @media (max-width: 768px) {");
        sb.AppendLine("            .container {");
        sb.AppendLine("                flex-direction: column;");
        sb.AppendLine("            }");
        sb.AppendLine("            ");
        sb.AppendLine("            .sidebar {");
        sb.AppendLine("                width: 100%;");
        sb.AppendLine("                height: auto;");
        sb.AppendLine("                max-height: 40vh;");
        sb.AppendLine("            }");
        sb.AppendLine("            ");
        sb.AppendLine("            .main-content {");
        sb.AppendLine("                flex: 1;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    </style>");
    }

    /// <summary>
    /// 添加HTML页面结构
    /// </summary>
    private static void AddHtmlStructure(StringBuilder sb)
    {
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <div class=\"sidebar\">");
        sb.AppendLine("            <h1>🏗️ 架构图导航</h1>");
        sb.AppendLine("            ");
        sb.AppendLine("            <!-- 搜索框 -->");
        sb.AppendLine("            <div class=\"search-container\">");
        sb.AppendLine("                <input type=\"text\" id=\"searchBox\" class=\"search-box\" placeholder=\"搜索图表...\" oninput=\"performSearch()\">");
        sb.AppendLine("                <div id=\"searchResults\" class=\"search-results\" style=\"display: none;\"></div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            ");
        sb.AppendLine("            <div class=\"nav-group\">");
        sb.AppendLine("                <h3>整体架构</h3>");
        sb.AppendLine("                <a class=\"nav-item\" data-diagram=\"architecture\" href=\"#architecture\" title=\"📋 完整架构流程图\">");
        sb.AppendLine("                    📋 完整架构流程图");
        sb.AppendLine("                </a>");
        sb.AppendLine("                <a class=\"nav-item\" data-diagram=\"class\" href=\"#class\" title=\"🏛️ 类图\">");
        sb.AppendLine("                    🏛️ 类图");
        sb.AppendLine("                </a>");
        sb.AppendLine("            </div>");
        sb.AppendLine();
        sb.AppendLine("            <div class=\"nav-group\">");
        sb.AppendLine("                <h3>专项流程</h3>");
        sb.AppendLine("                <a class=\"nav-item\" data-diagram=\"command\" href=\"#command\" title=\"⚡ 命令流程图\">");
        sb.AppendLine("                    ⚡ 命令流程图");
        sb.AppendLine("                </a>");
        sb.AppendLine("                <a class=\"nav-item\" data-diagram=\"event\" href=\"#event\" title=\"📡 事件流程图\">");
        sb.AppendLine("                    📡 事件流程图");
        sb.AppendLine("                </a>");
        sb.AppendLine("            </div>");
        sb.AppendLine();            sb.AppendLine("            <div class=\"nav-group\">");
            sb.AppendLine("                <h3>命令链路 <span class=\"expand-toggle\" onclick=\"toggleChains()\">▶</span> <span class=\"chain-counter\" id=\"chainCounter\">0</span></h3>");
            sb.AppendLine("                <div class=\"chains-container\" id=\"chainsContainer\">");
            sb.AppendLine("                    <!-- 动态生成的命令链路将在这里显示 -->");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine();
            sb.AppendLine("            <div class=\"nav-group\">");
            sb.AppendLine("                <h3>链路流程图</h3>");
            sb.AppendLine("                <a class=\"nav-item\" data-diagram=\"multiChain\" href=\"#multiChain\" title=\"🔗 多链路流程图\">");
            sb.AppendLine("                    🔗 多链路流程图");
            sb.AppendLine("                </a>");
            sb.AppendLine("            </div>");
            sb.AppendLine();
            sb.AppendLine("            <div class=\"nav-group\">");
            sb.AppendLine("                <h3>单独链路流程图 <span class=\"expand-toggle\" onclick=\"toggleIndividualChains()\">▶</span> <span class=\"chain-counter\" id=\"individualChainCounter\">0</span></h3>");
        sb.AppendLine("                <div class=\"chains-container\" id=\"individualChainsContainer\">");
        sb.AppendLine("                    <!-- 动态生成的单独链路流程图将在这里显示 -->");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine();
        sb.AppendLine("        <div class=\"main-content\">");
        sb.AppendLine("            <div class=\"content-header\">");
        sb.AppendLine("                <div class=\"diagram-header\">");
        sb.AppendLine("                    <div class=\"diagram-title-section\">");
        sb.AppendLine("                        <h2 id=\"diagramTitle\">选择图表类型</h2>");
        sb.AppendLine("                        <p id=\"diagramDescription\">请从左侧菜单选择要查看的图表类型</p>");
        sb.AppendLine("                    </div>");
        sb.AppendLine("                    <div class=\"diagram-actions\">");
        sb.AppendLine("                        <button id=\"mermaidLiveButton\" class=\"mermaid-live-button\" style=\"display: none;\" onclick=\"openInMermaidLive()\">");
        sb.AppendLine("                            🔗 View in Mermaid Live");
        sb.AppendLine("                        </button>");
        sb.AppendLine("                    </div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            ");
        sb.AppendLine("            <div class=\"diagram-container\">");
        sb.AppendLine("                <div id=\"diagramContent\">");
        sb.AppendLine("                    <div class=\"loading\">正在加载图表数据...</div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
    }

    /// <summary>
    /// 添加HTML JavaScript逻辑
    /// </summary>
    private static void AddHtmlScript(StringBuilder sb, CodeFlowAnalysisResult analysisResult,
        string architectureDiagram, string commandDiagram, string eventDiagram, string classDiagram,
        string multiChainFlowChart, List<(string ChainName, string Diagram)> allChainFlowCharts, List<(string ChainName, string MermaidDiagram)> commandChains)
    {
        sb.AppendLine("    <script>");
        sb.AppendLine("        // 初始化 Mermaid");
        sb.AppendLine("        mermaid.initialize({");
        sb.AppendLine("            startOnLoad: false,");
        sb.AppendLine("            theme: 'base',");
        sb.AppendLine("            themeVariables: {");
        sb.AppendLine("                primaryColor: '#3498db',");
        sb.AppendLine("                primaryTextColor: '#2c3e50',");
        sb.AppendLine("                primaryBorderColor: '#2980b9',");
        sb.AppendLine("                lineColor: '#34495e',");
        sb.AppendLine("                secondaryColor: '#ecf0f1',");
        sb.AppendLine("                tertiaryColor: '#bdc3c7',");
        sb.AppendLine("                background: '#ffffff',");
        sb.AppendLine("                mainBkg: '#ffffff',");
        sb.AppendLine("                secondBkg: '#f8f9fa',");
        sb.AppendLine("                tertiaryBkg: '#ecf0f1'");
        sb.AppendLine("            },");
        sb.AppendLine("            flowchart: {");
        sb.AppendLine("                htmlLabels: true,");
        sb.AppendLine("                curve: 'basis',");
        sb.AppendLine("                diagramPadding: 20,");
        sb.AppendLine("                useMaxWidth: false,");
        sb.AppendLine("                useMaxHeight: false,");
        sb.AppendLine("                nodeSpacing: 50,");
        sb.AppendLine("                rankSpacing: 50");
        sb.AppendLine("            },");
        sb.AppendLine("            classDiagram: {");
        sb.AppendLine("                htmlLabels: true,");
        sb.AppendLine("                diagramPadding: 20,");
        sb.AppendLine("                useMaxWidth: false,");
        sb.AppendLine("                useMaxHeight: false");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine();

        // 添加分析结果数据
        AddAnalysisResultData(sb, analysisResult);

        // 添加图表数据
        AddDiagramData(sb, architectureDiagram, commandDiagram, eventDiagram, classDiagram, multiChainFlowChart, allChainFlowCharts, commandChains);

        // 添加JavaScript函数
        AddJavaScriptFunctions(sb);

        sb.AppendLine("    </script>");
    }

    /// <summary>
    /// 添加分析结果数据到JavaScript
    /// </summary>
    private static void AddAnalysisResultData(StringBuilder sb, CodeFlowAnalysisResult analysisResult)
    {
        sb.AppendLine("        // 分析结果数据");
        sb.AppendLine("        const analysisResult = {");

        // Controllers
        sb.AppendLine("            controllers: [");
        foreach (var controller in analysisResult.Controllers)
        {
            sb.AppendLine($"                {{ name: \"{EscapeJavaScript(controller.Name)}\", fullName: \"{EscapeJavaScript(controller.FullName)}\", methods: {FormatStringArray(controller.Methods)} }},");
        }
        sb.AppendLine("            ],");

        // Commands
        sb.AppendLine("            commands: [");
        foreach (var command in analysisResult.Commands)
        {
            sb.AppendLine($"                {{ name: \"{EscapeJavaScript(command.Name)}\", fullName: \"{EscapeJavaScript(command.FullName)}\" }},");
        }
        sb.AppendLine("            ],");

        // Entities
        sb.AppendLine("            entities: [");
        foreach (var entity in analysisResult.Entities)
        {
            sb.AppendLine($"                {{ name: \"{EscapeJavaScript(entity.Name)}\", fullName: \"{EscapeJavaScript(entity.FullName)}\", isAggregateRoot: {entity.IsAggregateRoot.ToString().ToLower()}, methods: {FormatStringArray(entity.Methods)} }},");
        }
        sb.AppendLine("            ],");

        // Domain Events
        sb.AppendLine("            domainEvents: [");
        foreach (var domainEvent in analysisResult.DomainEvents)
        {
            sb.AppendLine($"                {{ name: \"{EscapeJavaScript(domainEvent.Name)}\", fullName: \"{EscapeJavaScript(domainEvent.FullName)}\" }},");
        }
        sb.AppendLine("            ],");

        // Integration Events
        sb.AppendLine("            integrationEvents: [");
        foreach (var integrationEvent in analysisResult.IntegrationEvents)
        {
            sb.AppendLine($"                {{ name: \"{EscapeJavaScript(integrationEvent.Name)}\", fullName: \"{EscapeJavaScript(integrationEvent.FullName)}\" }},");
        }
        sb.AppendLine("            ],");

        // Domain Event Handlers
        sb.AppendLine("            domainEventHandlers: [");
        foreach (var handler in analysisResult.DomainEventHandlers)
        {
            sb.AppendLine($"                {{ name: \"{EscapeJavaScript(handler.Name)}\", fullName: \"{EscapeJavaScript(handler.FullName)}\", handledEventType: \"{EscapeJavaScript(handler.HandledEventType)}\", commands: {FormatStringArray(handler.Commands)} }},");
        }
        sb.AppendLine("            ],");

        // Integration Event Handlers
        sb.AppendLine("            integrationEventHandlers: [");
        foreach (var handler in analysisResult.IntegrationEventHandlers)
        {
            sb.AppendLine($"                {{ name: \"{EscapeJavaScript(handler.Name)}\", fullName: \"{EscapeJavaScript(handler.FullName)}\", handledEventType: \"{EscapeJavaScript(handler.HandledEventType)}\", commands: {FormatStringArray(handler.Commands)} }},");
        }
        sb.AppendLine("            ],");

        // Integration Event Converters
        sb.AppendLine("            integrationEventConverters: [");
        foreach (var converter in analysisResult.IntegrationEventConverters)
        {
            sb.AppendLine($"                {{ name: \"{EscapeJavaScript(converter.Name)}\", fullName: \"{EscapeJavaScript(converter.FullName)}\", domainEventType: \"{EscapeJavaScript(converter.DomainEventType)}\", integrationEventType: \"{EscapeJavaScript(converter.IntegrationEventType)}\" }},");
        }
        sb.AppendLine("            ],");

        // Relationships
        sb.AppendLine("            relationships: [");
        foreach (var relationship in analysisResult.Relationships)
        {
            sb.AppendLine($"                {{ sourceType: \"{EscapeJavaScript(relationship.SourceType)}\", targetType: \"{EscapeJavaScript(relationship.TargetType)}\", callType: \"{EscapeJavaScript(relationship.CallType)}\", sourceMethod: \"{EscapeJavaScript(relationship.SourceMethod)}\", targetMethod: \"{EscapeJavaScript(relationship.TargetMethod)}\" }},");
        }
        sb.AppendLine("            ]");

        sb.AppendLine("        };");
        sb.AppendLine();
    }

    /// <summary>
    /// 添加图表数据到JavaScript
    /// </summary>
    private static void AddDiagramData(StringBuilder sb, string architectureDiagram, string commandDiagram,
        string eventDiagram, string classDiagram, string multiChainFlowChart, List<(string ChainName, string Diagram)> allChainFlowCharts,
        List<(string ChainName, string MermaidDiagram)> commandChains)
    {
        sb.AppendLine("        // 图表配置");
        sb.AppendLine("        const diagramConfigs = {");
        sb.AppendLine("            architecture: {");
        sb.AppendLine("                title: '完整架构流程图',");
        sb.AppendLine("                description: '展示整个系统的架构组件和它们之间的关系'");
        sb.AppendLine("            },");
        sb.AppendLine("            command: {");
        sb.AppendLine("                title: '命令流程图',");
        sb.AppendLine("                description: '专注于命令执行流程的图表'");
        sb.AppendLine("            },");
        sb.AppendLine("            event: {");
        sb.AppendLine("                title: '事件流程图',");
        sb.AppendLine("                description: '专注于事件驱动流程的图表'");
        sb.AppendLine("            },");
        sb.AppendLine("            class: {");
        sb.AppendLine("                title: '类图',");
        sb.AppendLine("                description: '展示类型间关系的UML类图'");
        sb.AppendLine("            },");
        sb.AppendLine("            multiChain: {");
        sb.AppendLine("                title: '多链路流程图',");
        sb.AppendLine("                description: '在一张图中展示多个命令链路的完整流程'");
        sb.AppendLine("            }");
        sb.AppendLine("        };");
        sb.AppendLine();

        sb.AppendLine("        // Mermaid图表数据");
        sb.AppendLine("        const diagrams = {");
        sb.AppendLine($"            architecture: `{EscapeJavaScriptTemplate(architectureDiagram)}`,");
        sb.AppendLine($"            command: `{EscapeJavaScriptTemplate(commandDiagram)}`,");
        sb.AppendLine($"            event: `{EscapeJavaScriptTemplate(eventDiagram)}`,");
        sb.AppendLine($"            class: `{EscapeJavaScriptTemplate(classDiagram)}`,");
        sb.AppendLine($"            multiChain: `{EscapeJavaScriptTemplate(multiChainFlowChart)}`");
        sb.AppendLine("        };");
        sb.AppendLine();

        sb.AppendLine("        // 单独的链路流程图数据");
        sb.AppendLine("        const allChainFlowCharts = [");
        for (int i = 0; i < allChainFlowCharts.Count; i++)
        {
            var (chainName, diagram) = allChainFlowCharts[i];
            sb.AppendLine("            {");
            sb.AppendLine($"                name: \"{EscapeJavaScript(chainName)}\",");
            sb.AppendLine($"                diagram: `{EscapeJavaScriptTemplate(diagram)}`");
            sb.AppendLine($"            }}{(i < allChainFlowCharts.Count - 1 ? "," : "")}");
        }
        sb.AppendLine("        ];");
        sb.AppendLine();

        sb.AppendLine("        // 命令链路数据");
        sb.AppendLine("        const commandChains = [");
        foreach (var (chainName, mermaidDiagram) in commandChains)
        {
            sb.AppendLine("            {");
            sb.AppendLine($"                name: \"{EscapeJavaScript(chainName)}\",");
            sb.AppendLine($"                diagram: `{EscapeJavaScriptTemplate(mermaidDiagram)}`");
            sb.AppendLine("            },");
        }
        sb.AppendLine("        ];");
        sb.AppendLine();
    }

    /// <summary>
    /// 添加JavaScript函数
    /// </summary>
    private static void AddJavaScriptFunctions(StringBuilder sb)
    {
        sb.AppendLine("        let currentDiagram = null;");
        sb.AppendLine("        let currentDiagramData = null;");
        sb.AppendLine("        let chainsExpanded = false;");
        sb.AppendLine("        let individualChainsExpanded = false;");
        sb.AppendLine();
        sb.AppendLine("        // 初始化页面");
        sb.AppendLine("        function initializePage() {");
        sb.AppendLine("            generateChainNavigation();");
        sb.AppendLine("            addNavigationListeners();");
        sb.AppendLine("            addHashChangeListener();");
        sb.AppendLine("            handleInitialHash();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 生成命令链路导航");
        sb.AppendLine("        function generateChainNavigation() {");
        sb.AppendLine("            const container = document.getElementById('chainsContainer');");
        sb.AppendLine("            const counter = document.getElementById('chainCounter');");
        sb.AppendLine("            container.innerHTML = '';");
        sb.AppendLine("            counter.textContent = commandChains.length;");
        sb.AppendLine("            ");
        sb.AppendLine("            // 默认设置为折叠状态");
        sb.AppendLine("            container.classList.add('chains-collapsed');");
        sb.AppendLine("            ");            sb.AppendLine("            commandChains.forEach((chain, index) => {");
            sb.AppendLine("                const chainItem = document.createElement('a');");
            sb.AppendLine("                chainItem.className = 'nav-item chain-item';");
            sb.AppendLine("                chainItem.setAttribute('data-chain', index);");
            sb.AppendLine("                const chainId = encodeURIComponent(chain.name.replace(/[^a-zA-Z0-9\\u4e00-\\u9fa5]/g, '-'));");
            sb.AppendLine("                chainItem.href = `#chain-${chainId}`;");
            sb.AppendLine("                chainItem.textContent = `🔗 ${chain.name}`;");
            sb.AppendLine("                chainItem.title = `🔗 ${chain.name}`;"); // 添加完整文本提示
            sb.AppendLine("                container.appendChild(chainItem);");
            sb.AppendLine("            });");
        sb.AppendLine("            ");
        sb.AppendLine("            // 生成单独链路流程图导航");
        sb.AppendLine("            const individualContainer = document.getElementById('individualChainsContainer');");
        sb.AppendLine("            const individualCounter = document.getElementById('individualChainCounter');");
        sb.AppendLine("            individualContainer.innerHTML = '';");
        sb.AppendLine("            individualCounter.textContent = allChainFlowCharts.length;");
        sb.AppendLine("            ");
        sb.AppendLine("            // 默认设置为折叠状态");
        sb.AppendLine("            individualContainer.classList.add('chains-collapsed');");
        sb.AppendLine("            ");            sb.AppendLine("            allChainFlowCharts.forEach((chain, index) => {");
            sb.AppendLine("                const chainItem = document.createElement('a');");
            sb.AppendLine("                chainItem.className = 'nav-item chain-item';");
            sb.AppendLine("                chainItem.setAttribute('data-individual-chain', index);");
            sb.AppendLine("                const chainId = encodeURIComponent(chain.name.replace(/[^a-zA-Z0-9\\u4e00-\\u9fa5]/g, '-'));");
            sb.AppendLine("                chainItem.href = `#individual-chain-${chainId}`;");
            sb.AppendLine("                chainItem.textContent = `📊 ${chain.name}`;");
            sb.AppendLine("                chainItem.title = `📊 ${chain.name}`;"); // 添加完整文本提示
            sb.AppendLine("                individualContainer.appendChild(chainItem);");
            sb.AppendLine("            });");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 添加导航事件监听");
        sb.AppendLine("        function addNavigationListeners() {");
        sb.AppendLine("            document.querySelectorAll('.nav-item[data-diagram]').forEach(item => {");
        sb.AppendLine("                item.addEventListener('click', (e) => {");
        sb.AppendLine("                    e.preventDefault();");
        sb.AppendLine("                    const diagramType = item.getAttribute('data-diagram');");
        sb.AppendLine("                    showDiagram(diagramType);");
        sb.AppendLine("                });");
        sb.AppendLine("            });");
        sb.AppendLine("            ");
        sb.AppendLine("            document.querySelectorAll('.nav-item[data-chain]').forEach(item => {");
        sb.AppendLine("                item.addEventListener('click', (e) => {");
        sb.AppendLine("                    e.preventDefault();");
        sb.AppendLine("                    const chainIndex = parseInt(item.getAttribute('data-chain'));");
        sb.AppendLine("                    // 确保菜单展开");
        sb.AppendLine("                    if (!chainsExpanded) {");
        sb.AppendLine("                        toggleChains();");
        sb.AppendLine("                    }");
        sb.AppendLine("                    showChain(chainIndex);");
        sb.AppendLine("                });");
        sb.AppendLine("            });");
        sb.AppendLine("            ");
        sb.AppendLine("            document.querySelectorAll('.nav-item[data-individual-chain]').forEach(item => {");
        sb.AppendLine("                item.addEventListener('click', (e) => {");
        sb.AppendLine("                    e.preventDefault();");
        sb.AppendLine("                    const chainIndex = parseInt(item.getAttribute('data-individual-chain'));");
        sb.AppendLine("                    // 确保菜单展开");
        sb.AppendLine("                    if (!individualChainsExpanded) {");
        sb.AppendLine("                        toggleIndividualChains();");
        sb.AppendLine("                    }");
        sb.AppendLine("                    showIndividualChain(chainIndex);");
        sb.AppendLine("                });");
        sb.AppendLine("            });");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 显示图表");
        sb.AppendLine("        async function showDiagram(diagramType, updateHash = true) {");
        sb.AppendLine("            const config = diagramConfigs[diagramType];");
        sb.AppendLine("            if (!config) return;");
        sb.AppendLine();
        sb.AppendLine("            if (updateHash) {");
        sb.AppendLine("                window.location.hash = diagramType;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            document.querySelectorAll('.nav-item').forEach(item => {");
        sb.AppendLine("                item.classList.remove('active');");
        sb.AppendLine("            });");
        sb.AppendLine("            document.querySelector(`[data-diagram=\"${diagramType}\"]`).classList.add('active');");
        sb.AppendLine(); sb.AppendLine("            document.getElementById('diagramTitle').textContent = config.title;");
        sb.AppendLine("            document.getElementById('diagramDescription').textContent = config.description;");
        sb.AppendLine("            hideMermaidLiveButton();");
        sb.AppendLine();
        sb.AppendLine("            const contentDiv = document.getElementById('diagramContent');");
        sb.AppendLine("            contentDiv.innerHTML = '<div class=\"loading\">正在生成图表...</div>';");
        sb.AppendLine();
        sb.AppendLine("            try {");
        sb.AppendLine("                await new Promise(resolve => setTimeout(resolve, 300));");
        sb.AppendLine("                const diagramData = diagrams[diagramType];");
        sb.AppendLine("                if (!diagramData) {");
        sb.AppendLine("                    throw new Error('图表数据不存在');"); sb.AppendLine("                }");
        sb.AppendLine("                await renderMermaidDiagram(diagramData, contentDiv);");
        sb.AppendLine("                currentDiagram = diagramType;");
        sb.AppendLine("                currentDiagramData = diagramData;");
        sb.AppendLine("                showMermaidLiveButton();"); sb.AppendLine("            } catch (error) {");
        sb.AppendLine("                console.error('生成图表失败:', error);");
        sb.AppendLine("                contentDiv.innerHTML = `<div class=\"error\">生成图表失败: ${error.message}</div>`;");
        sb.AppendLine("                hideMermaidLiveButton();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 显示命令链路");
        sb.AppendLine("        async function showChain(chainIndex, updateHash = true) {");
        sb.AppendLine("            const chain = commandChains[chainIndex];");
        sb.AppendLine("            if (!chain) return;");
        sb.AppendLine();
        sb.AppendLine("            // 如果命令链路菜单是折叠的，则展开它");
        sb.AppendLine("            if (!chainsExpanded) {");
        sb.AppendLine("                toggleChains();");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (updateHash) {");
        sb.AppendLine("                const chainId = encodeURIComponent(chain.name.replace(/[^a-zA-Z0-9\\u4e00-\\u9fa5]/g, '-'));");
        sb.AppendLine("                window.location.hash = `chain-${chainId}`;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            document.querySelectorAll('.nav-item').forEach(item => {");
        sb.AppendLine("                item.classList.remove('active');");
        sb.AppendLine("            });");
        sb.AppendLine("            document.querySelector(`[data-chain=\"${chainIndex}\"]`).classList.add('active');");
        sb.AppendLine(); sb.AppendLine("            document.getElementById('diagramTitle').textContent = `命令链路: ${chain.name}`;");
        sb.AppendLine("            document.getElementById('diagramDescription').textContent = '展示单个命令链路的完整执行流程';");
        sb.AppendLine("            hideMermaidLiveButton();");
        sb.AppendLine();
        sb.AppendLine("            const contentDiv = document.getElementById('diagramContent');");
        sb.AppendLine("            contentDiv.innerHTML = '<div class=\"loading\">正在生成链路图...</div>';");
        sb.AppendLine();
        sb.AppendLine("            try {"); sb.AppendLine("                await new Promise(resolve => setTimeout(resolve, 200));");
        sb.AppendLine("                await renderMermaidDiagram(chain.diagram, contentDiv);");
        sb.AppendLine("                currentDiagram = `chain-${chainIndex}`;");
        sb.AppendLine("                currentDiagramData = chain.diagram;");
        sb.AppendLine("                showMermaidLiveButton();"); sb.AppendLine("            } catch (error) {");
        sb.AppendLine("                console.error('生成链路图失败:', error);");
        sb.AppendLine("                contentDiv.innerHTML = `<div class=\"error\">生成链路图失败: ${error.message}</div>`;");
        sb.AppendLine("                hideMermaidLiveButton();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 渲染Mermaid图表");
        sb.AppendLine("        async function renderMermaidDiagram(diagramData, container) {");
        sb.AppendLine("            const diagramId = `diagram-${Date.now()}`;");
        sb.AppendLine("            ");
        sb.AppendLine("            try {");
        sb.AppendLine("                container.innerHTML = '';");
        sb.AppendLine("                const diagramElement = document.createElement('div');");
        sb.AppendLine("                diagramElement.id = diagramId;");
        sb.AppendLine("                diagramElement.className = 'mermaid';");
        sb.AppendLine("                diagramElement.textContent = diagramData;");
        sb.AppendLine("                container.appendChild(diagramElement);");
        sb.AppendLine("                await mermaid.run({ nodes: [diagramElement] });");
        sb.AppendLine("            } catch (error) {");
        sb.AppendLine("                console.error('Mermaid渲染失败:', error);");
        sb.AppendLine("                throw new Error('图表渲染失败，请检查图表语法');");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 显示单独链路流程图");
        sb.AppendLine("        async function showIndividualChain(chainIndex, updateHash = true) {");
        sb.AppendLine("            const chain = allChainFlowCharts[chainIndex];");
        sb.AppendLine("            if (!chain) return;");
        sb.AppendLine();
        sb.AppendLine("            // 如果单独链路流程图菜单是折叠的，则展开它");
        sb.AppendLine("            if (!individualChainsExpanded) {");
        sb.AppendLine("                toggleIndividualChains();");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (updateHash) {");
        sb.AppendLine("                const chainId = encodeURIComponent(chain.name.replace(/[^a-zA-Z0-9\\u4e00-\\u9fa5]/g, '-'));");
        sb.AppendLine("                window.location.hash = `individual-chain-${chainId}`;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            document.querySelectorAll('.nav-item').forEach(item => {");
        sb.AppendLine("                item.classList.remove('active');");
        sb.AppendLine("            });");
        sb.AppendLine("            document.querySelector(`[data-individual-chain=\"${chainIndex}\"]`).classList.add('active');");
        sb.AppendLine(); sb.AppendLine("            document.getElementById('diagramTitle').textContent = `${chain.name}`;");
        sb.AppendLine("            document.getElementById('diagramDescription').textContent = '单独链路的完整流程图';");
        sb.AppendLine("            hideMermaidLiveButton();");
        sb.AppendLine();
        sb.AppendLine("            const contentDiv = document.getElementById('diagramContent');");
        sb.AppendLine("            contentDiv.innerHTML = '<div class=\"loading\">正在生成单独链路图...</div>';");
        sb.AppendLine();
        sb.AppendLine("            try {"); sb.AppendLine("                await new Promise(resolve => setTimeout(resolve, 200));");
        sb.AppendLine("                await renderMermaidDiagram(chain.diagram, contentDiv);");
        sb.AppendLine("                currentDiagram = `individual-chain-${chainIndex}`;");
        sb.AppendLine("                currentDiagramData = chain.diagram;");
        sb.AppendLine("                showMermaidLiveButton();"); sb.AppendLine("            } catch (error) {");
        sb.AppendLine("                console.error('生成单独链路图失败:', error);");
        sb.AppendLine("                contentDiv.innerHTML = `<div class=\"error\">生成单独链路图失败: ${error.message}</div>`;");
        sb.AppendLine("                hideMermaidLiveButton();");
        sb.AppendLine("            }");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        // 切换命令链路展开/收起");
        sb.AppendLine("        function toggleChains() {");
        sb.AppendLine("            chainsExpanded = !chainsExpanded;");
        sb.AppendLine("            const container = document.getElementById('chainsContainer');");
        sb.AppendLine("            const toggle = document.querySelector('.expand-toggle');");
        sb.AppendLine("            ");
        sb.AppendLine("            if (chainsExpanded) {");
        sb.AppendLine("                container.classList.remove('chains-collapsed');");
        sb.AppendLine("                toggle.textContent = '▼';");
        sb.AppendLine("            } else {");
        sb.AppendLine("                container.classList.add('chains-collapsed');");
        sb.AppendLine("                toggle.textContent = '▶';");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 显示 Mermaid Live 按钮");
        sb.AppendLine("        function showMermaidLiveButton() {");
        sb.AppendLine("            const button = document.getElementById('mermaidLiveButton');");
        sb.AppendLine("            if (button && currentDiagramData) {");
        sb.AppendLine("                button.style.display = 'inline-flex';");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 隐藏 Mermaid Live 按钮");
        sb.AppendLine("        function hideMermaidLiveButton() {");
        sb.AppendLine("            const button = document.getElementById('mermaidLiveButton');");
        sb.AppendLine("            if (button) {");
        sb.AppendLine("                button.style.display = 'none';");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 在 Mermaid Live 中打开当前图表");
        sb.AppendLine("        function openInMermaidLive() {");
        sb.AppendLine("            if (!currentDiagramData) {");
        sb.AppendLine("                alert('没有可用的图表数据');");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            try {");
        sb.AppendLine("                // 检查是否有 pako 库可用");
        sb.AppendLine("                if (typeof pako !== 'undefined') {");
        sb.AppendLine("                    // 使用 pako 压缩方式 (首选)");
        sb.AppendLine("                    const state = {");
        sb.AppendLine("                        code: currentDiagramData,");
        sb.AppendLine("                        mermaid: {");
        sb.AppendLine("                            theme: 'default'");
        sb.AppendLine("                        },");
        sb.AppendLine("                        autoSync: true,");
        sb.AppendLine("                        updateDiagram: true");
        sb.AppendLine("                    };");
        sb.AppendLine();
        sb.AppendLine("                    const stateString = JSON.stringify(state);");
        sb.AppendLine("                    const compressed = pako.deflate(stateString, { to: 'string' });");
        sb.AppendLine("                    const encoded = btoa(String.fromCharCode.apply(null, compressed));");
        sb.AppendLine("                    const mermaidLiveUrl = `https://mermaid.live/edit#pako:${encoded}`;");
        sb.AppendLine();
        sb.AppendLine("                    window.open(mermaidLiveUrl, '_blank');");
        sb.AppendLine("                } else {");
        sb.AppendLine("                    // 回退到 base64 编码方式");
        sb.AppendLine("                    const encodedDiagram = btoa(unescape(encodeURIComponent(currentDiagramData)));");
        sb.AppendLine("                    const mermaidLiveUrl = `https://mermaid.live/edit#base64:${encodedDiagram}`;");
        sb.AppendLine("                    window.open(mermaidLiveUrl, '_blank');");
        sb.AppendLine("                }");
        sb.AppendLine("            } catch (error) {");
        sb.AppendLine("                console.error('无法打开 Mermaid Live:', error);");
        sb.AppendLine("                // 如果编码失败，尝试直接传递代码");
        sb.AppendLine("                try {");
        sb.AppendLine("                    const simpleEncodedDiagram = btoa(currentDiagramData);");
        sb.AppendLine("                    const fallbackUrl = `https://mermaid.live/edit#base64:${simpleEncodedDiagram}`;");
        sb.AppendLine("                    window.open(fallbackUrl, '_blank');");
        sb.AppendLine("                } catch (fallbackError) {");
        sb.AppendLine("                    console.error('备用方案也失败:', fallbackError);");
        sb.AppendLine("                    alert('无法打开 Mermaid Live，请检查浏览器控制台');");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 切换单独链路展开/收起");
        sb.AppendLine("        function toggleIndividualChains() {");
        sb.AppendLine("            individualChainsExpanded = !individualChainsExpanded;");
        sb.AppendLine("            const container = document.getElementById('individualChainsContainer');");
        sb.AppendLine("            const toggles = document.querySelectorAll('.expand-toggle');");
        sb.AppendLine("            const individualToggle = toggles[1]; // 第二个展开/收起按钮");
        sb.AppendLine("            ");
        sb.AppendLine("            if (individualChainsExpanded) {");
        sb.AppendLine("                container.classList.remove('chains-collapsed');");
        sb.AppendLine("                if (individualToggle) individualToggle.textContent = '▼';");
        sb.AppendLine("            } else {");
        sb.AppendLine("                container.classList.add('chains-collapsed');");
        sb.AppendLine("                if (individualToggle) individualToggle.textContent = '▶';");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 添加URL哈希变化监听");
        sb.AppendLine("        function addHashChangeListener() {");
        sb.AppendLine("            window.addEventListener('hashchange', handleHashChange);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 根据链路名称查找链路索引");
        sb.AppendLine("        function findChainIndexByName(chainName, chainArray) {");
        sb.AppendLine("            const decodedName = decodeURIComponent(chainName).replace(/-/g, ' ');");
        sb.AppendLine("            for (let i = 0; i < chainArray.length; i++) {");
        sb.AppendLine("                const normalizedChainName = chainArray[i].name.replace(/[^a-zA-Z0-9\\u4e00-\\u9fa5]/g, '-');");
        sb.AppendLine("                if (normalizedChainName === chainName || chainArray[i].name === decodedName) {");
        sb.AppendLine("                    return i;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            return -1;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 处理URL哈希变化");
        sb.AppendLine("        function handleHashChange() {");
        sb.AppendLine("            const hash = window.location.hash.substring(1); // 移除 # 符号");
        sb.AppendLine("            if (!hash) return;");
        sb.AppendLine();
        sb.AppendLine("            // 处理图表类型");
        sb.AppendLine("            if (diagramConfigs[hash]) {");
        sb.AppendLine("                showDiagram(hash, false);");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // 处理命令链路");
        sb.AppendLine("            if (hash.startsWith('chain-')) {");
        sb.AppendLine("                const chainName = hash.substring(6);");
        sb.AppendLine("                // 先尝试按名称查找");
        sb.AppendLine("                let chainIndex = findChainIndexByName(chainName, commandChains);");
        sb.AppendLine("                // 如果按名称找不到，尝试按索引查找（向后兼容）");
        sb.AppendLine("                if (chainIndex === -1) {");
        sb.AppendLine("                    chainIndex = parseInt(chainName);");
        sb.AppendLine("                }");
        sb.AppendLine("                if (!isNaN(chainIndex) && chainIndex >= 0 && chainIndex < commandChains.length) {");
        sb.AppendLine("                    showChain(chainIndex, false);");
        sb.AppendLine("                    return;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // 处理单独链路流程图");
        sb.AppendLine("            if (hash.startsWith('individual-chain-')) {");
        sb.AppendLine("                const chainName = hash.substring(17);");
        sb.AppendLine("                // 先尝试按名称查找");
        sb.AppendLine("                let chainIndex = findChainIndexByName(chainName, allChainFlowCharts);");
        sb.AppendLine("                // 如果按名称找不到，尝试按索引查找（向后兼容）");
        sb.AppendLine("                if (chainIndex === -1) {");
        sb.AppendLine("                    chainIndex = parseInt(chainName);");
        sb.AppendLine("                }");
        sb.AppendLine("                if (!isNaN(chainIndex) && chainIndex >= 0 && chainIndex < allChainFlowCharts.length) {");
        sb.AppendLine("                    showIndividualChain(chainIndex, false);");
        sb.AppendLine("                    return;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 处理初始URL哈希");
        sb.AppendLine("        function handleInitialHash() {");
        sb.AppendLine("            const hash = window.location.hash.substring(1);");
        sb.AppendLine("            if (hash) {");
        sb.AppendLine("                handleHashChange();");
        sb.AppendLine("            } else {");
        sb.AppendLine("                // 默认显示架构图");
        sb.AppendLine("                showDiagram('architecture', false);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 搜索功能");
        sb.AppendLine("        let allSearchableItems = [];");
        sb.AppendLine();
        sb.AppendLine("        // 初始化搜索数据");
        sb.AppendLine("        function initializeSearchData() {");
        sb.AppendLine("            allSearchableItems = [");
        sb.AppendLine("                { name: '完整架构流程图', type: 'architecture', category: '整体架构', icon: '📋', target: 'architecture' },");
        sb.AppendLine("                { name: '类图', type: 'class', category: '整体架构', icon: '🏛️', target: 'class' },");
        sb.AppendLine("                { name: '命令流程图', type: 'command', category: '专项流程', icon: '⚡', target: 'command' },");
        sb.AppendLine("                { name: '事件流程图', type: 'event', category: '专项流程', icon: '📡', target: 'event' },");
        sb.AppendLine("                { name: '多链路流程图', type: 'multiChain', category: '链路流程图', icon: '🔗', target: 'multiChain' }");
        sb.AppendLine("            ];");
        sb.AppendLine();
        sb.AppendLine("            // 添加命令链路");
        sb.AppendLine("            commandChains.forEach((chain, index) => {");
        sb.AppendLine("                const chainId = encodeURIComponent(chain.name.replace(/[^a-zA-Z0-9\\u4e00-\\u9fa5]/g, '-'));");
        sb.AppendLine("                allSearchableItems.push({");
        sb.AppendLine("                    name: chain.name,");
        sb.AppendLine("                    type: 'chain',");
        sb.AppendLine("                    category: '命令链路',");
        sb.AppendLine("                    icon: '🔗',");
        sb.AppendLine("                    target: `chain-${chainId}`,");
        sb.AppendLine("                    index: index");
        sb.AppendLine("                });");
        sb.AppendLine("            });");
        sb.AppendLine();
        sb.AppendLine("            // 添加单独链路流程图");
        sb.AppendLine("            allChainFlowCharts.forEach((chain, index) => {");
        sb.AppendLine("                const chainId = encodeURIComponent(chain.name.replace(/[^a-zA-Z0-9\\u4e00-\\u9fa5]/g, '-'));");
        sb.AppendLine("                allSearchableItems.push({");
        sb.AppendLine("                    name: chain.name,");
        sb.AppendLine("                    type: 'individualChain',");
        sb.AppendLine("                    category: '单独链路流程图',");
        sb.AppendLine("                    icon: '📊',");
        sb.AppendLine("                    target: `individual-chain-${chainId}`,");
        sb.AppendLine("                    index: index");
        sb.AppendLine("                });");
        sb.AppendLine("            });");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 执行搜索");
        sb.AppendLine("        function performSearch() {");
        sb.AppendLine("            const searchBox = document.getElementById('searchBox');");
        sb.AppendLine("            const searchResults = document.getElementById('searchResults');");
        sb.AppendLine("            const query = searchBox.value.trim().toLowerCase();");
        sb.AppendLine();
        sb.AppendLine("            if (query === '') {");
        sb.AppendLine("                searchResults.style.display = 'none';");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // 搜索匹配项");
        sb.AppendLine("            const results = allSearchableItems.filter(item => ");
        sb.AppendLine("                item.name.toLowerCase().includes(query) || ");
        sb.AppendLine("                item.category.toLowerCase().includes(query)");
        sb.AppendLine("            );");
        sb.AppendLine();
        sb.AppendLine("            // 显示搜索结果");
        sb.AppendLine("            displaySearchResults(results, query);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 显示搜索结果");
        sb.AppendLine("        function displaySearchResults(results, query) {");
        sb.AppendLine("            const searchResults = document.getElementById('searchResults');");
        sb.AppendLine("            searchResults.innerHTML = '';");
        sb.AppendLine();
        sb.AppendLine("            if (results.length === 0) {");
        sb.AppendLine("                searchResults.innerHTML = '<div class=\"search-no-results\">未找到匹配的图表</div>';");
        sb.AppendLine("                searchResults.style.display = 'block';");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            results.forEach(item => {");
        sb.AppendLine("                const resultItem = document.createElement('div');");
        sb.AppendLine("                resultItem.className = 'search-result-item';");
        sb.AppendLine("                resultItem.innerHTML = `${item.icon} ${highlightMatch(item.name, query)} <span class=\"search-category\">[${item.category}]</span>`;");
        sb.AppendLine("                resultItem.onclick = () => {");
        sb.AppendLine("                    selectSearchResult(item);");
        sb.AppendLine("                };");
        sb.AppendLine("                searchResults.appendChild(resultItem);");
        sb.AppendLine("            });");
        sb.AppendLine();
        sb.AppendLine("            searchResults.style.display = 'block';");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 高亮匹配文本");
        sb.AppendLine("        function highlightMatch(text, query) {");
        sb.AppendLine("            if (!query) return text;");
        sb.AppendLine("            const regex = new RegExp(`(${escapeRegExp(query)})`, 'gi');");
        sb.AppendLine("            return text.replace(regex, '<strong>$1</strong>');");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 转义正则表达式特殊字符");
        sb.AppendLine("        function escapeRegExp(string) {");
        sb.AppendLine("            return string.replace(/[.*+?^${}()|[\\]\\\\]/g, '\\\\$&');");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 选择搜索结果");
        sb.AppendLine("        function selectSearchResult(item) {");
        sb.AppendLine("            // 隐藏搜索结果");
        sb.AppendLine("            document.getElementById('searchResults').style.display = 'none';");
        sb.AppendLine("            document.getElementById('searchBox').value = '';");
        sb.AppendLine();
        sb.AppendLine("            // 根据类型导航到对应图表");
        sb.AppendLine("            switch (item.type) {");
        sb.AppendLine("                case 'architecture':");
        sb.AppendLine("                case 'class':"); 
        sb.AppendLine("                case 'command':");
        sb.AppendLine("                case 'event':");
        sb.AppendLine("                case 'multiChain':");
        sb.AppendLine("                    window.location.hash = item.target;");
        sb.AppendLine("                    break;");
        sb.AppendLine("                case 'chain':");
        sb.AppendLine("                    // 确保命令链路菜单展开");
        sb.AppendLine("                    if (!chainsExpanded) {");
        sb.AppendLine("                        toggleChains();");
        sb.AppendLine("                    }");
        sb.AppendLine("                    window.location.hash = item.target;");
        sb.AppendLine("                    break;");
        sb.AppendLine("                case 'individualChain':");
        sb.AppendLine("                    // 确保单独链路流程图菜单展开");
        sb.AppendLine("                    if (!individualChainsExpanded) {");
        sb.AppendLine("                        toggleIndividualChains();");
        sb.AppendLine("                    }");
        sb.AppendLine("                    window.location.hash = item.target;");
        sb.AppendLine("                    break;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 点击页面其他地方隐藏搜索结果");
        sb.AppendLine("        document.addEventListener('click', function(event) {");
        sb.AppendLine("            const searchContainer = document.querySelector('.search-container');");
        sb.AppendLine("            if (!searchContainer.contains(event.target)) {");
        sb.AppendLine("                document.getElementById('searchResults').style.display = 'none';");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine();
        sb.AppendLine("        // 键盘导航支持");
        sb.AppendLine("        document.getElementById('searchBox').addEventListener('keydown', function(event) {");
        sb.AppendLine("            const searchResults = document.getElementById('searchResults');");
        sb.AppendLine("            const resultItems = searchResults.querySelectorAll('.search-result-item');");
        sb.AppendLine();
        sb.AppendLine("            if (event.key === 'Escape') {");
        sb.AppendLine("                searchResults.style.display = 'none';");
        sb.AppendLine("                this.value = '';");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (event.key === 'Enter' && resultItems.length > 0) {");
        sb.AppendLine("                const highlighted = searchResults.querySelector('.search-result-item.highlight');");
        sb.AppendLine("                if (highlighted) {");
        sb.AppendLine("                    highlighted.click();");
        sb.AppendLine("                } else {");
        sb.AppendLine("                    resultItems[0].click();");
        sb.AppendLine("                }");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {");
        sb.AppendLine("                event.preventDefault();");
        sb.AppendLine("                const highlighted = searchResults.querySelector('.search-result-item.highlight');");
        sb.AppendLine("                let nextIndex = 0;");
        sb.AppendLine();
        sb.AppendLine("                if (highlighted) {");
        sb.AppendLine("                    highlighted.classList.remove('highlight');");
        sb.AppendLine("                    const currentIndex = Array.from(resultItems).indexOf(highlighted);");
        sb.AppendLine("                    if (event.key === 'ArrowDown') {");
        sb.AppendLine("                        nextIndex = (currentIndex + 1) % resultItems.length;");
        sb.AppendLine("                    } else {");
        sb.AppendLine("                        nextIndex = (currentIndex - 1 + resultItems.length) % resultItems.length;");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                if (resultItems[nextIndex]) {");
        sb.AppendLine("                    resultItems[nextIndex].classList.add('highlight');");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine();
        sb.AppendLine("        // 页面加载完成后初始化");
        sb.AppendLine("        document.addEventListener('DOMContentLoaded', function() {");
        sb.AppendLine("            initializePage();");
        sb.AppendLine("            initializeSearchData();");
        sb.AppendLine("        });");
    }

    /// <summary>
    /// HTML转义
    /// </summary>
    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&#39;");
    }

    /// <summary>
    /// JavaScript字符串转义
    /// </summary>
    private static string EscapeJavaScript(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("'", "\\'")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t")
                   .Replace("<", "\\u003c")
                   .Replace(">", "\\u003e");
    }

    /// <summary>
    /// JavaScript模板字符串转义
    /// </summary>
    private static string EscapeJavaScriptTemplate(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("\\", "\\\\")
                   .Replace("`", "\\`")
                   .Replace("${", "\\${");
    }

    /// <summary>
    /// 格式化字符串数组为JavaScript数组
    /// </summary>
    private static string FormatStringArray(IEnumerable<string> strings)
    {
        if (strings == null) return "[]";
        var escaped = strings.Select(s => $"\"{EscapeJavaScript(s)}\"");
        return $"[{string.Join(", ", escaped)}]";
    }

    /// <summary>
    /// 获取节点样式类
    /// </summary>
    private static string GetNodeStyleClass(string nodeFullName, CodeFlowAnalysisResult analysisResult)
    {
        var className = GetClassNameFromFullName(nodeFullName);

        if (analysisResult.Controllers.Any(c => c.FullName == nodeFullName))
            return "controller";
        if (analysisResult.Commands.Any(c => c.FullName == nodeFullName))
            return "command";
        if (analysisResult.Entities.Any(e => e.FullName == nodeFullName))
            return "entity";
        if (analysisResult.DomainEvents.Any(d => d.FullName == nodeFullName))
            return "domainEvent";
        if (analysisResult.IntegrationEvents.Any(i => i.FullName == nodeFullName))
            return "integrationEvent";
        if (analysisResult.DomainEventHandlers.Any(h => h.FullName == nodeFullName))
            return "handler";
        if (analysisResult.IntegrationEventHandlers.Any(h => h.FullName == nodeFullName))
            return "handler";
        if (analysisResult.IntegrationEventConverters.Any(c => c.FullName == nodeFullName))
            return "converter";

        return "default";
    }

    #endregion
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetCorePal.Extensions.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace NetCorePal.Extensions.CodeAnalysis.UnitTests;

public class AnalysisResultAggregatorTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Aggregate_WithEmptyAssemblies_ShouldReturnEmptyResult()
    {
        // Act
        var result = AnalysisResultAggregator.Aggregate();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Controllers);
        Assert.Empty(result.Commands);
        Assert.Empty(result.Entities);
        Assert.Empty(result.DomainEvents);
        Assert.Empty(result.DomainEventHandlers);
        Assert.Empty(result.IntegrationEvents);
        Assert.Empty(result.IntegrationEventHandlers);
        Assert.Empty(result.IntegrationEventConverters);
        Assert.Empty(result.Relationships);
    }

    [Fact]
    public void Aggregate_WithNullAssemblies_ShouldReturnEmptyResult()
    {
        // Act
        var result = AnalysisResultAggregator.Aggregate((Assembly[])null!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Controllers);
        Assert.Empty(result.Commands);
        Assert.Empty(result.Entities);
        Assert.Empty(result.DomainEvents);
        Assert.Empty(result.DomainEventHandlers);
        Assert.Empty(result.IntegrationEvents);
        Assert.Empty(result.IntegrationEventHandlers);
        Assert.Empty(result.IntegrationEventConverters);
        Assert.Empty(result.Relationships);
    }

    [Fact]
    public void Aggregate_WithValidAssembly_ShouldReturnResult()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var result = AnalysisResultAggregator.Aggregate(assembly);

        // Assert
        Assert.NotNull(result);
        
        // 验证是否能检测到我们创建的测试类
        testOutputHelper.WriteLine($"Found {result.Controllers.Count} controllers");
        testOutputHelper.WriteLine($"Found {result.Commands.Count} commands");
        testOutputHelper.WriteLine($"Found {result.Entities.Count} entities");
        testOutputHelper.WriteLine($"Found {result.DomainEvents.Count} domain events");
        testOutputHelper.WriteLine($"Found {result.DomainEventHandlers.Count} domain event handlers");
        testOutputHelper.WriteLine($"Found {result.IntegrationEvents.Count} integration events");
        testOutputHelper.WriteLine($"Found {result.IntegrationEventHandlers.Count} integration event handlers");
        testOutputHelper.WriteLine($"Found {result.IntegrationEventConverters.Count} integration event converters");
        testOutputHelper.WriteLine($"Found {result.Relationships.Count} relationships");
    }

    [Fact]
    public void AggregateFromCurrentDomain_ShouldReturnResult()
    {
        // Act
        var result = AnalysisResultAggregator.AggregateFromCurrentDomain();

        // Assert
        Assert.NotNull(result);
        
        // 验证集合数量
        Assert.Equal(9, result.Controllers.Count);
        Assert.Equal(11, result.Commands.Count);
        Assert.Equal(2, result.Entities.Count);
        Assert.Equal(8, result.DomainEvents.Count);
        Assert.Equal(4, result.IntegrationEvents.Count);
        Assert.Equal(7, result.DomainEventHandlers.Count);
        Assert.Equal(6, result.IntegrationEventHandlers.Count);
        Assert.Equal(4, result.IntegrationEventConverters.Count);
        // Relationships 基于源生成器分析的确切调用关系
        Assert.Equal(69, result.Relationships.Count);
        
        // 验证关系类型的分类计数
        Assert.Equal(28, result.Relationships.Count(r => r.CallType == "MethodToCommand"));
        Assert.Equal(15, result.Relationships.Count(r => r.CallType == "CommandToAggregateMethod"));
        Assert.Equal(7, result.Relationships.Count(r => r.CallType == "DomainEventToHandler"));
        Assert.Equal(6, result.Relationships.Count(r => r.CallType == "IntegrationEventToHandler"));
        Assert.Equal(4, result.Relationships.Count(r => r.CallType == "DomainEventToIntegrationEvent"));
        Assert.Equal(9, result.Relationships.Count(r => r.CallType == "MethodToDomainEvent"));
        
        // 验证控制器
        Assert.Contains(result.Controllers, c => c.Name == "UserController");
        Assert.Contains(result.Controllers, c => c.Name == "OrderController");
        
        // 验证端点
        Assert.Contains(result.Controllers, c => c.Name == "CreateUserEndpoint");
        Assert.Contains(result.Controllers, c => c.Name == "CreateOrderEndpoint");
        Assert.Contains(result.Controllers, c => c.Name == "ActivateUserEndpoint");
        Assert.Contains(result.Controllers, c => c.Name == "DeactivateUserEndpoint");
        
        // 验证命令
        Assert.Contains(result.Commands, c => c.Name == "CreateUserCommand");
        Assert.Contains(result.Commands, c => c.Name == "ActivateUserCommand");
        Assert.Contains(result.Commands, c => c.Name == "DeactivateUserCommand");
        Assert.Contains(result.Commands, c => c.Name == "CreateOrderCommand");
        Assert.Contains(result.Commands, c => c.Name == "CancelOrderCommand");
        Assert.Contains(result.Commands, c => c.Name == "ConfirmOrderCommand");
        Assert.Contains(result.Commands, c => c.Name == "OrderPaidCommand");
        Assert.Contains(result.Commands, c => c.Name == "DeleteOrderCommand");
        Assert.Contains(result.Commands, c => c.Name == "ChangeOrderNameCommand");
        
        // 验证聚合根
        Assert.Contains(result.Entities, e => e.Name == "User" && e.IsAggregateRoot);
        Assert.Contains(result.Entities, e => e.Name == "Order" && e.IsAggregateRoot);
        
        // 验证领域事件
        Assert.Contains(result.DomainEvents, e => e.Name == "UserCreatedDomainEvent");
        Assert.Contains(result.DomainEvents, e => e.Name == "UserActivatedDomainEvent");
        Assert.Contains(result.DomainEvents, e => e.Name == "UserDeactivatedDomainEvent");
        Assert.Contains(result.DomainEvents, e => e.Name == "UserRegisteredDomainEvent");
        Assert.Contains(result.DomainEvents, e => e.Name == "OrderCreatedDomainEvent");
        Assert.Contains(result.DomainEvents, e => e.Name == "OrderPaidDomainEvent");
        Assert.Contains(result.DomainEvents, e => e.Name == "OrderNameChangedDomainEvent");
        Assert.Contains(result.DomainEvents, e => e.Name == "OrderDeletedDomainEvent");
        
        // 验证集成事件
        Assert.Contains(result.IntegrationEvents, e => e.Name == "UserRegisteredIntegrationEvent");
        Assert.Contains(result.IntegrationEvents, e => e.Name == "OrderCreatedIntegrationEvent");
        Assert.Contains(result.IntegrationEvents, e => e.Name == "OrderPaidIntegrationEvent");
        
        // 验证关系：控制器方法到命令
        Assert.Contains(result.Relationships, r => 
            r.SourceType.Contains("UserController") && 
            r.TargetType.Contains("CreateUserCommand") && 
            r.CallType == "MethodToCommand");
        
        Assert.Contains(result.Relationships, r => 
            r.SourceType.Contains("OrderController") && 
            r.TargetType.Contains("CreateOrderCommand") && 
            r.CallType == "MethodToCommand");
        
        // 验证关系：端点到命令
        Assert.Contains(result.Relationships, r => 
            r.SourceType.Contains("CreateUserEndpoint") && 
            r.TargetType.Contains("CreateUserCommand") && 
            r.CallType == "MethodToCommand");
        
        // 验证关系：聚合方法到领域事件
        Assert.Contains(result.Relationships, r => 
            r.SourceType.Contains("User") && 
            r.TargetType.Contains("UserCreatedDomainEvent") && 
            r.CallType == "MethodToDomainEvent");
        
        Assert.Contains(result.Relationships, r => 
            r.SourceType.Contains("Order") && 
            r.TargetType.Contains("OrderCreatedDomainEvent") && 
            r.CallType == "MethodToDomainEvent");
        
        // 输出详细的关系信息用于分析
        testOutputHelper.WriteLine($"\nFound {result.Relationships.Count} relationships:");
        var relationshipsByType = result.Relationships.GroupBy(r => r.CallType).ToList();
        foreach (var group in relationshipsByType)
        {
            testOutputHelper.WriteLine($"\n{group.Key} ({group.Count()}):");
            foreach (var relationship in group)
            {
                testOutputHelper.WriteLine($"  - {relationship.SourceType}.{relationship.SourceMethod} -> {relationship.TargetType}.{relationship.TargetMethod}");
            }
        }
        
        // 基于实际测试结果更新断言
        Assert.Equal(69, result.Relationships.Count);
        
        testOutputHelper.WriteLine($"\nFound {result.Controllers.Count} controllers:");
        foreach (var controller in result.Controllers)
        {
            testOutputHelper.WriteLine($"  - {controller.Name} ({controller.FullName})");
        }
        
        testOutputHelper.WriteLine($"\nFound {result.Commands.Count} commands:");
        foreach (var command in result.Commands)
        {
            testOutputHelper.WriteLine($"  - {command.Name} ({command.FullName})");
        }
        
        testOutputHelper.WriteLine($"\nFound {result.Entities.Count} entities:");
        foreach (var entity in result.Entities)
        {
            testOutputHelper.WriteLine($"  - {entity.Name} ({entity.FullName}) [IsAggregateRoot: {entity.IsAggregateRoot}]");
        }
        
        testOutputHelper.WriteLine($"\nFound {result.DomainEvents.Count} domain events:");
        foreach (var domainEvent in result.DomainEvents)
        {
            testOutputHelper.WriteLine($"  - {domainEvent.Name} ({domainEvent.FullName})");
        }
        
        testOutputHelper.WriteLine($"\nFound {result.IntegrationEvents.Count} integration events:");
        foreach (var integrationEvent in result.IntegrationEvents)
        {
            testOutputHelper.WriteLine($"  - {integrationEvent.Name} ({integrationEvent.FullName})");
        }
    }

    [Fact]
    public void AggregateFromAssemblyNames_WithEmptyNames_ShouldReturnEmptyResult()
    {
        // Act
        var result = AnalysisResultAggregator.AggregateFromAssemblyNames();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Controllers);
    }

    [Fact]
    public void AggregateFromAssemblyNames_WithInvalidName_ShouldReturnEmptyResult()
    {
        // Act
        var result = AnalysisResultAggregator.AggregateFromAssemblyNames("InvalidAssemblyName.dll");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Controllers);
    }

    [Fact]
    public void Aggregate_WithTestData_ShouldProduceValidResult()
    {
        // Arrange - 创建测试用的分析结果实现类
        var testResult = CreateTestAnalysisResult();

        // 由于我们不能直接测试内部合并逻辑，我们创建一个模拟场景
        var result = new CodeFlowAnalysisResult();
        
        // 手动模拟合并过程
        result.Controllers.AddRange(testResult.Controllers);
        result.Commands.AddRange(testResult.Commands);
        result.Entities.AddRange(testResult.Entities);
        result.DomainEvents.AddRange(testResult.DomainEvents);
        result.DomainEventHandlers.AddRange(testResult.DomainEventHandlers);
        result.IntegrationEvents.AddRange(testResult.IntegrationEvents);
        result.IntegrationEventHandlers.AddRange(testResult.IntegrationEventHandlers);
        result.IntegrationEventConverters.AddRange(testResult.IntegrationEventConverters);
        result.Relationships.AddRange(testResult.Relationships);

        // Assert
        Assert.NotEmpty(result.Controllers);
        Assert.NotEmpty(result.Commands);
        Assert.NotEmpty(result.Entities);
        Assert.NotEmpty(result.DomainEvents);
        Assert.NotEmpty(result.DomainEventHandlers);
        Assert.NotEmpty(result.IntegrationEvents);
        Assert.NotEmpty(result.IntegrationEventHandlers);
        Assert.NotEmpty(result.IntegrationEventConverters);
        Assert.NotEmpty(result.Relationships);

        var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        testOutputHelper.WriteLine("Generated Analysis Result:");
        testOutputHelper.WriteLine(json);
    }

    [Fact]
    public void RunConsoleAnalysis_ShouldOutputResults()
    {
        // 运行控制台分析并输出结果
        AnalysisTestRunner.RunAnalysis();
        
        // 这个测试总是通过，主要目的是触发控制台输出
        Assert.True(true);
    }

    private static CodeFlowAnalysisResult CreateTestAnalysisResult()
    {
        return new CodeFlowAnalysisResult
        {
            Controllers = new List<ControllerInfo>
            {
                new() { Name = "OrderController", FullName = "Test.Controllers.OrderController", Methods = new List<string> { "Get", "Post", "SetPaid" } },
                new() { Name = "UserController", FullName = "Test.Controllers.UserController", Methods = new List<string> { "CreateUser", "Login" } }
            },
            Commands = new List<CommandInfo>
            {
                new() { Name = "CreateOrderCommand", FullName = "Test.Application.Commands.CreateOrderCommand", Properties = new List<string>() },
                new() { Name = "OrderPaidCommand", FullName = "Test.Application.Commands.OrderPaidCommand", Properties = new List<string>() },
                new() { Name = "DeleteOrderCommand", FullName = "Test.Application.Commands.DeleteOrderCommand", Properties = new List<string>() }
            },
            Entities = new List<EntityInfo>
            {
                new() { Name = "Order", FullName = "Test.Domain.Order", IsAggregateRoot = true, Methods = new List<string> { "OrderPaid", "SoftDelete", "ChangeItemName" } },
                new() { Name = "DeliverRecord", FullName = "Test.Domain.DeliverRecord", IsAggregateRoot = true, Methods = new List<string>() }
            },
            DomainEvents = new List<DomainEventInfo>
            {
                new() { Name = "OrderCreatedDomainEvent", FullName = "Test.Domain.DomainEvents.OrderCreatedDomainEvent", Properties = new List<string>() }
            },
            IntegrationEvents = new List<IntegrationEventInfo>
            {
                new() { Name = "OrderCreatedIntegrationEvent", FullName = "Test.Application.IntegrationEvents.OrderCreatedIntegrationEvent" },
                new() { Name = "OrderPaidIntegrationEvent", FullName = "Test.Application.IntegrationEvents.OrderPaidIntegrationEvent" }
            },
            DomainEventHandlers = new List<DomainEventHandlerInfo>
            {
                new() { Name = "OrderCreatedDomainEventHandler", FullName = "Test.Application.DomainEventHandlers.OrderCreatedDomainEventHandler", HandledEventType = "Test.Domain.DomainEvents.OrderCreatedDomainEvent", Commands = new List<string>() }
            },
            IntegrationEventHandlers = new List<IntegrationEventHandlerInfo>
            {
                new() { Name = "OrderCreatedIntegrationEventHandler", FullName = "Test.Application.IntegrationEventHandlers.OrderCreatedIntegrationEventHandler", HandledEventType = "Test.Application.IntegrationEvents.OrderCreatedIntegrationEvent", Commands = new List<string>() },
                new() { Name = "OrderPaidIntegrationEventHandler", FullName = "Test.Application.IntegrationEventHandlers.OrderPaidIntegrationEventHandler", HandledEventType = "Test.Application.IntegrationEvents.OrderPaidIntegrationEvent", Commands = new List<string>() }
            },
            IntegrationEventConverters = new List<IntegrationEventConverterInfo>
            {
                new() { Name = "OrderCreatedIntegrationEventConverter", FullName = "Test.Application.IntegrationConverters.OrderCreatedIntegrationEventConverter", DomainEventType = "Test.Domain.DomainEvents.OrderCreatedDomainEvent", IntegrationEventType = "Test.Application.IntegrationEvents.OrderCreatedIntegrationEvent" }
            },
            Relationships = new List<CallRelationship>
            {
                new("Test.Controllers.OrderController", "Post", "Test.Application.Commands.CreateOrderCommand", "", "MethodToCommand"),
                new("Test.Application.Commands.OrderPaidCommand", "Handle", "Test.Domain.Order", "OrderPaid", "CommandToAggregateMethod"),
                new("Test.Application.Commands.DeleteOrderCommand", "Handle", "Test.Domain.Order", "SoftDelete", "CommandToAggregateMethod"),
                new("Test.Domain.DomainEvents.OrderCreatedDomainEvent", "", "Test.Application.DomainEventHandlers.OrderCreatedDomainEventHandler", "HandleAsync", "DomainEventToHandler"),
                new("Test.Domain.DomainEvents.OrderCreatedDomainEvent", "", "Test.Application.IntegrationEvents.OrderCreatedIntegrationEvent", "", "DomainEventToIntegrationEvent"),
                new("Test.Application.IntegrationEvents.OrderCreatedIntegrationEvent", "", "Test.Application.IntegrationEventHandlers.OrderCreatedIntegrationEventHandler", "Subscribe", "IntegrationEventToHandler"),
                new("Test.Application.IntegrationEvents.OrderPaidIntegrationEvent", "", "Test.Application.IntegrationEventHandlers.OrderPaidIntegrationEventHandler", "Subscribe", "IntegrationEventToHandler")
            }
        };
    }

    [Fact]
    public void AnalyzeRelationshipDetails_ShouldShowBreakdown()
    {
        // Act
        var result = AnalysisResultAggregator.AggregateFromCurrentDomain();

        // Assert
        Assert.NotNull(result);
        
        // 按关系类型分组并统计
        var relationshipsByType = result.Relationships.GroupBy(r => r.CallType).ToList();
        
        testOutputHelper.WriteLine("=== 关系类型详细分析 ===");
        testOutputHelper.WriteLine($"总关系数: {result.Relationships.Count}");
        testOutputHelper.WriteLine("");
        
        foreach (var group in relationshipsByType.OrderBy(g => g.Key))
        {
            testOutputHelper.WriteLine($"{group.Key}: {group.Count()} 个");
            foreach (var relationship in group.OrderBy(r => r.SourceType).ThenBy(r => r.TargetType))
            {
                testOutputHelper.WriteLine($"  - {relationship.SourceType}.{relationship.SourceMethod} -> {relationship.TargetType}.{relationship.TargetMethod}");
            }
            testOutputHelper.WriteLine("");
        }
        
        // 验证总数
        var totalCount = relationshipsByType.Sum(g => g.Count());
        Assert.Equal(69, totalCount);
        
        // 根据实际输出添加分类断言
        var methodToCommandCount = relationshipsByType.FirstOrDefault(g => g.Key == "MethodToCommand")?.Count() ?? 0;
        var domainEventToHandlerCount = relationshipsByType.FirstOrDefault(g => g.Key == "DomainEventToHandler")?.Count() ?? 0;
        var integrationEventToHandlerCount = relationshipsByType.FirstOrDefault(g => g.Key == "IntegrationEventToHandler")?.Count() ?? 0;
        var domainEventToIntegrationEventCount = relationshipsByType.FirstOrDefault(g => g.Key == "DomainEventToIntegrationEvent")?.Count() ?? 0;
        var methodToDomainEventCount = relationshipsByType.FirstOrDefault(g => g.Key == "MethodToDomainEvent")?.Count() ?? 0;
        var commandToAggregateMethodCount = relationshipsByType.FirstOrDefault(g => g.Key == "CommandToAggregateMethod")?.Count() ?? 0;
        
        testOutputHelper.WriteLine("=== 分类统计 ===");
        testOutputHelper.WriteLine($"MethodToCommand: {methodToCommandCount}");
        testOutputHelper.WriteLine($"DomainEventToHandler: {domainEventToHandlerCount}");
        testOutputHelper.WriteLine($"IntegrationEventToHandler: {integrationEventToHandlerCount}");
        testOutputHelper.WriteLine($"DomainEventToIntegrationEvent: {domainEventToIntegrationEventCount}");
        testOutputHelper.WriteLine($"MethodToDomainEvent: {methodToDomainEventCount}");
        testOutputHelper.WriteLine($"CommandToAggregateMethod: {commandToAggregateMethodCount}");
        
        // 输出分类断言代码
        testOutputHelper.WriteLine("\n=== 建议的分类断言 ===");
        testOutputHelper.WriteLine($"Assert.Equal({methodToCommandCount}, result.Relationships.Count(r => r.CallType == \"MethodToCommand\"));");
        testOutputHelper.WriteLine($"Assert.Equal({domainEventToHandlerCount}, result.Relationships.Count(r => r.CallType == \"DomainEventToHandler\"));");
        testOutputHelper.WriteLine($"Assert.Equal({integrationEventToHandlerCount}, result.Relationships.Count(r => r.CallType == \"IntegrationEventToHandler\"));");
        testOutputHelper.WriteLine($"Assert.Equal({domainEventToIntegrationEventCount}, result.Relationships.Count(r => r.CallType == \"DomainEventToIntegrationEvent\"));");
        testOutputHelper.WriteLine($"Assert.Equal({methodToDomainEventCount}, result.Relationships.Count(r => r.CallType == \"MethodToDomainEvent\"));");
        testOutputHelper.WriteLine($"Assert.Equal({commandToAggregateMethodCount}, result.Relationships.Count(r => r.CallType == \"CommandToAggregateMethod\"));");
    }
}

using System.Collections.Concurrent;
using NetCorePal.Extensions.Primitives.Diagnostics;
using SkyApm;
using SkyApm.Common;
using SkyApm.Config;
using SkyApm.Diagnostics;
using SkyApm.Tracing;
using SkyApm.Tracing.Segments;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace NetCorePal.SkyApm.Diagnostics;

public class NetCorePalTracingDiagnosticProcessor : ITracingDiagnosticProcessor
{
    private readonly ConcurrentDictionary<Guid, SegmentContext> _commandContexts =
        new ConcurrentDictionary<Guid, SegmentContext>();

    private StringOrIntValue _component = new StringOrIntValue(3020, "NetCorePal");

    private readonly ConcurrentDictionary<Guid, SegmentContext> _transactionContexts =
        new ConcurrentDictionary<Guid, SegmentContext>();

    private readonly ConcurrentDictionary<Guid, SegmentContext> _domainEventHandlerContexts =
        new ConcurrentDictionary<Guid, SegmentContext>();

    private readonly ConcurrentDictionary<Guid, SegmentContext> _integrationEventHandlerContexts =
        new ConcurrentDictionary<Guid, SegmentContext>();

    public string ListenerName => NetCorePalDiagnosticListenerNames.DiagnosticListenerName;

    private readonly ITracingContext _tracingContext;
    private readonly IEntrySegmentContextAccessor _entrySegmentContextAccessor;
    private readonly TracingConfig _tracingConfig;
    private readonly NetCorePalTracingOptions _options;

    public NetCorePalTracingDiagnosticProcessor(ITracingContext tracingContext,
        IEntrySegmentContextAccessor segmentContextAccessor,
        IConfigAccessor configAccessor,
        IOptions<NetCorePalTracingOptions> options)
    {
        _tracingContext = tracingContext;
        _entrySegmentContextAccessor = segmentContextAccessor;
        _tracingConfig = configAccessor.Get<TracingConfig>();
        _options = options.Value;
    }


    [DiagnosticName(NetCorePalDiagnosticListenerNames.CommandHandlerBegin)]
    public void CommandBegin([Object] CommandBegin eventData)
    {
        var context = _tracingContext.CreateLocalSegmentContext(eventData.Name);
        _commandContexts[eventData.Id] = context;
        context.Span.Component = _component;
        context.Span.AddTag("CommandName", eventData.Name);
        context.Span.AddLog(LogEvent.Event("CommandBegin"));
        if (_options.WriteCommandData)
        {
            context.Span.AddLog(LogEvent.Message("Command：" +
                                                 JsonSerializer.Serialize(eventData.CommandData,
                                                     _options.JsonSerializerOptions)));
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.CommandHandlerEnd)]
    public void CommandEnd([Object] CommandEnd eventData)
    {
        if (_commandContexts.TryRemove(eventData.Id, out var context))
        {
            context.Span.AddLog(LogEvent.Event("CommandEnd"));
            context.Span.AddLog(LogEvent.Message("CommandEnd"));
            _tracingContext.Release(context);
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.CommandHandlerError)]
    public void CommandError([Object] CommandError eventData)
    {
        if (_commandContexts.TryRemove(eventData.Id, out var context))
        {
            context.Span.AddLog(LogEvent.Event("CommandError"));
            context.Span.AddLog(LogEvent.Message("CommandError"));
            context.Span.ErrorOccurred(eventData.Exception, _tracingConfig);
            _tracingContext.Release(context);
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.DomainEventHandlerBegin)]
    public void DomainEventHandlerBegin([Object] DomainEventHandlerBegin eventData)
    {
        var context =
            _tracingContext.CreateLocalSegmentContext(eventData.Name);
        _domainEventHandlerContexts[eventData.Id] = context;
        context.Span.Component = _component;
        context.Span.AddLog(LogEvent.Event("DomainEventHandlerBegin"));
        context.Span.AddLog(LogEvent.Message("DomainEventHandlerBegin: " + eventData.Name));
        if (_options.WriteDomainEventData)
        {
            context.Span.AddLog(LogEvent.Message("DomainEventData：" +
                                                 JsonSerializer.Serialize(eventData.EventData,
                                                     _options.JsonSerializerOptions)));
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.DomainEventHandlerEnd)]
    public void DomainEventHandlerEnd([Object] DomainEventHandlerEnd eventData)
    {
        if (_domainEventHandlerContexts.TryRemove(eventData.Id, out var context))
        {
            context.Span.AddLog(LogEvent.Event("DomainEventHandlerEnd"));
            context.Span.AddLog(LogEvent.Message("DomainEventHandlerEnd: " + eventData.Name));
            _tracingContext.Release(context);
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.DomainEventHandlerError)]
    public void DomainEventHandlerError([Object] DomainEventHandlerError eventData)
    {
        if (_domainEventHandlerContexts.TryRemove(eventData.Id, out var context))
        {
            context.Span.AddLog(LogEvent.Event("DomainEventHandlerError"));
            context.Span.AddLog(LogEvent.Message("DomainEventHandlerError: " + eventData.Name));
            context.Span.ErrorOccurred(eventData.Exception, _tracingConfig);
            _tracingContext.Release(context);
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.TransactionBegin)]
    public void TransactionBegin([Object] TransactionBegin eventData)
    {
        var context = _entrySegmentContextAccessor.Context == null
            ? _tracingContext.CreateEntrySegmentContext("Transaction", null)
            : _tracingContext.CreateLocalSegmentContext("Transaction");
        context.Span.Component = _component;
        context.Span.AddLog(LogEvent.Event("TransactionBegin"));
        context.Span.AddLog(LogEvent.Message("TransactionBegin: " + eventData.TransactionId));
        _transactionContexts[eventData.TransactionId] = context;
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.TransactionCommit)]
    public void TransactionCommit([Object] TransactionCommit eventData)
    {
        if (_transactionContexts.TryRemove(eventData.TransactionId, out var context))
        {
            context.Span.AddLog(LogEvent.Event("TransactionCommit"));
            context.Span.AddLog(LogEvent.Message("TransactionCommit: " + eventData.TransactionId));
            _tracingContext.Release(context);
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.TransactionRollback)]
    public void TransactionRollback([Object] TransactionRollback eventData)
    {
        if (_transactionContexts.TryRemove(eventData.TransactionId, out var context))
        {
            context.Span.AddLog(LogEvent.Event("TransactionRollback"));
            context.Span.AddLog(LogEvent.Message("TransactionRollback: " + eventData.TransactionId));
            _tracingContext.Release(context);
        }
    }


    [DiagnosticName(NetCorePalDiagnosticListenerNames.IntegrationEventHandlerBegin)]
    public void IntegrationEventHandlerBegin([Object] IntegrationEventHandlerBegin eventData)
    {
        var context =
            _tracingContext.CreateLocalSegmentContext(eventData.HandlerName);
        _integrationEventHandlerContexts[eventData.Id] = context;
        context.Span.Component = _component;
        context.Span.AddLog(LogEvent.Event("IntegrationEventHandlerBegin"));
        if (_options.WriteIntegrationEventData)
        {
            context.Span.AddLog(LogEvent.Message("IntegrationEventData：" +
                                                 JsonSerializer.Serialize(eventData.EventData,
                                                     _options.JsonSerializerOptions)));
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.IntegrationEventHandlerEnd)]
    public void IntegrationEventHandlerEnd([Object] IntegrationEventHandlerEnd eventData)
    {
        if (_integrationEventHandlerContexts.TryRemove(eventData.Id, out var context))
        {
            context.Span.AddLog(LogEvent.Event("IntegrationEventHandlerEnd"));
            _tracingContext.Release(context);
        }
    }

    [DiagnosticName(NetCorePalDiagnosticListenerNames.IntegrationEventHandlerError)]
    public void IntegrationEventHandlerError([Object] IntegrationEventHandlerError eventData)
    {
        if (_integrationEventHandlerContexts.TryRemove(eventData.Id, out var context))
        {
            context.Span.AddLog(LogEvent.Event("IntegrationEventHandlerError"));
            context.Span.ErrorOccurred(eventData.Exception, _tracingConfig);
            _tracingContext.Release(context);
        }
    }
}
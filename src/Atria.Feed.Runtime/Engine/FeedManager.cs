using Atria.Common.KV.Factory;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Feed.Runtime.Configuration.Options;
using Atria.Feed.Runtime.Engine.Filters;
using Atria.Feed.Runtime.Engine.Filters.Interfaces;
using Atria.Feed.Runtime.Engine.Filters.Js;
using Atria.Feed.Runtime.Engine.Filters.Js.Interfaces;
using Atria.Feed.Runtime.Engine.Filters.Js.Options;
using Atria.Feed.Runtime.Engine.Functions;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Interfaces;
using Atria.Feed.Runtime.Engine.Functions.Interfaces;
using Atria.Feed.Runtime.Engine.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Runtime.Engine;

public class FeedManager
{
    private readonly FeedRuntimeRegistry _feedRegistry;
    private readonly IFissionClient _fissionClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly RuntimeOptions _runtimeOptions;
    private readonly IJsRuntimeProvider _jsRuntimeProvider;
    private readonly IOptions<JsRuntimeOptions> _jsRuntimeOptions;
    private readonly IKvStoreFactory? _kvStoreFactory;

    public FeedManager(
        FeedRuntimeRegistry registry,
        IServiceScopeFactory serviceScopeFactory,
        IFissionClient fissionClient,
        IOptions<RuntimeOptions> runtimeOptions,
        IJsRuntimeProvider jsRuntimeProvider,
        IOptions<JsRuntimeOptions> jsRuntimeOptions,
        IKvStoreFactory? kvStoreFactory = null)
    {
        _feedRegistry = registry;
        _serviceScopeFactory = serviceScopeFactory;
        _fissionClient = fissionClient;
        _runtimeOptions = runtimeOptions.Value;
        _jsRuntimeProvider = jsRuntimeProvider;
        _jsRuntimeOptions = jsRuntimeOptions;
        _kvStoreFactory = kvStoreFactory;
    }

    public async Task DeployAsync(FeedRuntime feedRuntime)
    {
        if (_feedRegistry.Exists(feedRuntime.Id))
        {
            await StopAsync(feedRuntime.Id, CancellationToken.None);
        }

        IFilterContext? filterContext = null;
        IFunctionContext? functionContext = null;

        try
        {
            if (!string.IsNullOrEmpty(feedRuntime.FilterCode))
            {
                filterContext = await CreateFilterContextAsync(feedRuntime.FilterLangKind, feedRuntime.FilterCode, feedRuntime.EkvNamespace);
            }

            if (!string.IsNullOrEmpty(feedRuntime.FunctionCode))
            {
                functionContext = CreateFunctionContext(FunctionKind.Fission, feedRuntime.FunctionLangKind, feedRuntime.Id);
                await functionContext.RedeployAndWaitForReadyAsync(feedRuntime.FunctionCode, CancellationToken.None);
            }
        }
        catch
        {
            if (filterContext != null)
            {
                await filterContext.DisposeAsync();
            }

            if (functionContext != null)
            {
                await functionContext.DeleteAsync(CancellationToken.None);
            }

            throw;
        }

        var feedRuntimeContext = new FeedRuntimeContext
        {
            FeedRuntime = feedRuntime,
            FilterContext = filterContext,
            FunctionContext = functionContext,
        };

        _feedRegistry.AddOrUpdate(feedRuntimeContext);
    }

    public async Task StopAsync(string id, CancellationToken ct)
    {
        var feed = _feedRegistry.Get(id);
        if (feed == null)
        {
            return;
        }

        if (feed.FilterContext != null)
        {
            await feed.FilterContext.DisposeAsync();
        }

        if (feed.FunctionContext != null)
        {
            await feed.FunctionContext.DeleteAsync(ct);
        }

        _feedRegistry.Remove(id);
    }

    public FeedRuntimeContext? Get(string id)
    {
        return _feedRegistry.Get(id);
    }

    public async Task<object?> ExecuteAsync(
        string id,
        object? input,
        string fn = "main",
        CancellationToken ct = default)
    {
        var feed = _feedRegistry.Get(id);

        if (feed == null)
        {
            throw new KeyNotFoundException();
        }

        object? result = input;

        var execTimeout = TimeSpan.FromSeconds(_runtimeOptions.ExecutionTimeoutSec);
        if (feed.FilterContext != null)
        {
            using var filterCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            filterCts.CancelAfter(execTimeout);

            try
            {
                result = await feed.FilterContext.ExecuteAsync(fn, result, filterCts.Token);
            }
            catch (OperationCanceledException) when (filterCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Filter execution exceeded timeout {execTimeout}");
            }
        }

        if (feed.FunctionContext != null)
        {
            using var functionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            functionCts.CancelAfter(execTimeout);

            try
            {
                result = await feed.FunctionContext.ExecuteAsync(result, functionCts.Token);
            }
            catch (OperationCanceledException) when (functionCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Function execution exceeded timeout {execTimeout}");
            }
        }

        return result;
    }

    public async Task<TestExecutionResult> ExecuteTestAsync(
        FeedRuntime feedRuntime,
        object? data,
        CancellationToken ct = default)
    {
        IFilterContext? filterContext = null;
        IFunctionContext? functionContext = null;

        var testId = $"{feedRuntime.Id}-test";
        try
        {
            var result = new TestExecutionResult
            {
                FilterResult = data,
            };

            var execTimeout = TimeSpan.FromSeconds(_runtimeOptions.ExecutionTimeoutSec);

            if (!string.IsNullOrEmpty(feedRuntime.FilterCode))
            {
                filterContext = await CreateFilterContextAsync(feedRuntime.FilterLangKind, feedRuntime.FilterCode, feedRuntime.EkvNamespace);

                using var filterCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                filterCts.CancelAfter(execTimeout);

                try
                {
                    result.FilterResult = await filterContext.ExecuteAsync("main", data, filterCts.Token);
                }
                catch (OperationCanceledException) when (filterCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    throw new TimeoutException($"Filter execution exceeded timeout {execTimeout}");
                }
            }

            if (!string.IsNullOrEmpty(feedRuntime.FunctionCode))
            {
                functionContext = CreateFunctionContext(FunctionKind.Fission, feedRuntime.FunctionLangKind, testId);

                await functionContext.RedeployAndWaitForReadyAsync(feedRuntime.FunctionCode, ct);

                using var functionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                functionCts.CancelAfter(execTimeout);

                try
                {
                    result.FunctionResult = await functionContext.ExecuteAsync(result.FilterResult, functionCts.Token);
                }
                catch (OperationCanceledException) when (functionCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    throw new TimeoutException($"Function execution exceeded timeout {execTimeout}");
                }
            }

            return result;
        }
        finally
        {
            if (filterContext != null)
            {
                await filterContext.DisposeAsync();
            }

            if (functionContext != null)
            {
                await functionContext.DeleteAsync(ct);
            }
        }
    }

    public ICollection<FeedRuntimeContext> GetRunningFeeds()
        => _feedRegistry.GetAll();

    private async Task<IFilterContext> CreateFilterContextAsync(FilterLangKind kind, string code, string? ekvNamespace)
    {
        IKvHostBridge? kvBridge = null;

        var requiredModules = JsFilterContext.ParseRequires(code);
        if (requiredModules.Contains("@atria/kv"))
        {
            kvBridge = await CreateKvBridgeAsync(ekvNamespace);
        }

        return kind switch
        {
            FilterLangKind.JavaScript => new JsFilterContext(_jsRuntimeProvider, _jsRuntimeOptions, code, kvBridge),
            _ => throw new NotSupportedException($"{kind} filter type not supported")
        };
    }

    private async Task<IKvHostBridge?> CreateKvBridgeAsync(string? ekvNamespace)
    {
        if (_kvStoreFactory == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(ekvNamespace))
        {
            throw new InvalidOperationException("KV is configured but EkvNamespace is not set in the deploy request");
        }

        var kvStore = await _kvStoreFactory.CreateAsync(ekvNamespace);
        return new KvHostBridge(kvStore);
    }

    private IFunctionContext CreateFunctionContext(FunctionKind kind, FunctionLangKind langKind, string id)
    {
        return kind switch
        {
            FunctionKind.Fission => new FissionFunctionContext(_serviceScopeFactory, id, langKind, _fissionClient),
            _ => throw new NotSupportedException($"{kind} filter type not supported")
        };
    }
}

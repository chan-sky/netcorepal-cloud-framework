using System.Reflection;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NetCorePal.Extensions.AspNetCore;
using NetCorePal.Extensions.AspNetCore.CommandLocks;
using NetCorePal.Extensions.AspNetCore.Validation;
using NetCorePal.Extensions.Domain.Json;
using NetCorePal.Extensions.Dto;
using NetCorePal.Extensions.NewtonsoftJson;
using NetCorePal.Extensions.Primitives;
using System.Text.Json;
using Newtonsoft.Json;

namespace NetCorePal.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    /// <summary>
    /// 添加IRequestCancellationToken服务，用于获取请求的取消令牌
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddRequestCancellationToken(this IServiceCollection services)
    {
        services.AddSingleton<IRequestCancellationToken, HttpContextAccessorRequestAbortedHandler>();
        return services;
    }

    /// <summary>
    /// 添加模型绑定错误处理，使用<see cref="ResponseData"/>作为错误输出
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddKnownExceptionErrorModelInterceptor(this IServiceCollection services)
    {
        services.AddTransient<IValidatorInterceptor, KnownExceptionErrorModelInterceptor>();
        return services;
    }

    /// <summary>
    /// 为Command添加验证器错误处理，使用<see cref="ResponseData"/>作为错误输出
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static MediatRServiceConfiguration AddKnownExceptionValidationBehavior(
        this MediatRServiceConfiguration cfg)
    {
        cfg.AddOpenBehavior(typeof(KnownExceptionValidationBehavior<,>));
        return cfg;
    }

    /// <summary>
    /// 添加CommandLockBehavior，以支持命令锁
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static MediatRServiceConfiguration AddCommandLockBehavior(this MediatRServiceConfiguration cfg)
    {
        cfg.AddOpenBehavior(typeof(CommandLockBehavior<,>));
        return cfg;
    }


    /// <summary>
    /// 将所有实现IQuery接口的类注册为查询类，添加到容器中
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddAllQueries(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            //从assembly中获取所有实现IQuery接口的类
            var queryTypes = assembly.GetTypes().Where(p =>
                p is { IsClass: true, IsAbstract: false } && Array.Exists(p.GetInterfaces(), i => i == typeof(IQuery)));
            foreach (var queryType in queryTypes)
            {
                //注册为自己
                services.AddScoped(queryType, queryType);
            }
        }

        return services;
    }

    /// <summary>
    /// 添加模型绑定错误处理，使用<see cref="ResponseData"/>作为错误输出
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IMvcBuilder AddKnownExceptionModelBinderErrorHandler(this IMvcBuilder builder)
    {
        builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                //错误信息转为 new { errorCode = p.ErrorCode, errorMessage = p.ErrorMessage, propertyName = p.PropertyName } 列表
                var errors = context.ModelState.Where(p => p.Value != null && p.Value.Errors.Count > 0)
                    .SelectMany(p =>
                        p.Value!.Errors.Select(e => new
                            { errorCode = 400, errorMessage = e.ErrorMessage, propertyName = p.Key })).ToArray();


                var result = errors.Length > 0
                    ? new ResponseData(success: false, message: errors[0].errorMessage, errorData: errors,
                        code: errors[0].errorCode)
                    : new ResponseData(success: false, message: "参数错误", errorData: errors, code: 400);

                var r = new JsonResult(result)
                {
                    StatusCode = 200
                };
                return r;
            };
        });
        return builder;
    }

    /// <summary>
    /// 添加EntityId的NewtonsoftJson序列化支持
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
#pragma warning disable S1133
    [Obsolete("请使用AddNetCorePalNewtonsoftJson")]
#pragma warning restore S1133
    public static IMvcBuilder AddEntityIdNewtonsoftJson(this IMvcBuilder builder)
    {
        builder.AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(new NewtonsoftEntityIdJsonConverter());
        });
        return builder;
    }

    /// <summary>
    /// 添加EntityId的NewtonsoftJson序列化支持
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IMvcBuilder AddNetCorePalNewtonsoftJson(this IMvcBuilder builder)
    {
        builder.AddNewtonsoftJson(options => { options.SerializerSettings.AddNetCorePalJsonConverters(); });
        return builder;
    }


    /// <summary>
    /// 添加EntityId的System.Text.Json序列化支持
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
#pragma warning disable S1133
    [Obsolete("请使用AddNetCorePalSystemTextJson")]
#pragma warning restore S1133
    public static IMvcBuilder AddEntityIdSystemTextJson(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new EntityIdJsonConverterFactory());
        });
        return builder;
    }

    /// <summary>
    /// 添加EntityId、UpdateTime的System.Text.Json序列化支持
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IMvcBuilder AddNetCorePalSystemTextJson(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options => { options.JsonSerializerOptions.AddNetCorePalJsonConverters(); });
        return builder;
    }


    /// <summary>
    /// 添加CommandLocks
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddCommandLocks(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();

            foreach (var type in types.Where(p => p is { IsClass: true, IsAbstract: false, IsGenericType: false }))
            {
                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces)
                {
                    if (@interface.IsGenericType &&
                        @interface.GetGenericTypeDefinition() == typeof(ICommandLock<>))
                    {
                        services.AddTransient(@interface, type);
                    }
                }
            }
        }

        return services;
    }
}
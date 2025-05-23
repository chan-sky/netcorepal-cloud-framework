using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCorePal.Extensions.DependencyInjection;
using NetCorePal.Extensions.DistributedTransactions.CAP.UnitTests;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace NetCorePal.Extensions.DistributedTransactions.CAP.SqlServer.UnitTests;

public class MockHost : IAsyncLifetime
{
    private readonly RabbitMqContainer rabbitMqContainer = new RabbitMqBuilder()
        .WithUsername("guest").WithPassword("guest").Build();


    private readonly MsSqlContainer msSqlContainer = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-CU18-ubuntu-22.04").Build();


    public IHost? HostInstance { get; set; }

    async Task RunAsync()
    {
        HostInstance = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<MockDbContext>(options =>
                {
                    options.UseSqlServer(msSqlContainer.GetConnectionString(),
                        b => { b.MigrationsAssembly(typeof(MockDbContext).Assembly.FullName); });
                });

                services.AddCap(x =>
                {
                    x.UseEntityFramework<MockDbContext>();
                    x.UseRabbitMQ(p =>
                    {
                        p.HostName = rabbitMqContainer.Hostname;
                        p.UserName = "guest";
                        p.Password = "guest";
                        p.Port = rabbitMqContainer.GetMappedPublicPort(5672);
                        p.VirtualHost = "/";
                    });
                });

                services.AddMediatR(cfg =>
                    cfg.RegisterServicesFromAssemblies(typeof(MockDbContext).Assembly)
                        .AddUnitOfWorkBehaviors());

                services.AddIntegrationEvents(typeof(MockDbContext)).UseCap<MockDbContext>(capbuilder =>
                {
                    capbuilder.RegisterServicesFromAssemblies(typeof(MockDbContext));
                    capbuilder.UseSqlServer();
                });
            })
            .Build();
        using var scope = HostInstance!.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<MockDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        HostInstance.RunAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }


    public async Task InitializeAsync()
    {
        await Task.WhenAll(rabbitMqContainer.StartAsync(), msSqlContainer.StartAsync());
        await RunAsync();
        await Task.Delay(5000);
    }

    public async Task DisposeAsync()
    {
        if (HostInstance != null)
        {
            await HostInstance.StopAsync();
        }

        await Task.WhenAll(rabbitMqContainer.StopAsync(), msSqlContainer.StopAsync());
    }
}
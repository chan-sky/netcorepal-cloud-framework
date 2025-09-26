using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;

namespace NetCorePal.Extensions.DistributedTransactions.CAP.SqlServer
{
    public class SqlServerCapTransactionFactory(ICapPublisher capPublisher) : ICapTransactionFactory
    {
        public INetCorePalCapTransaction CreateCapTransaction()
        {
            return ActivatorUtilities.CreateInstance<NetCorePalSqlServerCapTransaction>(capPublisher.ServiceProvider);
        }
    }
}
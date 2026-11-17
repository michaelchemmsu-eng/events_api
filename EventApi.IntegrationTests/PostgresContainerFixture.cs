using Microsoft.EntityFrameworkCore;
using Npgsql;
using project.DataAccess;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests
{
    public class PostgresContainerFixture: IAsyncLifetime
    {
        public readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
           .WithDatabase("eventapi")
           .Build();
        public async ValueTask InitializeAsync() => await _postgres.StartAsync();
        public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();
        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            var context = new AppDbContext(options);
            
            return context;
        }

        public async Task ResetDatabaseAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }
    }

    [CollectionDefinition("PostgresContainer collection")]
    public class PostgresContainerCollection : ICollectionFixture<PostgresContainerFixture>
    {
    }
}

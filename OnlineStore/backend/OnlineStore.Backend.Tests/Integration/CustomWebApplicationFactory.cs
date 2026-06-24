using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Backend.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["Jwt:Issuer"] = "OnlineStoreTests",
                ["Jwt:Audience"] = "OnlineStoreTestsClient",
                ["Jwt:Key"] = "TESTING_SECRET_KEY_1234567890_1234567890",
                ["Stripe:SecretKey"] = "sk_test_123",
                ["StorageRoot"] = "storage-tests"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // We don't need background workers during controller integration tests.
            services.RemoveAll<IHostedService>();

            RemoveDbContextRegistrations(services);

            services.AddSingleton(_connection);
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var connection = sp.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var descriptors = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1"))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

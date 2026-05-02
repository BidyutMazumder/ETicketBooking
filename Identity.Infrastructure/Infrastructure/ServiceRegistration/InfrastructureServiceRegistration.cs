using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Identity.Application.Common.Interfaces;
using Identity.Infrastructure.Services;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;

namespace Identity.Infrastructure.Infrastructure.ServiceRegistration;

/// <summary>
/// Registers infrastructure layer services with the dependency injection container.
/// This includes database context, repositories, and infrastructure services.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Infrastructure Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}

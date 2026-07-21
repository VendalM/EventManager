using Microsoft.EntityFrameworkCore;
using Users.Application.Interfaces;
using Users.Infrastructure.DataAccess;
using Users.Infrastructure.Mappers;
using Users.Infrastructure.Repositories;
using Users.Infrastructure.Services;

namespace Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? configuration.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("Connection string is not configured.");
        
        services.AddDbContext<AppDbUserContext>(options =>
            options.UseNpgsql(connectionString)); 
        
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // AutoMapper (профили, лежащие в Infrastructure)
        services.AddAutoMapper(typeof(UserMapping).Assembly);

        return services;
    }
}
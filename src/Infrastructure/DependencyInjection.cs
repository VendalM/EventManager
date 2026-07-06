using Application.Interfaces;
using Infrastructure.DataAccess;
using Infrastructure.Mappers;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection 
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Репозитории
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        // AutoMapper (профили, лежащие в Infrastructure)
        services.AddAutoMapper(typeof(EventMappingProfile).Assembly);

        return services;
    }
}
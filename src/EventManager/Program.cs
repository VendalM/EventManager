using System.Globalization;
using EventManager.Application;
using EventManager.Infrastructure.DataAccess;
using EventManager.Middlewares;
using EventManager.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' not found."); 

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)); 

builder.Services.AddApplication();
builder.Services.AddPresentation();

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

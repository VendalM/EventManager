using System.Globalization;
using Application;
using Infrastructure;
using Infrastructure.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presentation;
using Presentation.Middlewares;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' not found."); 

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)); 

builder.Services.AddInfrastructure(builder.Configuration);
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

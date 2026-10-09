using Microsoft.EntityFrameworkCore;
using P1.Api.Endpoints;
using P1.Core.Data;
using P1.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite("Data Source=p1_test.db"));

builder.Services.AddScoped<StandingsService>();

var app = builder.Build();

/* Code below will seed the databse. 
 * Use it only when neccessarry.
 */
//using var scope = app.Services.CreateScope();
//var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//await new HistoryResultsSeeder(dbCtx).SeedAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapStatsOptionsEndpoints();

app.Run();
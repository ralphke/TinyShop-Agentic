using Microsoft.EntityFrameworkCore;
using Products.Data;
using Products.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<ProductDataContext>(options =>
    options.UseInMemoryDatabase("inmemproducts"));

// Add services to the container.
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapProductEndpoints();

app.UseStaticFiles();

app.CreateDbIfNotExists();

app.Run();

namespace Products
{
    // Expose Program for integration tests
    public partial class Program { }
}
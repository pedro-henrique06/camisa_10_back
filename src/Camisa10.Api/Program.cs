using System.Text.Json.Serialization;
using Camisa10.Api.Data;
using Camisa10.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var mongo = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
var database = builder.Configuration["Mongo:Database"] ?? "camisa10";
builder.Services.AddDbContext<Camisa10DbContext>(o => o.UseMongoDB(mongo, database));

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.MapOpenApi();

using (var scope = app.Services.CreateScope())
{
    // Cria a coleção e os índices declarados no modelo, se ainda não existirem.
    await scope.ServiceProvider.GetRequiredService<Camisa10DbContext>().Database.EnsureCreatedAsync();
}

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapCareerEndpoints();

app.Run();

public partial class Program;

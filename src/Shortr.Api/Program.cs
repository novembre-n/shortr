using Shortr.Application;
using Shortr.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<ILinkRepository, LinkRepository>();
builder.Services.AddSingleton<ShortCodeGenerator>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<LinkService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// The schema must exist before the first request.
app.Services.GetRequiredService<DatabaseInitializer>().Initialize();

app.Run();
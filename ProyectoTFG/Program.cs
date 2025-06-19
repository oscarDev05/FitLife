using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text.Json.Serialization;

var host = Environment.GetEnvironmentVariable("DB_HOST");
var port = Environment.GetEnvironmentVariable("DB_PORT");
var db = Environment.GetEnvironmentVariable("DB_NAME");
var user = Environment.GetEnvironmentVariable("DB_USER");
var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

var builder = WebApplication.CreateBuilder(args);

// Agregar el contexto de base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSignalR();

// Agregar CORS para permitir las peticiones del front
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("https://frontend-q08g.onrender.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Agregar servicios de controladores y opciones JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});

// Configura Kestrel para escuchar en el puerto que asigna Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "5227";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
    options.Limits.MaxRequestBodySize = long.MaxValue;
});

// Agregar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔧 Aplicar migraciones automáticamente en producción (Render)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles(); // Archivos estáticos raíz

// Cabecera CORS para archivos estáticos adicionales
var corsHeader = new Action<StaticFileResponseContext>(ctx =>
{
    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "https://frontend-q08g.onrender.com");
});

// Función para servir carpetas estáticas con CORS
void UseSafeStaticFiles(WebApplication app, string folder)
{
    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder);
    if (Directory.Exists(fullPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(fullPath),
            RequestPath = $"/wwwroot/{folder}",
            OnPrepareResponse = corsHeader
        });
    }
}

// Servir carpetas con imágenes, vídeos, etc.
UseSafeStaticFiles(app, "users_images");
UseSafeStaticFiles(app, "post_files");
UseSafeStaticFiles(app, "ejercicio_files");
UseSafeStaticFiles(app, "miniaturas");

app.UseRouting(); // 👈 Añadido para seguridad con CORS
app.UseCors("AllowFrontend"); // 👈 Obligatorio antes de endpoints

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<ChatHub>("/chatHub").RequireCors("AllowFrontend");
});

app.Run();

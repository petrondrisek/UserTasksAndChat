using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using UserTasksAndChat.Data;
using UserTasksAndChat.Events;
using UserTasksAndChat.Hubs;
using UserTasksAndChat.Models;
using UserTasksAndChat.Repositories;
using UserTasksAndChat.Services;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        builder => builder.WithOrigins(Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "http://localhost:4200")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
    );
});


// Add authentication and authorization
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT key is not configured! Please set 'Jwt:Key' in appsettings or environment.");
}
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "localhost",
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "localhost"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/missionChatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Database
var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);

var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "database.dat");
Console.WriteLine($"File '{dbPath}' will be used as database file.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}")
);

// Event dispatcher
builder.Services.AddScoped<DomainEventDispatcher>();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserRefreshTokenRepository, UserRefreshTokenRepository>();
builder.Services.AddScoped<IMissionRepository, MissionRepository>();
builder.Services.AddScoped<IMissionChatRepository, MissionChatRepository>();
builder.Services.AddScoped<IMissionLastVisitRepository, MissionLastVisitRepository>();

// Register services
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IMissionChatService, MissionChatService>();
builder.Services.AddScoped<IFilesService, FilesService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IMissionLastVisitService, MissionLastVisitService>();

// Event service handlers
builder.Services.AddScoped<IDomainEventHandler<CreateMissionChatEvent>, MissionService>();
builder.Services.AddScoped<IDomainEventHandler<CreateMissionEvent>, MissionLastVisitService>();
builder.Services.AddScoped<IDomainEventHandler<UpdateMissionEvent>, MissionLastVisitService>();

// Register SignalR hub
builder.Services.AddSignalR();

var app = builder.Build();

// Cors
app.UseCors("AllowAngularApp");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Map SignalR hubs
app.MapHub<MissionChatHub>("/missionChatHub");

app.Run();

using System.Text;
using Backend.Infrastructure.Persistence;
using Backend.Modules.Identity.Interfaces;
using Backend.Modules.Identity.Models;
using Backend.Modules.Identity.Repositories;
using Backend.Modules.Identity.Services;
using Backend.Modules.Tenants.Interfaces;
using Backend.Modules.Tenants.Repositories;
using Backend.Modules.Tenants.Services;
using Backend.Modules.Tenants.Tests;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Repositories;
using Backend.Modules.Systems.Services;
using Backend.Modules.Systems.Tests;
using Backend.Modules.Dependencies.Interfaces;
using Backend.Modules.Dependencies.Repositories;
using Backend.Modules.Dependencies.Services;
using Backend.Modules.Dependencies.Tests;
using Backend.Modules.Graph.Interfaces;
using Backend.Modules.Graph.Services;
using Backend.Modules.Graph.Tests;
using Backend.Modules.Operations.Interfaces;
using Backend.Modules.Operations.Repositories;
using Backend.Modules.Operations.Services;
using Backend.Modules.Operations.Tests;
using Backend.Modules.ImpactAnalysis.Interfaces;
using Backend.Modules.ImpactAnalysis.Services;
using Backend.Modules.ImpactAnalysis.Tests;
using Backend.Modules.Incidents.Interfaces;
using Backend.Modules.Incidents.Repositories;
using Backend.Modules.Incidents.Services;
using Backend.Modules.Incidents.Tests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Contains("--verify-onboarding"))
            {
                var verifyExit = Backend.Modules.Identity.Tests.VerifyLegacyUserOnboardingLive.Run();
                Environment.Exit(verifyExit);
                return;
            }

            if (args.Contains("--run-tests"))
            {
                var tenantExit = TenantIsolationTests.RunAllTests();
                var systemsExit = LogisticsSystemTests.RunAllTests();
                var depExit = SystemDependencyTests.RunAllTests();
                var graphExit = GraphEngineTests.RunAllTests();
                var opsExit = BusinessOperationTests.RunAllTests();
                var impactExit = ImpactAnalysisTests.RunAllTests();
                var incidentExit = IncidentTests.RunAllTests();
                var exitCode = (tenantExit == 0 && systemsExit == 0 && depExit == 0 && graphExit == 0 && opsExit == 0 && impactExit == 0 && incidentExit == 0) ? 0 : 1;
                Environment.Exit(exitCode);
                return;
            }

            var builder = WebApplication.CreateBuilder(args);
            var connectionstring = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionstring))
            {
                throw new InvalidOperationException("Database connection string missing.");
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseMySQL(connectionstring, mysqlOptions =>
                {
                    mysqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });
            });

            // HTTP Context and Multi-Tenancy
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ITenantContext, TenantContext>();
            builder.Services.AddScoped<ITenantRepository, TenantRepository>();

            // Identity module services
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            // Systems module services
            builder.Services.AddScoped<ILogisticsSystemRepository, LogisticsSystemRepository>();
            builder.Services.AddScoped<ILogisticsSystemService, LogisticsSystemService>();

            // Dependencies module services
            builder.Services.AddScoped<ISystemDependencyRepository, SystemDependencyRepository>();
            builder.Services.AddScoped<ISystemDependencyService, SystemDependencyService>();

            // Graph module services
            builder.Services.AddScoped<IGraphEngineService, GraphEngineService>();

            // Operations module services
            builder.Services.AddScoped<IBusinessOperationRepository, BusinessOperationRepository>();
            builder.Services.AddScoped<IBusinessOperationService, BusinessOperationService>();

            // Impact Analysis module services
            builder.Services.AddScoped<IImpactAnalysisService, ImpactAnalysisService>();

            // Incident Management module services
            builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
            builder.Services.AddScoped<IIncidentService, IncidentService>();

            // JWT Bearer Authentication
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is not configured.");
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException("JWT Key must contain at least 32 bytes.");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]),
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]),
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();

            // OpenAPI / Swagger with JWT Bearer support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ResilienceX API",
                    Version = "v1",
                    Description = "Cyber-Continuity platform for Third-Party Logistics (3PLs)"
                });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT Bearer token."
                };

                options.AddSecurityDefinition("Bearer", securityScheme);

                options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer"),
                        new List<string>()
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

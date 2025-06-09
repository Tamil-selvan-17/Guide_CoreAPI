using Domain.Interface;
using Entity.Models;
using GuidProject.BAL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Repository.Authentication;
using Repository.Services;
using StackExchange.Redis;
using System.Net.Http;
using System.Text;

namespace GuidProject.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();// ✅ Add this line to register controllers

            services.ConfigureAuthentication(configuration);
            services.ConfigureCors();
            services.ConfigureSwagger();
            services.ConfigureDataProtection(configuration);
            services.ConfigureDatabase(configuration);
            services.ConfigureRedis(configuration);
            services.ConfigureHttpPolicies();
            services.RegisterServices();

            // ✅ Add Authorization Service
            services.AddAuthorization();
        }

        private static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = !string.IsNullOrEmpty(jwtSettings["Issuer"]),
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = !string.IsNullOrEmpty(jwtSettings["Audience"]),
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // ✅ Prevents accepting expired tokens

                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["AuthToken"];
                        return Task.CompletedTask;
                    }
                };
            });
        }

        private static void ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", builder =>
                {
                    builder.WithOrigins("http://localhost:4200", "https://localhost:1010", "http://localhost:5001", "http://localhost:5001")
                           .AllowCredentials()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });
        }

        private static void ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "JWT API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer {token}'"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] {}
                    }
                });
            });
        }

        private static void ConfigureDataProtection(this IServiceCollection services, IConfiguration configuration)
        {
            string appName = configuration["DataProtection:ApplicationName"] ?? "DefaultApp";

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "KeyStore")))
                .SetApplicationName(appName)
                .SetDefaultKeyLifetime(TimeSpan.FromDays(365));
        }

        private static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<your_DEVContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DbConnString"))
               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)); // ✅ Improve performance for read-only queries
        }

        private static void ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
        {
            string redisConnectionString = configuration.GetConnectionString("RedisCache");

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "GuidProject_";
            });
        }

        private static void ConfigureHttpPolicies(this IServiceCollection services)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1));

            services.AddHttpClient("ExternalAPI")
                .AddPolicyHandler(retryPolicy)
                .AddPolicyHandler(circuitBreakerPolicy);
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IAuthentication, AuthenticationRepo>();
            services.AddScoped<AuthenticationBL>();
            services.AddScoped<EncryptionService>();
            services.AddScoped<JwtService>();
            // ✅ Register ExceptionHandler as Singleton
            services.AddSingleton<ExceptionHandler>();
        }
    }
}

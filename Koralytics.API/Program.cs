using AutoMapper;

using FluentValidation;
using FluentValidation.AspNetCore;

using Koralytics.API.Middlewares;
using Koralytics.Application;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Mappings.Auth;
using Koralytics.Application.Services.Auth.Login;
using Koralytics.Application.Services.Auth.Register;
using Koralytics.Application.Validators.Auth;
using Koralytics.Application.Validators.UserBusiness;
using Koralytics.Domain.Entities;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Infrastructure.Context;
using Koralytics.Infrastructure.Repositories;
using Koralytics.Infrastructure.Seeding;
using Koralytics.Infrastructure.UnitOfWork;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using System.Text;

namespace Koralytics.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();



            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT signing key is not configured. Set 'Jwt:Key' in configuration before starting the app.");
            }

            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];
            var clockSkew = int.TryParse(builder.Configuration["Jwt:ClockSkewMinutes"], out var parsedClockSkew) ? parsedClockSkew : 1;



            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.FromMinutes(clockSkew)
                    };
                });


            builder.Services.AddAuthorization();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRegistrationService, RegistrationService>();

            // Register FluentValidation validators
            builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<BaseRegisterationRequestValidator>();
            builder.Services.AddScoped<IUserBusinessValidator, UserBusinessValidator>();

            builder.Services
                .AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

            // Register mapping profiles
            builder.Services.AddAutoMapper(op => op.AddProfile<RegisterProfile>());


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Host.UseSerilog((context, configuration) =>
                configuration
                    .WriteTo.Console()
                    .WriteTo.File(
                        "Logs/log-.txt",
                        rollingInterval: RollingInterval.Day));

            var app = builder.Build();

            app.UseExceptionHandler();

            // Seed database
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var userManager = services.GetRequiredService<UserManager<User>>();
                    var roleManager = services.GetRequiredService<RoleManager<Role>>();
                    await DbInitializer.SeedAsync(context, userManager, roleManager);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Koralytics API v1");
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
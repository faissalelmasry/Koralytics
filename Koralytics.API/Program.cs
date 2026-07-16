using AutoMapper;
using Amazon.S3;
using FluentValidation;
using FluentValidation.AspNetCore;
using Koralytics.API.Middlewares;
using Koralytics.Application;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Auth;
using Koralytics.Application.Services.Auth.Token;
using Koralytics.Application.Services.Auth.OAuth;
using Koralytics.Application.Options;
using Koralytics.Infrastructure.ExternalServices;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Application.Interfaces.Tournaments;
using Koralytics.Application.Mappings.Auth;
using Koralytics.Application.Mappings.Player;
using Koralytics.Application.Mappings.Tournaments;
using Koralytics.Application.Mappings.Drills;
using Koralytics.Application.Services.Auth.Login;
using Koralytics.Application.Services.Auth.Register;
using Koralytics.Application.Services.Coach.CoachAccessService;
using Koralytics.Application.Services.Coach.CoachNoteService;
using Koralytics.Application.Services.Coach.CoachSquadService;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Application.Services.Player.PlayerProfileServices;
using Koralytics.Application.Services.Player.PlayerGoalService;
using Koralytics.Application.Services.Drill.DrillAnalytic;
using Koralytics.Application.Services.Drill.DrillResult;
using Koralytics.Application.Services.Drill.DrillSession;
using Koralytics.Application.Services.Player.PlayerTransferService;
using Koralytics.Application.Validators.Auth;
using Koralytics.Application.Validators.Tournament;
using Koralytics.Application.Validators.UserBusiness;
using Koralytics.Domain.Entities;
using Koralytics.Application.Validators.Academies;
using Koralytics.Application.Mappings.Academies;
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
using Koralytics.Application.Services.Academy.AcademyService;
using Koralytics.Application.Services.Academy.AcademyTeamService;
using Koralytics.Application.Services.Academy.AcademyAnalyticsService;
using Koralytics.Application.Services.Academy.AcademyAnnouncementService;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Interfaces.Scouter;
using Koralytics.Application.Interfaces.ScouterInterfaces;
using Koralytics.Application.Mappings.ScouterProfile;
using Koralytics.Application.Services.Storage;
using Koralytics.Application.Options;
using Koralytics.API.Services;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Application.Services.Notification.AnnouncementNotificationService;
using Koralytics.Application.Services.Notification.PlayerNotificationService;
using Koralytics.Application.Services.Notification.ScouterNotificationService;
using Koralytics.Application.Interfaces.Email;
using Koralytics.Infrastructure.ExternalServices.Email;
using Koralytics.API.Hubs;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Application.Services.Match;
using Koralytics.Application.Validators.Match;
using Koralytics.Application.Mappings.Match;
using Koralytics.Application.Services.Drill.DrillTemplate;
using Koralytics.Application.Services.Tournaments;
using Koralytics.Application.Services.Scouter.ScouterFollowService;
using Koralytics.Application.Services.Scouter.ScouterReportService;
using Koralytics.Application.Services.Scouter.ScouterSearchService;
using Koralytics.Application.Services.Scouter.ScouterShortlistService;

namespace Koralytics.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

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
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Priority 1: Authorization header (Bearer token) — handled automatically by
                            // the JWT middleware; we only override when the header is absent.
                            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                // Let the built-in handler extract the token from the header.
                                return Task.CompletedTask;
                            }

                            // Priority 2: access_token cookie — used as a fallback when no header is sent.
                            var accessToken = context.Request.Cookies["access_token"];
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
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
            builder.Services.Configure<CloudflareR2Options>(
                builder.Configuration.GetSection(CloudflareR2Options.SectionName));
            
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection(EmailSettings.SectionName));
            builder.Services.AddSingleton<IEmailTemplateProvider, EmailTemplateProvider>();
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();

            builder.Services.AddSingleton<IAmazonS3>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var section = config.GetSection(CloudflareR2Options.SectionName);
                var accessKey = section["AccessKeyId"]
                    ?? throw new InvalidOperationException("CloudflareR2:AccessKeyId is missing from configuration.");
                var secretKey = section["SecretAccessKey"]
                    ?? throw new InvalidOperationException("CloudflareR2:SecretAccessKey is missing from configuration.");
                var endpoint = section["Endpoint"]
                    ?? throw new InvalidOperationException("CloudflareR2:Endpoint is missing from configuration.");

                var s3Config = new AmazonS3Config
                {
                    ServiceURL = endpoint,
                    ForcePathStyle = true
                };
                return new AmazonS3Client(accessKey, secretKey, s3Config);
            });

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRegistrationService, RegistrationService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<ICookieService, CookieService>();
            builder.Services.AddScoped<IOAuthProvider, GoogleOAuthProvider>();
            builder.Services.AddScoped<IOAuthProviderFactory, OAuthProviderFactory>();
            builder.Services.AddScoped<IPlayerTransferService, PlayerTransferService>();
            builder.Services.AddScoped<IDrillResultService, DrillResultService>();
            builder.Services.AddScoped<IDrillTemplateService, DrillTemplateService>();
            builder.Services.AddScoped<IDrillSessionService, DrillSessionService>();
            builder.Services.AddScoped<IDrillAnalyticsService, DrillAnalyticsService>();
            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<ITournamentDrawService, TournamentDrawService>();
            builder.Services.AddScoped<ITournamentFixtureService, TournamentFixtureService>();
            builder.Services.AddScoped<ITournamentReportService, TournamentReportService>();
            builder.Services.AddScoped<IPlayerCardService, PlayerCardService>();
            builder.Services.AddScoped<IPlayerProfileService, PlayerProfileService>();
            builder.Services.AddScoped<IPlayerGoalService, PlayerGoalService>();
            builder.Services.AddScoped<IAcademyService, AcademyService>();
            builder.Services.AddScoped<IAcademyTeamService, AcademyTeamService>();
            builder.Services.AddScoped<IAcademyAnalyticsService, AcademyAnalyticsService>();
            builder.Services.AddScoped<IAcademyAnnouncementService, AcademyAnnouncementService>();
            builder.Services.AddScoped<ICoachSquadService, CoachSquadService>();
            builder.Services.AddScoped<ICoachNoteService, CoachNoteService>();
            builder.Services.AddScoped<ICoachAccessService, CoachAccessService>();
            builder.Services.AddScoped<IMatchService, MatchService>();
            builder.Services.AddScoped<IMatchEventService, MatchEventService>();
            builder.Services.AddScoped<IMatchRatingService, MatchRatingService>();
            builder.Services.AddScoped<IMatchAnalyticsService, MatchAnalyticsService>();
            builder.Services.AddScoped<IMatchRequestService, MatchRequestService>();
            builder.Services.AddSingleton<CardInvalidationList>();
            builder.Services.AddSingleton<ICardInvalidationList>(sp => sp.GetRequiredService<CardInvalidationList>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<CardInvalidationList>());
            builder.Services.AddScoped<IScouterSearchService, ScouterSearchService>();
            builder.Services.AddScoped<IScouterShortlistService, ScouterShortlistService>();
            builder.Services.AddScoped<IScouterFollowService, ScouterFollowService>();
            builder.Services.AddScoped<IScouterReportService, ScouterReportService>();
            builder.Services.AddScoped<IStorageService, StorageService>();
            builder.Services.AddSignalR();
            builder.Services.AddScoped<IRealTimeBridge, RealTimeBridge>();
            builder.Services.AddScoped<IPlayerNotificationService, PlayerNotificationService>();
            builder.Services.AddScoped<IScouterNotificationService, ScouterNotificationService>();
            builder.Services.AddScoped<IAnnouncementNotificationService, AnnouncementNotificationService>();

            // Register FluentValidation validators
            builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<BaseRegisterationRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateTournamentValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<RegisterSquadValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateAcademyValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateAcademyValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<AddLocationValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateAgeGroupValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateTeamValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<SendAnnouncementValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateFriendlyMatchValidator>();
            builder.Services.AddScoped<IUserBusinessValidator, UserBusinessValidator>();

            builder.Services
                .AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

            // Register mapping profiles
            builder.Services.AddAutoMapper(op =>
            {
                op.AddProfile<RegisterProfile>();
                op.AddProfile<DrillMappingProfile>(); // <-- This is the one fixing the 500 Error
                op.AddProfile<TournamentProfile>();
                op.AddProfile<AcademyProfile>();
                op.AddProfile<PlayerProfile>();
                op.AddProfile<ScouterProfile>();
            });
            builder.Services.AddAutoMapper(op => op.AddProfile<RegisterProfile>());
            builder.Services.AddAutoMapper(op => op.AddProfile<TournamentProfile>());
            builder.Services.AddAutoMapper(op => op.AddProfile<AcademyProfile>());
            builder.Services.AddAutoMapper(op => op.AddProfile<PlayerProfile>());
            builder.Services.AddAutoMapper(op => op.AddProfile<MatchProfile>());
            builder.Services.AddAutoMapper(op=>op.AddProfile<ScouterProfile>());

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

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<NotificationHub>("/hubs/notifications");

            app.MapControllers();

            app.Run();
        }
    }
}
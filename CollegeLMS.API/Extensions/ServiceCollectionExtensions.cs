using System.Text;
using System.Text.Json.Serialization;
using CollegeLMS.API.Data;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace CollegeLMS.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(config.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention()
        );

        return services;
    }

    public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Key"]!)
                    ),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                };
            });
        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddSwaggerWithBearer(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "CollegeLMS API",
                    Version = "v1",
                    Description =
                        "API для управления учебным заведением. Все эндпоинты требуют JWT-аутентификацию.",
                }
            );

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            c.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Введите JWT токен. Пример: eyJhbGciOiJIUzI1NiIs...",
                }
            );
            c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer"), [] },
            });
        });

        return services;
    }

    public static IServiceCollection AddCorsFrontend(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.AddCors(o =>
        {
            o.AddPolicy(
                "AllowFrontend",
                p =>
                    p.WithOrigins(
                            config.GetSection("Cors:Origins").Get<string[]>()
                                ?? ["http://localhost:3000"]
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
            );
        });

        return services;
    }

    public static IServiceCollection AddJsonSerializer(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System
                .Text
                .Json
                .JsonNamingPolicy
                .CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = System
                .Text
                .Json
                .Serialization
                .JsonIgnoreCondition
                .WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System
                    .Text
                    .Json
                    .JsonNamingPolicy
                    .CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System
                    .Text
                    .Json
                    .Serialization
                    .JsonIgnoreCondition
                    .WhenWritingNull;
                options.JsonSerializerOptions.ReferenceHandler = System
                    .Text
                    .Json
                    .Serialization
                    .ReferenceHandler
                    .IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ILectureService, LectureService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<IWordPressImportService, WordPressImportService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ScheduleExportService>();
        services.AddScoped<ScheduleImportService>();
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }

    public static IServiceCollection AddHealthChecksWithDb(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.AddHealthChecks().AddNpgSql(config.GetConnectionString("DefaultConnection")!);

        return services;
    }
}

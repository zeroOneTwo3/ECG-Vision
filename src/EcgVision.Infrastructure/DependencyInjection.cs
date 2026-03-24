using System.Text;

using Amazon.Runtime;
using Amazon.S3;

using EcgVision.Core.Constants;
using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Interfaces;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Infrastructure.Concurrency;
using EcgVision.Infrastructure.Configuration;
using EcgVision.Infrastructure.Data;
using EcgVision.Infrastructure.Data.Repositories;
using EcgVision.Infrastructure.ExternalServices;
using EcgVision.Infrastructure.FileManagement;
using EcgVision.Infrastructure.Graphics;
using EcgVision.Infrastructure.Security;
using EcgVision.Infrastructure.Services;
using EcgVision.Infrastructure.TaskHandlers;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EcgVision.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSettings = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtSettings);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ClockSkew = TimeSpan.Zero
            });

        services.AddSingleton<IAuthorizationHandler, ViewJobAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppConstants.ViewJobPolicy, policy =>
                policy.Requirements.Add(new ViewJobRequirement()));
        });

        return services;
    }
    public static IServiceCollection AddEcgInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DB Configuration
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Add Identity Services
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = AppConstants.MinPasswordLength;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager<SignInManager<ApplicationUser>>()
        .AddDefaultTokenProviders();

        services.AddOptions<PythonOptions>()
                .Bind(configuration.GetSection(PythonOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(options =>
                {
                    bool isPythonValid = File.Exists(options.PythonExe);
                    string fullPath = Path.GetFullPath(options.ScriptFolder);
                    bool isFolderValid = Directory.Exists(fullPath);

                    return isPythonValid && isFolderValid;
                }, "The specified Python executable or Script folder does not exist on the host system.")
                .ValidateOnStart();

        services.AddOptions<S3Options>()
                .Bind(configuration.GetSection(S3Options.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<StorageOptions>()
                .Bind(configuration.GetSection(StorageOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opt = configuration.GetSection(S3Options.SectionName).Get<S3Options>()!;
            return new AmazonS3Client(opt.AccessKey, opt.SecretKey, new AmazonS3Config
            {
                ServiceURL = opt.ServiceUrl,
                AuthenticationRegion = opt.Region,
                ForcePathStyle = true,
                DefaultConfigurationMode = DefaultConfigurationMode.Standard,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
        });

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEcgSignalRepository, EcgSignalRepository>();
        services.AddScoped<IEcgJobRepository, EcgJobRepository>();

        // Services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IEcgJobService, EcgJobService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddScoped<IS3Client, S3Client>();
        services.AddScoped<IRawEcgFilesManager, RawEcgFilesManager>();
        services.AddScoped<IEcgSignalParser, CliEcgSignalParser>();
        services.AddSingleton<IEcgImageGenerator, EcgImageGenerator>();

        services.AddScoped<IStorageService, LocalStorageService>();
        services.AddScoped<IStorageService, S3StorageService>();
        services.AddScoped<IStorageFactory, StorageFactory>();
        services.AddEcgTaskHandlers();

        return services;
    }

    private static IServiceCollection AddEcgTaskHandlers(this IServiceCollection services)
    {
        // 1. Get the assembly where your handlers live
        var assembly = typeof(EcgPlotGenerationHandler).Assembly;

        // 2. Find all types that implement IEcgTaskHandler<>
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.GetInterfaces().Any(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IEcgTaskHandler<>)));

        foreach (var type in handlerTypes)
        {
            // 3. Register the interface to the concrete implementation
            var interfaceType = type.GetInterfaces()
                .First(i => i.GetGenericTypeDefinition() == typeof(IEcgTaskHandler<>));

            services.AddScoped(interfaceType, type);
        }

        return services;
    }
}
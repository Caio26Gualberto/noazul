using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NoAzul.Application.Abstractions.Authorization;
using NoAzul.Application.Abstractions.Jobs;
using NoAzul.Application.Abstractions.Logging;
using NoAzul.Application.Abstractions.Repository;
using NoAzul.Application.Abstractions.Repository.UnitOfWork;
using NoAzul.Application.AuditLogs;
using NoAzul.Application.AuditLogs.Interfaces;
using NoAzul.Application.Auth;
using NoAzul.Application.Auth.Interfaces;
using NoAzul.Application.Authorizations;
using NoAzul.Application.Authorizations.Interfaces;
using NoAzul.Application.BackgroundJobs;
using NoAzul.Application.BackgroundJobs.Handlers;
using NoAzul.Application.BackgroundJobs.Interfaces;
using NoAzul.Application.Common.Time;
using NoAzul.Application.Interfaces;
using NoAzul.Application.Items;
using NoAzul.Application.Items.Interfaces;
using NoAzul.Application.Profiles;
using NoAzul.Application.Profiles.Interfaces;
using NoAzul.Application.Users;
using NoAzul.Application.Users.Interfaces;
using NoAzul.Domain.Constants;
using NoAzul.Domain.Interfaces.Authenticate;
using NoAzul.Infra.Data.Auditing;
using NoAzul.Infra.Data.Cache;
using NoAzul.Infra.Data.Context;
using NoAzul.Infra.Data.Context.Seeding;
using NoAzul.Infra.Data.Email;
using NoAzul.Infra.Data.Identity;
using NoAzul.Infra.Data.Identity.AuthenticateService;
using NoAzul.Infra.Data.Identity.Authorization;
using NoAzul.Infra.Data.Logging;
using NoAzul.Infra.Data.Repositories;
using NoAzul.Infra.Data.Repositories.AuditLogs;
using NoAzul.Infra.Data.Repositories.BackgroundJobRuns;
using NoAzul.Infra.Data.Repositories.Items;
using NoAzul.Infra.Data.Repositories.UnitOfWork;
using NoAzul.Workers.Dispatch;
using NoAzul.Workers.Monitoring;
using NoAzul.Workers.Triggers.Enqueued.BackgroundJobs;
using NoAzul.Workers.Triggers.Enqueued.Items;
using NoAzul.Workers.Triggers.Recurring.Items;
using NoAzul.Workers.Wrappers.Recurring.Items;

namespace NoAzul.Infra.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration config)
        {
            AppTime.Configure(config["App:TimeZone"], config["App:WindowsTimeZone"]);

            // Logging
            services.AddSingleton(sp => new ApplicationLogMetadata(
                config["App:Name"] ?? ApplicationLogMetadata.DefaultApplication,
                sp.GetRequiredService<IHostEnvironment>().EnvironmentName));
            if (string.Equals(config["Logging:ApplicationLog:Format"], "Json", StringComparison.OrdinalIgnoreCase))
                services.AddSingleton<IApplicationLogWriter, ConsoleApplicationLogWriter>();
            else
                services.AddSingleton<IApplicationLogWriter, TextApplicationLogWriter>();
            services.AddScoped<ILogContextStore, LogContextStore>();
            services.AddScoped<IApplicationLoggerFactory, ApplicationLoggerFactory>();

            // Data
            services.AddScoped<AuditSaveChangesInterceptor>();

            services.AddDbContext<AppDbContext>((sp, options) =>
                DatabaseProvider.Configure(options, config)
                    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

            // Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            var jwtSettings = config.GetSection("JWT");
            var secretKey = jwtSettings["SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException(
                    "JWT:SecretKey não configurado. Defina a variável de ambiente JWT__SecretKey.");

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = JwtAudiences.Api,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization();
            services.AddHttpContextAccessor();

            // Cache
            services.AddHybridCache();
            services.AddScoped<ICacheClient, CacheClient>();

            // Auth e acesso
            services.AddScoped<IAuthenticateService, AuthenticateService>();
            services.AddScoped<IAuthAppService, AuthAppService>();
            services.AddScoped<IProfileAppService, ProfileAppService>();
            services.AddScoped<IPermissionDefinitionProvider, PermissionDefinitionProvider>();
            services.AddScoped<IPermissionChecker, PermissionChecker>();
            services.AddScoped<IPermissionManager, PermissionManager>();
            services.AddScoped<IRoleManagementService, RoleManagementService>();
            services.AddScoped<IAuthorizationAppService, AuthorizationAppService>();
            services.AddScoped<IUserAppService, UserAppService>();
            services.AddScoped<IAuditLogAppService, AuditLogAppService>();
            services.AddScoped<SeedData>();

            // Services
            services.AddScoped<IItemAppService, ItemAppService>();

            // External services
            services.AddScoped<IEmailService, ConsoleEmailService>();

            // Jobs
            services.AddScoped<IRecurringJobReader, RecurringJobReader>();
            services.AddScoped<IEnqueuedJobDispatcher, EnqueuedJobDispatcher>();
            services.AddScoped<IBackgroundJobAppService, BackgroundJobAppService>();
            services.AddScoped<IBackgroundJobHandler, ItemsSnapshotJobHandler>();
            services.AddScoped<BackgroundJobsEnqueuedTrigger>();
            services.AddScoped<ItemsEnqueuedJobsTrigger>();
            services.AddScoped<ItemRecurringJobsTrigger>();
            services.AddScoped<ItemRecurringJobsWrapper>();

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IBackgroundJobRunRepository, BackgroundJobRunRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();

            return services;
        }
    }
}

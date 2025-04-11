using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Content.Server.Database;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using SS14.Admin.Data;
using SS14.Admin.Helpers;
using SS14.Admin.JobSystem;
using SS14.Admin.PersonalData;
using SS14.Admin.SignIn;
using SS14.Admin.Storage;

namespace SS14.Admin
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<SignInManager>();
            services.AddScoped<LoginHandler>();
            services.AddScoped<BanHelper>();
            services.AddScoped<PlayerLocator>();
            services.AddHttpContextAccessor();

            var connStr = Configuration.GetConnectionString("DefaultConnection");
            if (connStr == null)
                throw new InvalidOperationException("Need to specify DefaultConnection connection string");

            var adminConnStr = Configuration.GetConnectionString("AdminConnection");
            if (adminConnStr == null)
                throw new InvalidOperationException("Need to specify AdminConnection connection string");

            services.AddDbContext<PostgresServerDbContext>(options => options.UseNpgsql(connStr));
            services.AddDbContext<AdminDbContext>(options => options.UseNpgsql(adminConnStr));

            services.AddControllers();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/Players");
                options.Conventions.AuthorizeFolder("/Connections");
                options.Conventions.AuthorizeFolder("/Bans");
                options.Conventions.AuthorizeFolder("/RoleBans");
                options.Conventions.AuthorizeFolder("/Logs");
                options.Conventions.AuthorizeFolder("/Characters");
                options.Conventions.AuthorizeFolder("/Whitelist");
            });

            services.AddScoped<PersonalDataDownloader>();

            services.AddAuthorizationBuilder()
                .AddPolicy(Constants.PolicyPersonalDataManagement, policy => policy.RequireRole(Constants.HostRole));

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies", options => { options.ExpireTimeSpan = TimeSpan.FromHours(1); })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.SignInScheme = "Cookies";

                    options.Authority = Configuration["Auth:Authority"];
                    options.ClientId = Configuration["Auth:ClientId"];
                    options.ClientSecret = Configuration["Auth:ClientSecret"];
                    options.SaveTokens = true;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.Events.OnTokenValidated = async ctx =>
                    {
                        var handler = ctx.HttpContext.RequestServices.GetRequiredService<LoginHandler>();
                        await handler.HandleTokenValidated(ctx);
                    };
                });

            services.Configure<JobSystemConfiguration>(cfg =>
            {
                cfg.RegisterIntervalJob<DeleteCollectedPersonalDataJob>(TimeSpan.FromHours(1));
                cfg.RegisterJob<CollectPersonalDataJob>();
            });

            services.AddHostedService<IntervalJobService>();
            services.AddSingleton<JobSchedulerService>();
            services.AddSingleton<IJobScheduler>(p => p.GetRequiredService<JobSchedulerService>());

            SetupStorage<StorageTemp>(services, StorageTemp.Position);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSerilogRequestLogging();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            };

            foreach (var ip in Configuration.GetSection("ForwardProxies").Get<string[]>() ?? Array.Empty<string>())
            {
                forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(ip));
            }

            app.UseForwardedHeaders(forwardedHeadersOptions);

            var pathBase = Configuration.GetValue<string>("PathBase");
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }

        private void SetupStorage<TStorageType>(IServiceCollection serviceCollection, string position)
            where TStorageType : StorageType
        {
            var section = Configuration.GetSection(position);
            var options = section.Get<StorageOptions>();
            if (options == null)
                throw new Exception($"Missing {position} storage configuration");

            switch (options.Type)
            {
                case StorageBackendType.Local:
                    if (options.Local is not { } local)
                        throw new Exception("No valid local storage config!");

                    // Can you believe this is real code?
                    serviceCollection.AddSingleton<IStorageManager<TStorageType>>(services =>
                        new LocalStorageManager<TStorageType>(local,
                            services.GetRequiredService<ILogger<LocalStorageManager<TStorageType>>>()));
                    break;
                case StorageBackendType.S3:
                    if (options.S3 is not { } s3)
                        throw new Exception("No valid S3 storage config!");

                    var clientConfig = Configuration.GetAWSOptions<AmazonS3Config>(
                        $"{position}:{nameof(StorageOptions.S3)}:{StorageOptionsS3.LocationS3Config}");
                    var credentials = new BasicAWSCredentials(s3.AccessKey, s3.SecretKey);

                    var client = new AmazonS3Client(credentials, (AmazonS3Config) clientConfig.DefaultClientConfig);

                    serviceCollection.AddSingleton<IStorageManager<TStorageType>>(services =>
                        new S3StorageManager<TStorageType>(client,
                            s3.BucketName,
                            services.GetRequiredService<ILogger<S3StorageManager<TStorageType>>>()));
                    break;
                default:
                    throw new Exception($"Missing {position} storage backend type");
            }
        }
    }
}

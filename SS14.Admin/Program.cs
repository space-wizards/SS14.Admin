using Microsoft.EntityFrameworkCore;
using Serilog;
using SS14.Admin.Data;

namespace SS14.Admin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = CreateHostBuilder(args).Build();

            using (var serviceScope = app.Services.CreateScope())
            {
                using var db = serviceScope.ServiceProvider.GetRequiredService<AdminDbContext>();
                db.Database.Migrate();
            }

            app.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var env = context.HostingEnvironment;
                    builder.AddYamlFile("appsettings.yml", false, true);
                    builder.AddYamlFile($"appsettings.{env.EnvironmentName}.yml", true, true);
                    builder.AddYamlFile("appsettings.Secret.yml", true, true);
                })
                .UseSerilog((ctx, cfg) =>
                {
                    cfg.ReadFrom.Configuration(ctx.Configuration);
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseSystemd();
    }
}


using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using Microsoft.IO;
using Osekai.Octon.WebServer;
using Osekai.Octon;
using Osekai.Octon.Caching.MsgPack;
using Osekai.Octon.OsuApi;
using Osekai.Octon.Database;
using Osekai.Octon.Database.EntityFramework;
using Osekai.Octon.Database.EntityFramework.Repositories;
using Osekai.Octon.Database.Repositories;
using Osekai.Octon.Options;
using Osekai.Octon.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<OsuOAuthConfiguration>(builder.Configuration.GetSection("OsuOAuthConfiguration"));

builder.Services.AddDbContext<MySqlOsekaiDbContext>(options => 
    options.UseMySql(builder.Configuration.GetConnectionString("MySql")!, MySqlServerVersion.LatestSupportedServerVersion, 
        sqlOptions => sqlOptions.MigrationsAssembly("Osekai.Octon.Database.EntityFramework.MySql.Migrations")));

builder.Services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
builder.Services.AddSingleton<ObjectPool<StringBuilder>>(serviceProvider =>
{
    var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
    var policy = new StringBuilderPooledObjectPolicy();
    return provider.Create(policy);
});

builder.Services.AddSingleton<RecyclableMemoryStreamManager>();
builder.Services.AddTransient<ICache, MsgPackDatabaseCache>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<OsuApiTimeThrottler>();
builder.Services.AddScoped<OsuApiV2Interface>();
builder.Services.AddScoped<IAuthenticatedOsuApiV2Interface, AuthenticatedOsuApiV2Interface>();
builder.Services.AddScoped<CachedAuthenticatedOsuApiV2Interface>();
builder.Services.AddScoped<AuthenticatedOsuApiV2Interface>();
builder.Services.AddScoped<CurrentSession>();
builder.Services.AddScoped<IOsuApiV2TokenProvider>(provider => provider.GetService<CurrentSession>()!);
builder.Services.AddScoped<DbContext>(provider => provider.GetService<MySqlOsekaiDbContext>()!);
builder.Services.AddScoped<ITransactionProvider, EntityFrameworkTransactionProvider>();
builder.Services.AddTransient<IDatabaseUnitOfWork, MySqlDatabaseUnitOfWork>();
builder.Services.AddScoped<ITokenGenerator, RandomBytes128BitTokenGenerator>();
builder.Services.AddSingleton<StaticUrlGenerator>();
builder.Services.AddScoped<AuthenticationService>();

builder.Services.AddMemoryCache();

#if DEBUG
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddScoped<ITestDataPopulator, MySqlTestDataPopulator>();
#else
builder.Services.AddRazorPages();
#endif

builder.Services.AddRouting();

var app = builder.Build();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

IServiceScope scope = app.Services.CreateScope();

await using (var context = scope.ServiceProvider.GetService<MySqlOsekaiDbContext>()!)
    await context.Database.MigrateAsync();

await app.RunAsync();
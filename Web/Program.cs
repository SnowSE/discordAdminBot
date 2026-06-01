using System.Net.Http.Headers;
using dotenv.net;
using Web;
using Web.Components;
using Web.Services;

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

var builder = WebApplication.CreateBuilder(args);

var appConfig = AppConfig.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(appConfig);

builder.Services.AddTransient<HttpErrorHandler>();

builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
{
  clientBuilder.AddHttpMessageHandler<HttpErrorHandler>();
});

builder.Services.AddHttpClient(
  "discord",
  client =>
  {
    client.BaseAddress = new Uri("https://discord.com/api/v10/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
      "Bot",
      appConfig.DiscordBotToken
    );
    client.DefaultRequestHeaders.Add("User-Agent", "DiscordAdminBot/1.0 (ASP.NET Core)");
  }
);

builder.Services.AddHttpClient(
  "snow",
  client =>
  {
    client.BaseAddress = new Uri("https://my.snow.edu/api/");
  }
);

builder.Services.AddSingleton<CacheDb>();
builder.Services.AddSingleton<DiscordDB>();
builder.Services.AddSingleton<DiscordAPI>();
builder.Services.AddSingleton<DiscordService>();
builder.Services.AddSingleton<SnowCourseDb>();
builder.Services.AddSingleton<SnowCourseService>();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

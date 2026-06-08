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

builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.Use(
  async (context, next) =>
  {
    if (context.Request.Path != "/")
    {
      await next(context);
      return;
    }

    var cloudflareEmailHeader = context
      .Request.Headers["Cf-Access-Authenticated-User-Email"]
      .ToString();
    var cloudflareJwtHeader = context.Request.Headers["Cf-Access-Jwt-Assertion"].ToString();

    if (string.IsNullOrWhiteSpace(cloudflareEmailHeader))
    {
      app.Logger.LogDebug(
        "Cloudflare Access email header was missing while preparing homepage identity display. Header {HeaderName}",
        "Cf-Access-Authenticated-User-Email"
      );
    }
    else
    {
      app.Logger.LogDebug(
        "Cloudflare Access email header received for homepage identity display. Header {HeaderName} value {HeaderValue}",
        "Cf-Access-Authenticated-User-Email",
        cloudflareEmailHeader
      );
    }

    if (string.IsNullOrWhiteSpace(cloudflareJwtHeader))
    {
      app.Logger.LogDebug(
        "Cloudflare Access JWT header was missing while inspecting Cloudflare identity formats. Header {HeaderName}",
        "Cf-Access-Jwt-Assertion"
      );
    }
    else
    {
      app.Logger.LogDebug(
        "Cloudflare Access JWT header received while inspecting Cloudflare identity formats. Header {HeaderName} characterCount {CharacterCount}",
        "Cf-Access-Jwt-Assertion",
        cloudflareJwtHeader.Length
      );
    }

    await next(context);
  }
);

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

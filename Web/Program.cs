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

app.Use(
  async (context, next) =>
  {
    var cloudflareEmailHeader = context
      .Request.Headers["Cf-Access-Authenticated-User-Email"]
      .ToString();
    var cloudflareJwtHeader = context.Request.Headers["Cf-Access-Jwt-Assertion"].ToString();

    var cloudflareHeaderValues = context
      .Request.Headers.Where(header =>
        header.Key.StartsWith("Cf-", StringComparison.OrdinalIgnoreCase)
      )
      .Select(header => $"{header.Key}={header.Value}")
      .ToList();

    app.Logger.LogInformation(
      "Cloudflare Access request inspection. Method {RequestMethod} scheme {RequestScheme} host {RequestHost} path {RequestPath} query {RequestQuery} remoteIp {RemoteIpAddress} forwardedFor {ForwardedFor} forwardedHost {ForwardedHost} forwardedProto {ForwardedProto} cloudflareHeaderCount {CloudflareHeaderCount} cloudflareHeaders {CloudflareHeaders}",
      context.Request.Method,
      context.Request.Scheme,
      context.Request.Host.ToString(),
      context.Request.Path,
      context.Request.QueryString.ToString(),
      context.Connection.RemoteIpAddress?.ToString() ?? "",
      context.Request.Headers["X-Forwarded-For"].ToString(),
      context.Request.Headers["X-Forwarded-Host"].ToString(),
      context.Request.Headers["X-Forwarded-Proto"].ToString(),
      cloudflareHeaderValues.Count,
      string.Join(" | ", cloudflareHeaderValues)
    );

    if (string.IsNullOrWhiteSpace(cloudflareEmailHeader))
    {
      app.Logger.LogInformation(
        "Cloudflare Access email header was missing while inspecting proxied request. Path {RequestPath} header {HeaderName}",
        context.Request.Path,
        "Cf-Access-Authenticated-User-Email"
      );
    }
    else
    {
      app.Logger.LogInformation(
        "Cloudflare Access email header was received while inspecting proxied request. Path {RequestPath} header {HeaderName} value {HeaderValue}",
        context.Request.Path,
        "Cf-Access-Authenticated-User-Email",
        cloudflareEmailHeader
      );
    }

    if (string.IsNullOrWhiteSpace(cloudflareJwtHeader))
    {
      app.Logger.LogInformation(
        "Cloudflare Access JWT header was missing while inspecting proxied request. Path {RequestPath} header {HeaderName}",
        context.Request.Path,
        "Cf-Access-Jwt-Assertion"
      );
    }
    else
    {
      app.Logger.LogInformation(
        "Cloudflare Access JWT header was received while inspecting proxied request. Path {RequestPath} header {HeaderName} characterCount {CharacterCount} value {HeaderValue}",
        context.Request.Path,
        "Cf-Access-Jwt-Assertion",
        cloudflareJwtHeader.Length,
        cloudflareJwtHeader
      );
    }

    await next(context);
  }
);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

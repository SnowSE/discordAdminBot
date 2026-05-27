using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Web.Services;

public class HttpErrorHandler(ILogger<HttpErrorHandler> logger) : DelegatingHandler
{
  private static readonly string[] SensitiveKeys =
  [
    "access_token",
    "token",
    "authorization",
    "password",
    "secret",
  ];

  private static readonly Regex JsonKeyValueRegex = new(
    "\"" + "(" + string.Join("|", SensitiveKeys) + ")" + "\"\\s*:\\s*\"([^\"]*)\"",
    RegexOptions.Compiled
  );

  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken ct
  )
  {
    var response = await base.SendAsync(request, ct);

    if (!response.IsSuccessStatusCode)
    {
      await ThrowWithDetails(request, response, ct);
    }

    return response;
  }

  private static async Task ThrowWithDetails(
    HttpRequestMessage request,
    HttpResponseMessage response,
    CancellationToken ct
  )
  {
    var redactedRequestBody = GetRedactedRequestBody(request, ct);
    var responseBody = await response.Content.ReadAsStringAsync(ct);

    throw new HttpRequestException(
      $"HTTP {request.Method} {(Uri?)request.RequestUri} failed with "
        + $"{(int)response.StatusCode} {response.ReasonPhrase ?? "(none)"}. "
        + $"Request: {redactedRequestBody}. Response: {responseBody}"
    );
  }

  private static string GetRedactedRequestBody(HttpRequestMessage request, CancellationToken ct) =>
    request.Content is not null
      ? RedactSensitiveData(request.Content.ReadAsStringAsync(ct).GetAwaiter().GetResult())
      : "(none)";

  private static string RedactSensitiveData(string body) =>
    JsonKeyValueRegex.Replace(body, m => $"\"{m.Groups[1].Value}\": \"***REDACTED***\"");
}

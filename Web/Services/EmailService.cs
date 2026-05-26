using Azure;
using Azure.Communication.Email;

namespace Web.Services;

public class EmailService
{
  private readonly EmailClient _client;
  private readonly string _senderAddress;

  public EmailService(AppConfig config)
  {
    _senderAddress = config.EmailSenderAddress;
    _client = new EmailClient(config.AzureCommunicationConnectionString);
  }

  public async Task SendAsync(
    string toAddress,
    string toName,
    string subject,
    string htmlBody,
    string? plainTextBody = null,
    CancellationToken ct = default
  )
  {
    var message = new EmailMessage(
      senderAddress: _senderAddress,
      recipients: new EmailRecipients([new EmailAddress(toAddress, toName)]),
      content: new EmailContent(subject) { Html = htmlBody, PlainText = plainTextBody }
    );

    await _client.SendAsync(WaitUntil.Started, message, ct);
  }
}

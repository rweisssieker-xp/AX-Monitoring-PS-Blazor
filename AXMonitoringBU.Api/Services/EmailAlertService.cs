using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IEmailAlertService
{
    Task<bool> SendAlertAsync(Alert alert, CancellationToken cancellationToken = default);
    Task<bool> SendDigestAsync(IEnumerable<Alert> alerts, string period = "hourly", CancellationToken cancellationToken = default);
}

public class EmailAlertService : IEmailAlertService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailAlertService> _logger;

    public EmailAlertService(IConfiguration configuration, ILogger<EmailAlertService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAlertAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            var recipients = GetRecipients(alert.Severity);
            if (!recipients.Any())
            {
                _logger.LogWarning("No recipients configured for severity {Severity}", alert.Severity);
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("AX Monitor", GetSenderEmail()));
            message.To.AddRange(recipients.Select(r => new MailboxAddress("", r)));
            message.Subject = CreateSubject(alert);

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = CreateHtmlContent(alert),
                TextBody = CreateTextContent(alert)
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var smtpServer = GetSmtpServer();
            var smtpPort = GetSmtpPort();
            var useTls = GetUseTls();

            await client.ConnectAsync(smtpServer, smtpPort, useTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            
            var senderPassword = GetSenderPassword();
            if (!string.IsNullOrEmpty(senderPassword))
            {
                await client.AuthenticateAsync(GetSenderEmail(), senderPassword, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email alert sent successfully for alert {AlertId}", alert.AlertId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email alert for {AlertId}", alert.AlertId);
            return false;
        }
    }

    public async Task<bool> SendDigestAsync(IEnumerable<Alert> alerts, string period = "hourly", CancellationToken cancellationToken = default)
    {
        try
        {
            var alertsList = alerts.ToList();
            if (!alertsList.Any())
            {
                return true;
            }

            var allRecipients = new HashSet<string>();
            foreach (var severity in new[] { "Critical", "Warning", "Info" })
            {
                foreach (var recipient in GetRecipients(severity))
                {
                    allRecipients.Add(recipient);
                }
            }

            if (!allRecipients.Any())
            {
                _logger.LogWarning("No recipients configured for digest");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("AX Monitor", GetSenderEmail()));
            message.To.AddRange(allRecipients.Select(r => new MailboxAddress("", r)));
            message.Subject = $"AX Monitor {period} Digest - {alertsList.Count} alerts";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = CreateDigestHtml(alertsList, period),
                TextBody = CreateDigestText(alertsList, period)
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var smtpServer = GetSmtpServer();
            var smtpPort = GetSmtpPort();
            var useTls = GetUseTls();

            await client.ConnectAsync(smtpServer, smtpPort, useTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            
            var senderPassword = GetSenderPassword();
            if (!string.IsNullOrEmpty(senderPassword))
            {
                await client.AuthenticateAsync(GetSenderEmail(), senderPassword, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email digest sent successfully with {Count} alerts", alertsList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email digest");
            return false;
        }
    }

    private List<string> GetRecipients(string severity)
    {
        var configKey = severity switch
        {
            "Critical" => "Alerting:Email:CriticalRecipients",
            "Warning" => "Alerting:Email:WarningRecipients",
            _ => "Alerting:Email:InfoRecipients"
        };

        var recipients = _configuration[configKey] ?? "";
        return recipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrEmpty(r))
            .ToList();
    }

    private string GetSenderEmail() => _configuration["Alerting:Email:SenderEmail"] ?? "monitor@company.com";
    private string GetSenderPassword() => _configuration["Alerting:Email:SenderPassword"] ?? "";
    private string GetSmtpServer() => _configuration["Alerting:Email:SmtpServer"] ?? "smtp.gmail.com";
    private int GetSmtpPort() => int.Parse(_configuration["Alerting:Email:SmtpPort"] ?? "587");
    private bool GetUseTls() => _configuration.GetValue<bool>("Alerting:Email:UseTls", true);

    private string CreateSubject(Alert alert)
    {
        return $"[{alert.Severity}] AX Monitor Alert: {alert.Type}";
    }

    private string CreateHtmlContent(Alert alert)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .alert-box {{ border-left: 4px solid {(alert.Severity == "Critical" ? "#dc3545" : alert.Severity == "Warning" ? "#ffc107" : "#17a2b8")}; padding: 15px; margin: 10px 0; background-color: #f8f9fa; }}
        .severity {{ font-weight: bold; color: {(alert.Severity == "Critical" ? "#dc3545" : alert.Severity == "Warning" ? "#ffc107" : "#17a2b8")}; }}
    </style>
</head>
<body>
    <h2>AX Monitor Alert</h2>
    <div class=""alert-box"">
        <p><strong>Type:</strong> {alert.Type}</p>
        <p><strong>Severity:</strong> <span class=""severity"">{alert.Severity}</span></p>
        <p><strong>Message:</strong> {alert.Message}</p>
        <p><strong>Timestamp:</strong> {alert.Timestamp:yyyy-MM-dd HH:mm:ss}</p>
        <p><strong>Alert ID:</strong> {alert.AlertId}</p>
    </div>
</body>
</html>";
    }

    private string CreateTextContent(Alert alert)
    {
        return $@"AX Monitor Alert

Type: {alert.Type}
Severity: {alert.Severity}
Message: {alert.Message}
Timestamp: {alert.Timestamp:yyyy-MM-dd HH:mm:ss}
Alert ID: {alert.AlertId}";
    }

    private string CreateDigestHtml(List<Alert> alerts, string period)
    {
        var criticalAlerts = alerts.Where(a => a.Severity == "Critical").ToList();
        var warningAlerts = alerts.Where(a => a.Severity == "Warning").ToList();
        var infoAlerts = alerts.Where(a => a.Severity == "Info").ToList();

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .summary {{ background-color: #f8f9fa; padding: 15px; margin: 10px 0; }}
        .alert-item {{ border-left: 4px solid #ccc; padding: 10px; margin: 5px 0; background-color: #fff; }}
        .critical {{ border-left-color: #dc3545; }}
        .warning {{ border-left-color: #ffc107; }}
        .info {{ border-left-color: #17a2b8; }}
    </style>
</head>
<body>
    <h2>AX Monitor {period} Digest</h2>
    <div class=""summary"">
        <p><strong>Total Alerts:</strong> {alerts.Count}</p>
        <p><strong>Critical:</strong> {criticalAlerts.Count}</p>
        <p><strong>Warning:</strong> {warningAlerts.Count}</p>
        <p><strong>Info:</strong> {infoAlerts.Count}</p>
    </div>
    <h3>Alerts</h3>
    {string.Join("", alerts.Select(a => $@"
    <div class=""alert-item {a.Severity.ToLower()}"">
        <p><strong>{a.Type}</strong> [{a.Severity}]</p>
        <p>{a.Message}</p>
        <p><small>{a.Timestamp:yyyy-MM-dd HH:mm:ss}</small></p>
    </div>"))}
</body>
</html>";
    }

    private string CreateDigestText(List<Alert> alerts, string period)
    {
        var criticalAlerts = alerts.Where(a => a.Severity == "Critical").ToList();
        var warningAlerts = alerts.Where(a => a.Severity == "Warning").ToList();
        var infoAlerts = alerts.Where(a => a.Severity == "Info").ToList();

        var text = $@"AX Monitor {period} Digest

Total Alerts: {alerts.Count}
Critical: {criticalAlerts.Count}
Warning: {warningAlerts.Count}
Info: {infoAlerts.Count}

Alerts:
{string.Join("\n", alerts.Select(a => $@"- [{a.Severity}] {a.Type}: {a.Message} ({a.Timestamp:yyyy-MM-dd HH:mm:ss})"))}";

        return text;
    }
}


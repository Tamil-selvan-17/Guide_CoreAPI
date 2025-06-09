using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data.Entity.Validation;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Repository.Services
{
    public class ExceptionHandler
    {
        private readonly string _senderEmail;
        private readonly string _senderPassword;
        private readonly string _recipientEmail;
        private readonly string _ccEmail;
        private readonly string _bccEmail;
        private readonly ILogger<ExceptionHandler> _logger;

        public ExceptionHandler(IConfiguration configuration, ILogger<ExceptionHandler> logger)
        {
            var emailConfig = configuration.GetSection("EmailConfiguration");
            _senderEmail = emailConfig["FromEmail"] ?? string.Empty;
            _senderPassword = emailConfig["AppPassword"] ?? string.Empty;
            _recipientEmail = emailConfig["TO"] ?? string.Empty;
            _ccEmail = emailConfig["CC"];
            _bccEmail = emailConfig["BCC"];
            _logger = logger;
        }

        public void HandleException(HttpContext context, Exception ex)
        {
            string module = context.Request.Path;
            string function = context.Request.Method;
            string errorMessage = FormatErrorMessage(ex);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                SendExceptionEmail(module, function, errorMessage);
            }
        }

        private string FormatErrorMessage(Exception ex)
        {
            return ex == null ? string.Empty :
                $"Exception: {ex.Message}<br/>Inner Exception: {ex.InnerException?.Message ?? "None"}<br/>Stack Trace: {ex.StackTrace}";
        }

        private void SendExceptionEmail(string module, string function, string errorDetails)
        {
            try
            {
                string emailBody = GenerateEmailTemplate(module, function, errorDetails, "exception.html");
                SendEmail(_senderEmail, _senderPassword, _recipientEmail, $"🚨 Exception Alert: {module}", emailBody, _ccEmail, _bccEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send exception email");
            }
        }

        private string GenerateEmailTemplate(string module, string function, string errorDetails, string templateName)
        {
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Source", "Template", templateName);
            if (!File.Exists(templatePath))
            {
                _logger.LogError("Email template not found at {Path}", templatePath);
                return $"<h3>Error loading template:</h3> Template file missing";
            }

            string content = File.ReadAllText(templatePath);
            return content
                .Replace("{{Module}}", module)
                .Replace("{{Function}}", function)
                .Replace("{{ErrorDetails}}", errorDetails)
                .Replace("{{Year}}", DateTime.Now.Year.ToString());
        }

        private void SendEmail(string fromEmail, string appPassword, string toEmail, string subject, string body, string ccEmail = null, string bccEmail = null)
        {
            using MailMessage mail = new()
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);
            if (!string.IsNullOrEmpty(ccEmail)) mail.CC.Add(ccEmail);
            if (!string.IsNullOrEmpty(bccEmail)) mail.Bcc.Add(bccEmail);

            using SmtpClient smtp = new("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(fromEmail, appPassword),
                EnableSsl = true
            };

            smtp.Send(mail);
        }
    }
}

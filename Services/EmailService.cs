using BugTrackingSystem.Models;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;

namespace BugTrackingSystem.Services
{
    public class EmailService : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var emailAddress = _emailSettings.EmailAddress ?? Environment.GetEnvironmentVariable("EmailAddress");
                var emailPassword = _emailSettings.EmailPassword ?? Environment.GetEnvironmentVariable("EmailPassword");
                var emailHost = _emailSettings.EmailHost ?? Environment.GetEnvironmentVariable("EmailHost");
                var emailPort = _emailSettings.EmailPort != 0 ? _emailSettings.EmailPort : int.Parse(Environment.GetEnvironmentVariable("EmailPort")!);
                MimeMessage newEmail = new MimeMessage();

                // Attach the email recipients
                newEmail.Sender = MailboxAddress.Parse(emailAddress);

                foreach (string address in email.Split(";"))
                {
                    {
                        newEmail.To.Add(MailboxAddress.Parse(address));

                    }


                    // Set The Subject
                    newEmail.Subject = subject;

                    // Format the body
                    BodyBuilder emailBody = new BodyBuilder();
                    emailBody.HtmlBody = htmlMessage;
                    newEmail.Body = emailBody.ToMessageBody();

                    // Prep the service and send the email
                    using SmtpClient smtpClient = new SmtpClient();

                    try
                    {
                        await smtpClient.ConnectAsync(emailHost, emailPort, SecureSocketOptions.StartTls);
                        await smtpClient.AuthenticateAsync(emailAddress, emailPassword);
                        await smtpClient.SendAsync(newEmail);

                        await smtpClient.DisconnectAsync(true);
                    }
                    catch (Exception)
                    {

                        // var error = ex.Message
                        throw;
                    }
                }


            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}


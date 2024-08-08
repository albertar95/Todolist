using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace Todolist.Helpers
{
    public static class NotifyHelper
    {
        public static bool SendMail(string message,string subject,string reciever)
        {
            try
            {
                EMailer mailer = new EMailer();
                mailer.ToEmail = reciever;
                mailer.Subject = subject;
                mailer.Body = message;
                mailer.Send();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    public class EMailer
    {
        public static string EmailUsername { get; set; } 
        public static string EmailPassword { get; set; }
        public static string EmailHost { get; set; }
        public static int EmailPort { get; set; }
        public static bool EmailSSL { get; set; }

        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtml { get; set; }

        static EMailer()
        {
            EmailHost = "albertar95.ir";
            EmailPort = 25; // Gmail can use ports 25, 465 & 587; but must be 25 for medium trust environment.
            EmailSSL = false;
            EmailUsername = "notify@albertar95.ir";
            EmailPassword = "Alireza@123";
        }

        public void Send()
        {
            SmtpClient smtp = new SmtpClient();
            smtp.Host = EmailHost;
            smtp.Port = EmailPort;
            smtp.EnableSsl = EmailSSL;
            //smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(EmailUsername, EmailPassword);

            using (var message = new MailMessage(EmailUsername, ToEmail))
            {
                message.Subject = Subject;
                message.Body = Body;
                smtp.Send(message);
            }
        }
    }
}
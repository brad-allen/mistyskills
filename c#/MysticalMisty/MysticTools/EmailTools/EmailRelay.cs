using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using MistyRobotics.SDK.Messengers;

namespace EmailTools
{
	public class EmailRelay
	{
		private readonly IRobotMessenger _misty;
		private readonly string _smtpHost;
		private readonly int _smtpPort;

		public EmailRelay(string smtpHost, int smtpPort, IRobotMessenger misty = null)
		{
			_smtpHost = smtpHost;
			_smtpPort = smtpPort;
			_misty = misty;
		}

		/// <summary>
		/// Need an app password on an email account or setup your own smtp server!
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="senderEmail"></param>
		/// <param name="toEmail"></param>
		/// <param name="body"></param>
		/// <param name="attachment"></param>
		/// <param name="attachmentName"></param>
		/// <returns>boolean indicating success or failure</returns>
		public bool SendEmail(string subject, EmailAccountInfo senderEmail, string toEmail, string body, byte[] attachment = null, string attachmentName = "Image.jpg")
		{
			try
			{
				if(senderEmail == null || senderEmail.Email == null)
				{
					TryToLog("Failed to send email. The sender's email address is required.");
					return false;
				}

				if (string.IsNullOrWhiteSpace(toEmail))
				{
					TryToLog("Failed to send email. A destination email is required.");
					return false;
				}

				if (string.IsNullOrWhiteSpace(body))
				{
					TryToLog("Failed to send email. You must include body html for the email.");
					return false;
				}

				if (string.IsNullOrWhiteSpace(subject))
				{
					TryToLog("Failed to send email. You must include a subject for the email.");
					return false;
				}

				using (MailMessage mail = new MailMessage())
				{
					mail.Subject = subject;
					mail.Body = "<h1>" + body + "</h1>";
					mail.IsBodyHtml = true;
					
					mail.From = new MailAddress(senderEmail.Email);
					mail.To.Add(toEmail);
					
					if(attachment != null && !string.IsNullOrWhiteSpace(attachmentName))
					{
						mail.Attachments.Add(new Attachment(new MemoryStream(attachment), attachmentName));
					}
					
					using (SmtpClient smtp = new SmtpClient(_smtpHost, _smtpPort))
					{
						smtp.Credentials = new NetworkCredential(senderEmail.Email, senderEmail.AppPassword);
						smtp.EnableSsl = true;
						smtp.Send(mail);
					}
				}

				TryToLog($"Congratulations! {senderEmail.Email} sent an email to {toEmail}.");
				return true;
			}
			catch (SmtpException ex)
			{
				TryToLog($"Smtp exception while sending email to {toEmail}. Please check your connection and credentials", ex);
			}
			catch (Exception ex)
			{
				TryToLog($"Failed to send email to {toEmail}.", ex);
			}
			return false;
		}

		private void TryToLog(string message, Exception ex = null)
        {
			if (_misty?.SkillLogger != null)
			{
				if(ex != null)
                {
					_misty.SkillLogger.Log(message, ex);
				}
				else
                {
					_misty.SkillLogger.Log(message);
				}
			}
		}
	}
}
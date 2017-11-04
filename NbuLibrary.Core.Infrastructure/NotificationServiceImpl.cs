using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Infrastructure
{
    public class EmailNotificationSection : ConfigurationSection
    {
        [ConfigurationProperty("smtpServer", DefaultValue = "localhost", IsRequired = false)]
        public string SmtpServer
        {
            get
            {
                return (string)this["smtpServer"];
            }
            set
            {
                this["smtpServer"] = value;
            }
        }

        [ConfigurationProperty("smtpPort", DefaultValue = 25, IsRequired = false)]
        public int SmtpPort
        {
            get
            {
                return (int)this["smtpPort"];
            }
            set
            {
                this["smtpPort"] = value;
            }
        }

        [ConfigurationProperty("from", DefaultValue = "library@nbu.bg", IsRequired = false)]
        public string From
        {
            get
            {
                return (string)this["from"];
            }
            set
            {
                this["from"] = value;
            }
        }

        [ConfigurationProperty("user", IsRequired = false)]
        public string User
        {
            get
            {
                return (string)this["user"];
            }
            set
            {
                this["user"] = value;
            }
        }

        [ConfigurationProperty("pass", IsRequired = false)]
        public string Password
        {
            get
            {
                return (string)this["pass"];
            }
            set
            {
                this["pass"] = value;
            }
        }

        [ConfigurationProperty("test", IsRequired = false, DefaultValue = false)]
        public bool TestingMode
        {
            get
            {
                return (bool)this["test"];
            }
            set
            {
                this["test"] = value;
            }
        }

        [ConfigurationProperty("useDefaultCredentials", IsRequired = false, DefaultValue = false)]
        public bool UseDefaultCredentials
        {
            get
            {
                return (bool)this["useDefaultCredentials"];
            }
            set
            {
                this["useDefaultCredentials"] = value;
            }
        }
    }

    public class NotificationServiceImpl : INotificationService
    {
        private string _from;
        private int _port;
        private string _host;
        private bool _testingMode;
        private ICredentialsByHost _credentials;
        private IEntityRepository _repository;
        private ISecurityService _securityService;
        private IFileService _fileService;
        private IDatabaseService _dbService;
        private bool _useDefaultCreds;

        public NotificationServiceImpl(IEntityRepository repository, ISecurityService securityService, IFileService fileService, IDatabaseService dbService)
        {
            _repository = repository;
            _securityService = securityService;
            _fileService = fileService;
            _dbService = dbService;

            var config = ConfigurationManager.GetSection("emailNotification") as EmailNotificationSection;
            _host = config.SmtpServer;
            _port = config.SmtpPort;
            _from = config.From;
            _testingMode = config.TestingMode;
            _useDefaultCreds = config.UseDefaultCredentials;

            if (!string.IsNullOrEmpty(config.User) || !string.IsNullOrEmpty(config.Password))
            {
                _credentials = new NetworkCredential(config.User, config.Password);
            }
        }

        public void SendNotification(bool withEmail, IEnumerable<User> to, string subject, string body, IEnumerable<Domain.File> attachments, IEnumerable<Relation> relations = null)
        {
            using (var dbContext = _dbService.GetDatabaseContext(true))
            {
                foreach (var user in to)
                {
                    Notification notif = new Notification()
                    {
                        Subject = subject,
                        Body = body,
                        Date = DateTime.Now,
                        Method = withEmail ? ReplyMethods.ByEmail : ReplyMethods.ByNotification
                    };

                    _repository.Create(notif);
                    _repository.Attach(notif, new Relation(Roles.Recipient, user));
                    _repository.Attach(notif, new Relation(Roles.Sender, _securityService.CurrentUser));
                    if (attachments != null)
                    {
                        foreach (var file in attachments)
                        {
                            _repository.Attach(notif, new Relation(Roles.Attachment, file));
                            _fileService.GrantAccess(file.Id, FileAccessType.Read, user);
                        }
                    }
                    if (relations != null)
                        foreach (var rel in relations)
                        {
                            var relToSave = new Relation(rel.Role, rel.Entity);
                            relToSave.Data = new Dictionary<string, object>(rel.Data);
                            _repository.Attach(notif, relToSave);
                        }
                }
                dbContext.Complete();
            }
        }

        public void SendEmail(string email, string subject, string body, IEnumerable<Domain.File> attachments)
        {
            //TODO:Notifications testing mode
            if (_testingMode)
                return;

            var webUrl = ConfigurationManager.AppSettings["WebAppRootUrl"];
            if (attachments != null && attachments.Count() > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(body);
                sb.Append("<br/>");
                sb.Append("<hr/>");
                sb.Append("Връзка към файлове:<br/>");
                sb.Append("<ul>");
                foreach (var att in attachments)
                {
                    sb.AppendFormat("<li><a href=\"{0}File/OpenAttachment/{1}\">{2}</a></li>", webUrl, att.Id, att.FileName);
                }
                sb.Append("</ul>");                
                body = sb.ToString();
            }

            var mail = new MailMessage(_from, email, subject, body);
            mail.IsBodyHtml = true;

            using (SmtpClient smtp = new SmtpClient(_host, _port))
            {
                smtp.UseDefaultCredentials = _useDefaultCreds;
                if (_credentials != null)
                {
                    smtp.Credentials = _credentials;
                }

                smtp.Send(mail);
            }
        }
    }
}

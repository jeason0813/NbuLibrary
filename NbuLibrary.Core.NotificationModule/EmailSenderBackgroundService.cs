using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.NotificationModule
{
    public class EmailSenderBackgroundService : IBackgroundService
    {
        private IEntityRepository _repository;
        private INotificationService _notificationService;

        public EmailSenderBackgroundService(IEntityRepository repository, INotificationService notificationService)
        {
            _repository = repository;
            _notificationService = notificationService;
        }

        public object Initialize()
        {
            return null;
        }

        public TimeSpan Interval
        {
            get { return TimeSpan.FromSeconds(20.0); }//TODO: email sender interval - to config
        }

        public object DoWork(object state)
        {
            EntityQuery2 q = new EntityQuery2(Notification.ENTITY);
            q.WhereIs("Method", ReplyMethods.ByEmail);
            q.WhereIs("EmailSent", false);
            q.WhereLessThen("EmailRetries", 6);
            q.Paging = new Paging(1, 5);
            q.Include(User.ENTITY, Roles.Recipient);
            q.Include(File.ENTITY, Roles.Attachment);
            q.AllProperties = true;
            var pending = _repository.Search(q).Select(e => new Notification(e));
            foreach (var notif in pending)
            {
                try
                {
                    _notificationService.SendEmail(notif.Recipient.Email, notif.Subject, notif.Body, notif.Attachments);
                }
                catch (Exception)
                {
                    _repository.Update(new Notification(notif.Id) { EmailRetries = notif.EmailRetries + 1 });
                    continue;
                }
                var upd = new Notification(notif.Id) { EmailSent = true };
                _repository.Update(upd);
            }

            return state;
        }

        public int ModuleId
        {
            get { return NotificationModule.Id; }
        }
    }
}

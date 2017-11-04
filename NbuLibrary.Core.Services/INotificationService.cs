using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.Domain;

namespace NbuLibrary.Core.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Sends a message(in the application and/or via email) to a registered user.
        /// </summary>
        /// <param name="withEmail">Controls whether the system must send an email.</param>
        /// <param name="toIds">User Ids to whom the notification should be sent.</param>
        /// <param name="subject">Subject field of the notification.</param>
        /// <param name="body">The body of the message.</param>
        void SendNotification(bool withEmail, IEnumerable<User> toIds, string subject, string body, IEnumerable<File> attachments, IEnumerable<Relation> relations = null);

        /// <summary>
        /// Sents email to the specified email address.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="subject">Subject.</param>
        /// <param name="body">Body of the messsage.</param>
        /// <param name="attachments">Files to be included in the email. Will be added to the body as links, not as attachments.</param>
        void SendEmail(string email, string subject, string body, IEnumerable<File> attachments);
    }
}

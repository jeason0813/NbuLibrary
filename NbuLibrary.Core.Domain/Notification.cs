using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class Notification : Entity
    {
        public const string ENTITY = "Notification";
        public const string ROLE = "Notification";

        public Notification()
            : base(ENTITY)
        {

        }

        public Notification(int id)
            : base(ENTITY, id)
        {

        }

        public Notification(Entity e)
            : base(e)
        {

        }

        public string Subject
        {
            get
            {
                return GetData<string>("Subject");
            }
            set
            {
                SetData<string>("Subject", value);
            }
        }

        public string Body
        {
            get
            {
                return GetData<string>("Body");
            }
            set
            {
                SetData<string>("Body", value);
            }
        }

        public bool Received
        {
            get
            {
                return GetData<bool>("Received");
            }
            set
            {
                SetData<bool>("Received", value);
            }
        }

        public bool EmailSent
        {
            get
            {
                return GetData<bool>("EmailSent");
            }
            set
            {
                SetData<bool>("EmailSent", value);
            }
        }

        public int EmailRetries
        {
            get
            {
                return GetData<int>("EmailRetries");
            }
            set
            {
                SetData<int>("EmailRetries", value);
            }
        }

        public ReplyMethods Method
        {
            get
            {
                return GetData<ReplyMethods>("Method");
            }
            set
            {
                SetData<ReplyMethods>("Method", value);
            }
        }

        public DateTime Date
        {
            get
            {
                return GetData<DateTime>("Date");
            }
            set
            {
                SetData<DateTime>("Date", value);
            }
        }

        public User Sender
        {
            get
            {
                var rel = this.GetSingleRelation(User.ENTITY, Roles.Sender);
                if (rel == null)
                    return null;
                else
                    return new User(rel.Entity);
            }
        }
        public User Recipient
        {
            get
            {
                var rel = this.GetSingleRelation(User.ENTITY, Roles.Recipient);
                if (rel == null)
                    return null;
                else
                    return new User(rel.Entity);
            }
        }

        public IEnumerable<File> Attachments
        {
            get
            {
                var rels = GetManyRelations(File.ENTITY, Roles.Attachment);
                if (rels == null)
                    return null;
                else
                    return rels.Select(r => new File(r.Entity));
            }
        }
    }
}

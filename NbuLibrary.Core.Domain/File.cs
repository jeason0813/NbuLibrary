using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class File : Entity
    {
        public const string ENTITY = "File";

        public File()
            : base(File.ENTITY)
        {

        }

        public File(int id)
            : base(File.ENTITY, id)
        {

        }

        public File(Entity e)
            : base(e)
        {

        }

        public string FileName
        {
            get
            {
                return GetData<string>("Name");
            }
            set
            {
                SetData<string>("Name", value);
            }
        }
        public string Extension
        {
            get
            {
                return GetData<string>("Extension");
            }
            set
            {
                SetData<string>("Extension", value);
            }
        }
        public string ContentType
        {
            get
            {
                return GetData<string>("ContentType");
            }
            set
            {
                SetData<string>("ContentType", value);
            }
        }
        public string ContentPath
        {
            get
            {
                return GetData<string>("ContentPath");
            }
            set
            {
                SetData<string>("ContentPath", value);
            }
        }
        public int? Size
        {
            get
            {
                return GetData<int?>("Size");
            }
            set
            {
                SetData<int?>("Size", value);
            }
        }

        public IEnumerable<FileAccess> Access
        {
            get
            {
                return GetManyRelations(User.ENTITY, Roles.Access).Select(r => new FileAccess(r));
            }
        }
    }

    public class FileAccess : Relation
    {
        public FileAccess()
            : base(Roles.Access, new Entity(User.ENTITY))
        {

        }

        public FileAccess(Relation r) : base(r.Role, r.Entity)
        {
            this.Data = r.Data;
        }

        public FileAccessType Type
        {
            get
            {
                return GetData<FileAccessType>("Type");
            }
            set
            {
                SetData<FileAccessType>("Type", value);
            }
        }
        public Guid? Token
        {
            get
            {
                return GetData<Guid?>("Token");
            }
            set
            {
                SetData<Guid?>("Token", value);
            }
        }
        public DateTime? Expire
        {
            get
            {
                return GetData<DateTime?>("Expire");
            }
            set
            {
                SetData<DateTime?>("Expire", value);
            }
        }
        public User User
        {
            get
            {
                return new User(Entity);
            }
            set
            {
                Entity = value;
            }
        }
    }

    public enum FileAccessType
    {
        None = 0,
        Owner = 1,
        Full = 2,
        Temporary = 3,
        Token = 4,
        Read = 5
    }
}

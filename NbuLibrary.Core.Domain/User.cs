using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class User : Entity
    {
        public const string ENTITY = "User";

        public User()
            : base(ENTITY)
        {
        }

        public User(int id)
            : base(ENTITY, id)
        {
        }

        public User(Entity e)
            : base(e)
        {
        }

        public string Password
        {
            get
            {
                return GetData<string>("Password");
            }
            set
            {
                SetData<string>("Password", value);
            }
        }
        public string Email
        {
            get
            {
                return GetData<string>("Email");
            }
            set
            {
                SetData<string>("Email", value);
            }
        }
        public bool IsActive
        {
            get
            {
                return GetData<bool>("IsActive");
            }
            set
            {
                SetData<bool>("IsActive", value);
            }
        }
        public string FirstName
        {
            get
            {
                return GetData<string>("FirstName");
            }
            set
            {
                SetData<string>("FirstName", value);
            }
        }
        public string MiddleName
        {
            get
            {
                return GetData<string>("MiddleName");
            }
            set
            {
                SetData<string>("MiddleName", value);
            }
        }
        public string LastName
        {
            get
            {
                return GetData<string>("LastName");
            }
            set
            {
                SetData<string>("LastName", value);
            }
        }
        public string FacultyNumber
        {
            get
            {
                return GetData<string>("FacultyNumber");
            }
            set
            {
                SetData<string>("FacultyNumber", value);
            }
        }
        public string CardNumber
        {
            get
            {
                return GetData<string>("CardNumber");
            }
            set
            {
                SetData<string>("CardNumber", value);
            }
        }
        public string PhoneNumber
        {
            get
            {
                return GetData<string>("PhoneNumber");
            }
            set
            {
                SetData<string>("PhoneNumber", value);
            }
        }
        public UserTypes UserType
        {
            get
            {
                return GetData<UserTypes>("UserType");
            }
            set
            {
                SetData<UserTypes>("UserType", value);
            }
        }
        public UserGroup UserGroup
        {
            get
            {
                var rel = GetSingleRelation(UserGroup.ENTITY, UserGroup.DEFAULT_ROLE);
                if (rel != null)
                    return new UserGroup(rel.Entity);
                else
                    return null;
            }
            set
            {
                SetSingleRelation(new Relation(UserGroup.DEFAULT_ROLE, value));
            }
        }

        public int? FailedLoginsCount
        {
            get
            {
                return GetData<int?>("FailedLoginsCount");
            }
            set
            {
                SetData<int?>("FailedLoginsCount", value);
            }
        }
        public DateTime? LastFailedLogin
        {
            get
            {
                return GetData<DateTime?>("LastFailedLogin");
            }
            set
            {
                SetData<DateTime?>("LastFailedLogin", value);
            }
        }
    }

    public enum UserTypes
    {
        Customer = 0,
        Librarian = 1,
        Admin = 2
    }
}

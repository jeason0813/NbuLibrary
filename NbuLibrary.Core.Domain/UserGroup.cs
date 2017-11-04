using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class UserGroup : Entity
    {
        public const string DEFAULT_ROLE = "UserGroup";
        public const string ENTITY = "UserGroup";

        public UserGroup()
            : base(ENTITY)
        {

        }

        public UserGroup(int id)
            : base(ENTITY, id)
        {

        }

        public UserGroup(Entity e)
            : base(e)
        {

        }

        public string Name
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
        public IEnumerable<ModulePermission> ModulePermissions
        {
            get
            {
                return GetManyRelations(ModulePermission.ENTITY, ModulePermission.DEFAULT_ROLE).Select(e => new ModulePermission(e));
            }
        }
    }
}

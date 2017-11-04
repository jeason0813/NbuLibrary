using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class ModulePermission : Relation
    {
        public const string DEFAULT_ROLE = "Permission";
        public const string ENTITY = "ModulePermission";

        public ModulePermission() : base(DEFAULT_ROLE)
        {
            Entity = new Entity(ENTITY);
        }

        public ModulePermission(Relation r) : base(r.Role, r.Entity)
        {
            this.Data = r.Data;
        }

        public int ModuleID
        {
            get
            {
                return Entity.GetData<int>("ModuleID");
            }
            set
            {
                Entity.SetData<int>("ModuleID", value);
            }
        }
        public string ModuleName
        {
            get
            {
                return Entity.GetData<string>("ModuleName");
            }
            set
            {
                Entity.SetData<string>("ModuleName", value);
            }
        }
        public string[] Available
        {
            get
            {
                return Entity.GetData<string>("Available").Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                Entity.SetData<string>("Available", string.Join(";", value));
            }
        }
        public string[] Granted
        {
            get
            {
                return GetData<string>("Granted").Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                SetData<string>("Granted", string.Join(";", value));
            }
        }
    }

    public class EntityPermission
    {
        public const string DEFAULT_ROLE = "EntityPermission";
        public string EntityName { get; set; }
        public EntityOperations Operations { get; set; }
    }

    [Flags]
    public enum EntityOperations
    {
        Create = 0x8,
        Read = 0x1,
        Update = 0x2,
        Delete = 0x4
    }

    [Flags]
    public enum Permissions
    {
        None = 0x0,
        RO = 0x1,
        RW = 0x2
    }
}

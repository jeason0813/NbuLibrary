using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using Ninject.Modules;
using NbuLibrary.Core.Domain;
using System.Security.Cryptography;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.DataModel;

namespace NbuLibrary.Core.AccountModule
{
    public class NotificationTemplates
    {
        public const string USER_CREATED = "90cf0184-4319-4d6e-a712-037301bdabad";
        public const string USER_ACTIVATED = "c183b90b-a6d8-4418-89ef-1c86167c01c5";
        public const string USER_PASSWORDRECOVERY = "3655d41e-5175-4fd0-a9c6-934e79d5cd28";
    }

    public class Permissions
    {
        public const string UserActivation = "UserActivation";
        public const string UserManagement = "UserManagement";
        public const string UserGroupManagement = "UserGroupManagement";
    }

    public class AccountNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<AccountModule>();
            Bind<IEntityOperationLogic>().To<AccountEntityOperationLogic>();
            Bind<IEntityQueryInspector>().To<AccountEntityQueryInspector>();
            Bind<IEntityOperationInspector>().To<AccountEntityOperationInspector>();
        }
    }


    public class AccountModule : IModule
    {
        private IUIProvider _uiProvider;

        public const int Id = 1;
        private IEntityRepository _entityRepository;
        private IDatabaseService _dbService;
        private string _systemUserEmail;

        public AccountModule(IEntityRepository entityRepository, IDatabaseService dbService, ISecurityService securityService)
        {
            _entityRepository = entityRepository;
            _dbService = dbService;
            _systemUserEmail = securityService.SystemUserEmail;
        }

        public string Name
        {
            get { return "Account"; }
        }

        public IUIProvider UIProvider
        {
            get
            {
                if (_uiProvider == null)
                    _uiProvider = new AccountUIProvider();

                return _uiProvider;
            }
        }

        public void Initialize()
        {
            try
            {
                var q = new EntityQuery2(User.ENTITY);
                q.WhereIs("Email", _systemUserEmail);
                using (var dbContext = _dbService.GetDatabaseContext(true))
                {
                    var e = _entityRepository.Search(q).SingleOrDefault();
                    if (e == null)
                    {
                        User admin = new User()
                        {
                            FirstName = "Build in",
                            LastName = "Administrator",
                            Email = _systemUserEmail,
                            Password = _systemUserEmail,
                            UserType = UserTypes.Admin,
                            IsActive = true
                        };

                        SHA1 sha1 = SHA1.Create();
                        admin.Password = Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(admin.Password)));

                        _entityRepository.Create(admin);
                        dbContext.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create build-in-administrator user.", ex);
            }
        }

        int IModule.Id
        {
            get { return AccountModule.Id; }
        }


        public ModuleRequirements Requirements
        {
            get
            {
                ModelBuilder mbUser = new ModelBuilder("User");
                mbUser.AddString("Password", 100);
                mbUser.AddString("Email", 256);
                mbUser.AddBoolean("IsActive", false);
                mbUser.AddString("FirstName", 256);
                mbUser.AddString("MiddleName", 256);
                mbUser.AddString("LastName", 256);
                mbUser.AddComputed("FullName", "[FirstName]+N' '+isnull([MiddleName], N'')+N' '+[LastName]");
                mbUser.AddString("CardNumber", 128);
                mbUser.AddString("FacultyNumber", 128);
                mbUser.AddString("PhoneNumber", 128);
                mbUser.AddEnum<UserTypes>("UserType");
                mbUser.AddUri("URI", "users");
                mbUser.AddInteger("FailedLoginsCount");
                mbUser.AddDateTime("LastFailedLogin");
                mbUser.AddString("RecoveryCode", 128);

                mbUser.Rules.AddUnique("CardNumber");
                mbUser.Rules.AddUnique("FacultyNumber");
                mbUser.Rules.AddUnique("Email");
                mbUser.Rules.AddRequired("Email");
                mbUser.Rules.AddRequired("Password");
                mbUser.Rules.AddRequired("IsActive");

                ModelBuilder mbGroup = new ModelBuilder("UserGroup");
                mbGroup.AddString("Name", 256);
                mbGroup.AddEnum<UserTypes>("UserType");

                ModelBuilder mbPerm = new ModelBuilder(ModulePermission.ENTITY);
                mbPerm.AddInteger("ModuleID");
                mbPerm.AddString("ModuleName", 256);
                mbPerm.AddString("Available", 1024);

                var rel = mbGroup.AddRelationTo(mbPerm.EntityModel, RelationType.ManyToMany, ModulePermission.DEFAULT_ROLE);
                ModelBuilder mbPermRel = new ModelBuilder(rel);
                mbPermRel.AddString("Granted", 1024, string.Empty);

                mbUser.AddRelationTo(mbGroup.EntityModel, RelationType.ManyToOne, "UserGroup");

                var dm = new DomainModel();
                dm.Entities.Add(mbUser.EntityModel);
                dm.Entities.Add(mbGroup.EntityModel);
                dm.Entities.Add(mbPerm.EntityModel);

                //_definitionService.Ensure(new EntityDefinition[] { user, userGroup, modulepPerm }, new EntityRelation[] { relation, relationGroupPerm });
                return new ModuleRequirements()
                {
                    Domain = dm,
                    //Definitions = new EntityDefinition[] { user, userGroup, modulepPerm },
                    //Relations = new EntityRelation[] { relation, relationGroupPerm },
                    UIDefinitions = GetUIDefs(),
                    Permissions = new string[] { Permissions.UserActivation, Permissions.UserGroupManagement, Permissions.UserManagement },
                    RequredModules = new[] { 0 },
                    Templates = new HtmlTemplate[]{
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.USER_ACTIVATED),
                            Name="User Activated",
                            SubjectTemplate="Потвърдена регистрация",
                            BodyTemplate=@"Здравейте {User.FirstName} {User.LastName},
<p>Вашата регистрация в системата за електронни услуги на библиотеката на Нов български университет е потвърдена. Може да влезете в системата на този адрес: <a href=""http://localhost:7564/Login\"">http://localhost:7564/Login</a></p>
Библиотека на НБУ"
                        },
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.USER_CREATED),
                            Name="User Created",
                            SubjectTemplate="Създаден потребител",
                            BodyTemplate=@"Здравейте {User.FirstName} {User.LastName},
<p>Вашата регистрация в системата за електронни услуги на библиотеката на Нов български университет беше успешно изпратена. Щом бъде прегледана от служител на библиотеката ще получите имейл с потвърдение и едва тогава ще може да влезете в системата.</p>
<p>Вашата парола е: <b>{User.Password}</b></p>
Библиотека на НБУ"
                        },
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.USER_PASSWORDRECOVERY),
                            Name="User Password Recovery",
                            SubjectTemplate="Забравена парола",
                            BodyTemplate=@"Здравейте {User.FirstName} {User.LastName},
<p>Може да смените вашата забравена парола като последвате този линк или го копирате във вашия браузър:</p>
<p><a href=""http://localhost:7564/Login/RecoverPassword?rc={User.RecoveryCode}&email={User.Email}"">http://localhost:7564/Login/RecoverPassword?rc={User.RecoveryCode}&email={User.Email}</a></p>
Библиотека на НБУ"
                        }
                    }
                };
            }
        }

        private IEnumerable<Service.tmp.UIDefinition> GetUIDefs()
        {
            List<UIDefinition> defs = new List<UIDefinition>();
            defs.Add(new GridDefinition()
            {
                Name = "Account_Admin_UsersGrid",
                Entity = "User",
                Label = "Admin - Users grid",
                Fields = new List<ViewField>()
            });

            defs.Add(new GridDefinition()
            {
                Name = "Account_Admin_UserGroupGrid",
                Entity = "UserGroup",
                Label = "Admin - UserGroups grid",
                Fields = new List<ViewField>()
            });

            defs.Add(new GridDefinition()
            {
                Name = "Account_Admin_PendingRegistrations",
                Entity = "User",
                Label = "Admin - Pending registrations grid",
                Fields = new List<ViewField>()
            });

            defs.Add(new ViewDefinition()
            {
                Name = "Account_Admin_User",
                Entity = "User",
                Label = "Admin - User",
                Fields = new List<ViewField>()
            });

            defs.Add(new FormDefinition()
            {
                Name = "Account_Admin_UserForm",
                Entity = "User",
                Label = "Admin - User Form",
                Fields = new List<EditField>()
            });
            defs.Add(new FormDefinition()
            {
                Name = "Account_Admin_UserGroupForm",
                Entity = "UserGroup",
                Label = "Admin - User Form",
                Fields = new List<EditField>()
            });


            return defs;
        }


        public decimal Version
        {
            get { return 2.1m; }
        }
    }

    public class AccountUIProvider : IUIProvider
    {
        public string GetClientTemplates(Domain.UserTypes type)
        {
            if (type == UserTypes.Admin)
                return GetContent("Templates.AdminUI.html");
            else return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Core.AccountModule.{0}", resourceFileName)))
            {
                using (var reader = new StreamReader(stream))
                {
                    resourceContent = reader.ReadToEnd();
                }
            }

            return resourceContent;
        }

        IEnumerable<ClientScript> IUIProvider.GetClientScripts(UserTypes type)
        {
            return new List<ClientScript>() {
                new ClientScript(){
                    Name = "account",
                    Content = GetContent("Scripts.account.js")
                },
                new ClientScript(){
                    Name = "vm.usergroup",
                    Content = GetContent("Scripts.vm.usergroup.js")
                },
                new ClientScript(){
                    Name = "vm.usergroups",
                    Content = GetContent("Scripts.vm.usergroups.js")
                },
                new ClientScript(){
                    Name = "vm.users",
                    Content = GetContent("Scripts.vm.users.js")
                },
                new ClientScript(){
                    Name = "vm.user",
                    Content = GetContent("Scripts.vm.user.js")
                },
                new ClientScript(){
                    Name = "vm.pendingUsers",
                    Content = GetContent("Scripts.vm.pendingUsers.js")
                }
            };
        }
    }
}

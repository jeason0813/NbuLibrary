using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ninject;
using System.Linq;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.DataModel;
using System.Collections.Generic;
using NbuLibrary.Core.Domain;
using System.Text;

namespace NbuLibrary.Tests.Performance
{
    [TestClass]
    public class DemoData
    {
        private IKernel _kernel;
        public IKernel Kernel
        {
            get
            {
                if (_kernel == null)
                {
                    _kernel = new StandardKernel();
                    _kernel.Load("NbuLibrary.Core.*.dll");
                    _kernel.Load("NbuLibrary.Modules.*.dll");

                    _kernel.Rebind<IDatabaseService>().To<TestDatabaseService>();
                }
                return _kernel;
            }
        }

        public T GetService<T>()
        {
            return Kernel.Get<T>();

        }

        private ViewField GetViewFieldForProperty(PropertyModel pm, int order, string label = null)
        {
            if (pm.Type == PropertyType.String)
            {
                var stringPropModel = pm as StringPropertyModel;
                return new Textfield()
                {
                    Label = label ?? pm.Name,
                    Length = stringPropModel.Length,
                    Order = order,
                    Property = pm.Name
                };
            }
            else if (pm.Type == PropertyType.Number)
            {
                var numberPropModel = pm as NumberPropertyModel;
                return new Numberfield()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name
                };
            }
            else if (pm.Type == PropertyType.Enum)
            {
                var enumPropModel = pm as EnumPropertyModel;
                return new Enumfield()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name,
                    EnumClass = enumPropModel.EnumType.AssemblyQualifiedName
                };
            }
            else if (pm.Type == PropertyType.Computed)
            {
                return new Textfield()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name
                };
            }
            else if (pm.Type == PropertyType.Datetime)
            {
                return new Datefield()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name
                };
            }
            else if (pm.Type == PropertyType.Boolean)
            {
                return new Textfield()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name,
                    Type = ViewTypes.Checkfield
                };
            }
            else if (pm.Type == PropertyType.Sequence)
            {
                return new Textfield()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name
                };
            }
            else
                throw new NotImplementedException();
        }

        private EditField GetEditFieldForProperty(PropertyModel pm, int order, bool required = false, string label = null)
        {
            if (pm.Type == PropertyType.String)
            {
                var stringPropModel = pm as StringPropertyModel;
                return new Textbox()
                {
                    Label = label ?? pm.Name,
                    MaxLength = stringPropModel.Length,
                    Order = order,
                    Property = pm.Name,
                    Required = required
                };
            }
            else if (pm.Type == PropertyType.Number)
            {
                var numberPropModel = pm as NumberPropertyModel;
                return new Numberbox()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name,
                    Integer = numberPropModel.IsInteger,
                    Required = required
                };
            }
            else if (pm.Type == PropertyType.Enum)
            {
                var enumPropModel = pm as EnumPropertyModel;
                return new Enumlist()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name,
                    EnumClass = enumPropModel.EnumType.AssemblyQualifiedName,
                    Required = required
                };
            }
            else if (pm.Type == PropertyType.Computed)
            {
                throw new NotSupportedException();
            }
            else if (pm.Type == PropertyType.Datetime)
            {
                return new Datepicker()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name,
                    Required = required
                };
            }
            else if (pm.Type == PropertyType.Boolean)
            {
                return new Textbox()
                {
                    Label = label ?? pm.Name,
                    Order = order,
                    Property = pm.Name,
                    Type = EditTypes.Checkbox
                };
            }
            else if (pm.Type == PropertyType.Sequence)
            {
                throw new NotSupportedException();
            }
            else
                throw new NotImplementedException();
        }

        private ViewField GetViewFieldForRelationProperty(EntityModel em, RelationModel rm, string property, int order, string label = null)
        {
            EntityModel other = rm.Left == em ? rm.Right : rm.Left;
            var field = GetViewFieldForProperty(other.Properties[property], order, label);

            var type = rm.TypeFor(em.Name);
            if (type == RelationType.OneToMany || type == RelationType.ManyToMany)
            {
                field.Type = ViewTypes.Listfield;
            }

            field.Role = rm.Role;
            field.Entity = other.Name;
            return field;
        }

        [TestMethod]
        public void _GenerateDemoUI()
        {
            var modelSvc = GetService<IDomainModelService>();
            var uiSvc = GetService<Core.Services.IUIDefinitionService>();

            var userModel = modelSvc.Domain.Entities["User"];

            var usersGrid = uiSvc.GetByName<NbuLibrary.Core.Service.tmp.GridDefinition>("Account_Admin_UsersGrid");
            usersGrid.Fields.Clear();
            usersGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["Email"], usersGrid.Fields.Count, "Email"));
            usersGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["FullName"], usersGrid.Fields.Count, "Full name"));
            usersGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["IsActive"], usersGrid.Fields.Count, "Is active"));
            usersGrid.Fields.Add(GetViewFieldForRelationProperty(userModel, userModel.GetRelation(UserGroup.ENTITY, UserGroup.DEFAULT_ROLE), "Name", usersGrid.Fields.Count, "User group"));
            usersGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["FailedLoginsCount"], usersGrid.Fields.Count, "Failed logins"));
            usersGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["LastFailedLogin"], usersGrid.Fields.Count, "Last failed login"));
            uiSvc.Update(usersGrid);

            var usersPendingGrid = uiSvc.GetByName<NbuLibrary.Core.Service.tmp.GridDefinition>("Account_Admin_PendingRegistrations");
            usersPendingGrid.Fields.Clear();
            usersPendingGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["Email"], usersPendingGrid.Fields.Count, "Email"));
            usersPendingGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["FullName"], usersPendingGrid.Fields.Count, "Full name"));
            usersPendingGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["PhoneNumber"], usersPendingGrid.Fields.Count, "Phone number"));
            usersPendingGrid.Fields.Add(GetViewFieldForRelationProperty(userModel, userModel.GetRelation(UserGroup.ENTITY, UserGroup.DEFAULT_ROLE), "Name", usersPendingGrid.Fields.Count, "User group"));
            usersPendingGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["CardNumber"], usersPendingGrid.Fields.Count, "Card number"));
            usersPendingGrid.Fields.Add(GetViewFieldForProperty(userModel.Properties["FacultyNumber"], usersPendingGrid.Fields.Count, "Faculty number"));
            uiSvc.Update(usersPendingGrid);

            var usersDetails = uiSvc.GetByName<NbuLibrary.Core.Service.tmp.ViewDefinition>("Account_Admin_User");
            usersDetails.Fields.Clear();
            usersDetails.Fields.Add(GetViewFieldForProperty(userModel.Properties["Email"], usersDetails.Fields.Count, "Email"));
            usersDetails.Fields.Add(GetViewFieldForProperty(userModel.Properties["FullName"], usersDetails.Fields.Count, "Full name"));
            usersDetails.Fields.Add(GetViewFieldForProperty(userModel.Properties["IsActive"], usersDetails.Fields.Count, "Is active"));
            usersDetails.Fields.Add(GetViewFieldForProperty(userModel.Properties["CardNumber"], usersDetails.Fields.Count, "Card number"));
            usersDetails.Fields.Add(GetViewFieldForProperty(userModel.Properties["FacultyNumber"], usersDetails.Fields.Count, "Faculty number"));
            usersDetails.Fields.Add(GetViewFieldForProperty(userModel.Properties["PhoneNumber"], usersDetails.Fields.Count, "Phone number"));
            usersDetails.Fields.Add(GetViewFieldForRelationProperty(userModel, userModel.GetRelation(UserGroup.ENTITY, UserGroup.DEFAULT_ROLE), "Name", usersDetails.Fields.Count, "User group"));
            usersDetails.Fields.Add(GetViewFieldForProperty(userModel.Properties["FailedLoginsCount"], usersDetails.Fields.Count, "Failed logins"));
            usersDetails.Fields.Add(GetViewFieldForProperty(userModel.Properties["LastFailedLogin"], usersDetails.Fields.Count, "Last failed login"));
            uiSvc.Update(usersDetails);

            var usersForm = uiSvc.GetByName<NbuLibrary.Core.Service.tmp.FormDefinition>("Account_Admin_UserForm");
            usersForm.Fields.Clear();
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["Email"], usersDetails.Fields.Count, true, "Email"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["Password"], usersDetails.Fields.Count, true, "Password"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["FirstName"], usersDetails.Fields.Count, true, "First name"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["MiddleName"], usersDetails.Fields.Count, false, "Middle name"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["LastName"], usersDetails.Fields.Count, true, "Last name"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["IsActive"], usersDetails.Fields.Count, false, "Is active"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["CardNumber"], usersDetails.Fields.Count, false, "Card number"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["FacultyNumber"], usersDetails.Fields.Count, false, "Faculty number"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["PhoneNumber"], usersDetails.Fields.Count, false, "Phone number"));
            usersForm.Fields.Add(GetEditFieldForProperty(userModel.Properties["FailedLoginsCount"], usersDetails.Fields.Count, false, "Failed logins"));
            uiSvc.Update(usersForm);
            var groupModel = modelSvc.Domain.Entities["UserGroup"];

            var groupsGrid = uiSvc.GetByName<NbuLibrary.Core.Service.tmp.GridDefinition>("Account_Admin_UserGroupGrid");
            groupsGrid.Fields.Clear();
            groupsGrid.Fields.Add(GetViewFieldForProperty(groupModel.Properties["Name"], groupsGrid.Fields.Count, "Name"));
            groupsGrid.Fields.Add(GetViewFieldForProperty(groupModel.Properties["UserType"], groupsGrid.Fields.Count, "For type"));
            uiSvc.Update(groupsGrid);
        }

        [TestMethod]
        public void _GenerateDemoData()
        {
            using (GetService<ISecurityService>().BeginSystemContext())
            {

                var repo = GetService<IEntityRepository>();
                var userGroupLibAll = new NbuLibrary.Core.Domain.UserGroup()
                {
                    Name = "Библиотечно-информационни услуги",
                    UserType = Core.Domain.UserTypes.Librarian
                };
                var userGroupLibMyMag = new NbuLibrary.Core.Domain.UserGroup()
                {
                    Name = "Моите списания",
                    UserType = Core.Domain.UserTypes.Librarian
                };
                var userGroupStudents = new NbuLibrary.Core.Domain.UserGroup()
                {
                    Name = "Студент",
                    UserType = Core.Domain.UserTypes.Customer
                };
                var userGroupEmpl = new NbuLibrary.Core.Domain.UserGroup()
                {
                    Name = "Служител",
                    UserType = Core.Domain.UserTypes.Customer
                };
                var userGroupProfs = new NbuLibrary.Core.Domain.UserGroup()
                {
                    Name = "Преподавател",
                    UserType = Core.Domain.UserTypes.Customer
                };
                var userGroupOutWithCard = new NbuLibrary.Core.Domain.UserGroup()
                {
                    Name = "Външен (с читателска карта)",
                    UserType = Core.Domain.UserTypes.Customer
                };
                var userGroupOutNoCard = new NbuLibrary.Core.Domain.UserGroup()
                {
                    Name = "Външен (без читателска карта)",
                    UserType = Core.Domain.UserTypes.Customer
                };

                repo.Search(new EntityQuery2("UserGroup")).ToList().ForEach(e => repo.Delete(e, true));

                repo.Create(userGroupLibAll);
                repo.Create(userGroupLibMyMag);
                repo.Create(userGroupStudents);
                repo.Create(userGroupEmpl);
                repo.Create(userGroupProfs);
                repo.Create(userGroupOutWithCard);
                repo.Create(userGroupOutNoCard);

                var userLib1 = new NbuLibrary.Core.Domain.User()
                {
                    UserType = Core.Domain.UserTypes.Librarian,
                    Email = "librarian1@nbu.bg",
                    Password = "librarian1",
                    FirstName = "Петър",
                    LastName = "Димитров",
                    UserGroup = userGroupLibAll
                };

                var userLib2 = new NbuLibrary.Core.Domain.User()
                {
                    UserType = Core.Domain.UserTypes.Librarian,
                    Email = "librarian2@nbu.bg",
                    Password = "librarian2",
                    FirstName = "Радост",
                    LastName = "Иванова",
                    UserGroup = userGroupLibAll
                };

                var userLib3 = new NbuLibrary.Core.Domain.User()
                {
                    UserType = Core.Domain.UserTypes.Librarian,
                    Email = "librarian3@nbu.bg",
                    Password = "librarian3",
                    FirstName = "Иван",
                    LastName = "Георгиев",
                    UserGroup = userGroupLibMyMag
                };
                var qAllUsersWithoutAdmin = new EntityQuery2("User");
                qAllUsersWithoutAdmin.WhereAnyOf("UserType", new object[] { 0, 1 });
                //Assert.AreEqual(0, repo.Search(qAllUsersWithoutAdmin).Count());
                repo.Search(qAllUsersWithoutAdmin).ToList().ForEach(e => repo.Delete(e, true));

                //GetService<IEntityOperationService>().Update(new EntityUpdate(userLib1));
                repo.Create(userLib1);
                repo.Attach(userLib1, new Relation(UserGroup.DEFAULT_ROLE, userLib1.UserGroup));
                repo.Create(userLib2);
                repo.Attach(userLib2, new Relation(UserGroup.DEFAULT_ROLE, userLib2.UserGroup));
                repo.Create(userLib3);
                repo.Attach(userLib3, new Relation(UserGroup.DEFAULT_ROLE, userLib3.UserGroup));

                List<User> customers = new List<User>();
                int count = 100;
                string[] fnames = new[] { "Иван", "Петър", "Кристиян", "Йордан", "Георги", "Симеон", "Александър", "Димитър", "Добромир", "Божидар", "Тодор", "Теодор", "Борис", "Борислав", "Цветан" };
                string[] lnames = new[] { "Димитров", "Иванов", "Петров", "Йорданов", "Георгиев", "Симеонов", "Александров", "Кирилов", "Тодоров", "Бориславов", "Караиванов", "Ножаров", "Ковачев" };
                UserGroup[] customerGroups = new[] { userGroupOutNoCard, userGroupOutWithCard, userGroupProfs, userGroupStudents, userGroupEmpl };


                Random r = new Random(1);//fixed seed so that we have identical values generated
                for (int i = 0; i < count; i++)
                {
                    string email = GenerateEmail(r);
                    string password = email.Substring(0, email.IndexOf('.'));
                    UserGroup group = customerGroups[r.Next(customerGroups.Length)];
                    var user = new User()
                        {
                            UserType = UserTypes.Customer,
                            UserGroup = group,
                            Email = email,
                            Password = password,
                            PhoneNumber = GeneratePhone(r),
                            FirstName = fnames[r.Next(fnames.Length)],
                            MiddleName = lnames[r.Next(lnames.Length)],
                            LastName = lnames[r.Next(lnames.Length)],
                            IsActive = r.Next(3) < 2,
                            FacultyNumber = GenerateFacultyNumber(r, group),
                            CardNumber = group == userGroupOutWithCard ? GenerateCardNumber(r) : null
                        };
                    customers.Add(user);
                }


                customers.ForEach(e =>
                {
                    repo.Create(e);
                    repo.Attach(e, new Relation(UserGroup.DEFAULT_ROLE, e.UserGroup));
                });
            }
        }

        private string GenerateCardNumber(Random r)
        {

            string numbers = "123456789";
            StringBuilder sb = new StringBuilder();
            int len = 10;
            for (int i = 0; i < len; i++)
            {
                sb.Append(numbers[r.Next(numbers.Length)]);
            }

            return sb.ToString();
        }

        private string GenerateFacultyNumber(Random r, UserGroup group)
        {
            StringBuilder sb = new StringBuilder();
            if (group.Name == "Студент")
                sb.Append("F");
            else if (group.Name == "Преподавател")
                sb.Append("P");
            else
                return null;

            string numbers = "0123456789";

            int len = 5;
            for (int i = 0; i < len; i++)
            {
                sb.Append(numbers[r.Next(numbers.Length)]);
            }

            return sb.ToString();
        }

        private string GenerateEmail(Random r)
        {
            string letters = "abcdefghijklmnopqrstuvwxyz0123456789";
            string[] domains = new[] { ".comx", ".eux", ".bgx", ".co.ukx", ".orgx" };
            StringBuilder sb = new StringBuilder();
            int len = r.Next(5, 10);
            for (int i = 0; i < len; i++)
                sb.Append(letters[r.Next(letters.Length)]);
            sb.Append(domains[r.Next(domains.Length)]);
            return sb.ToString();
        }

        private string GeneratePhone(Random r)
        {
            string numbers = "123456789";
            StringBuilder sb = new StringBuilder();
            int len = 8;
            for (int i = 0; i < len; i++)
            {
                if (i > 0 && i % 2 == 0 && i < len - 1)
                    sb.Append("-");
                sb.Append(numbers[r.Next(numbers.Length)]);
            }

            return sb.ToString();
        }
    }
}

using NbuLibrary.Core.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NbuLibrary.Web.Models
{
    public class LoginViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class RegisterGroupModel
    {
        [Display(Name = "Потребителска група")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        public int UserGroup { get; set; }

        public IEnumerable<UserGroup> AvailableGroups { get; set; }
    }

    public class RegisterViewModel
    {
        public RegisterViewModel()
        {

        }

        public RegisterViewModel(RegisterStudentViewModel reg)
        {
            Email = reg.Email;
            UserGroup = reg.UserGroup;
            Password = reg.Password;
            ConfirmPassword = reg.ConfirmPassword;
            FirstName = reg.FirstName;
            MiddleName = reg.MiddleName;
            LastName = reg.LastName;
            FacultyNumber = reg.FacultyNumber;
            CardNumber = reg.CardNumber;
            PhoneNumber = reg.PhoneNumber;
        }

        public RegisterViewModel(RegisterExternalViewModel reg)
        {
            Email = reg.Email;
            UserGroup = reg.UserGroup;
            UserGroupName = reg.UserGroupName;
            Password = reg.Password;
            ConfirmPassword = reg.ConfirmPassword;
            FirstName = reg.FirstName;
            MiddleName = reg.MiddleName;
            LastName = reg.LastName;
            CardNumber = reg.CardNumber;
            PhoneNumber = reg.PhoneNumber;
        }

        [Display(Name = "Имейл")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [EmailAddress(ErrorMessage="Въведеният имейл адрес е невалиден.")]
        public string Email { get; set; }

        [Display(Name = "Потребителска група")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        public int UserGroup { get; set; }

        public string UserGroupName { get; set; }

        [Display(Name = "Парола")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string Password { get; set; }

        [Display(Name = "Повторно въвеждане на паролата")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [Compare("Password", ErrorMessage = "Въведените данни за парола не съответстват.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Име")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string FirstName { get; set; }

        [Display(Name = "Презиме")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string MiddleName { get; set; }

        [Display(Name = "Фамилия")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string LastName { get; set; }

        [Display(Name = "Факултетен/Преподавателски номер")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string FacultyNumber { get; set; }

        [Display(Name = "Номер на читателска карта")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string CardNumber { get; set; }

        [Display(Name = "Телефонен номер")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string PhoneNumber { get; set; }
    }

    public class RegisterStudentViewModel
    {
        public RegisterStudentViewModel()
        {

        }

        [Display(Name = "Имейл")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [EmailAddress(ErrorMessage = "Въведеният имейл адрес е невалиден.")]
        public string Email { get; set; }

        [Display(Name = "Потребителска група")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        public int UserGroup { get; set; }

        public string UserGroupName { get; set; }

        [Display(Name = "Парола")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string Password { get; set; }

        [Display(Name = "Повторно въвеждане на паролата")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [Compare("Password", ErrorMessage = "Въведените данни за парола не съответстват.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Име")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string FirstName { get; set; }

        [Display(Name = "Презиме")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string MiddleName { get; set; }

        [Display(Name = "Фамилия")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string LastName { get; set; }

        [Display(Name = "Факултетен/Преподавателски номер")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [RegularExpression("[f,p,F,P][0-9]+", ErrorMessage = "Въведете валиден факултетен или преподавателски номер. (Да започва с F или P)")]
        public string FacultyNumber { get; set; }

        [Display(Name = "Номер на читателска карта")]
        [StringLength(128, ErrorMessage="Дължината на полето не трябва да превишава {1} символа.")]
        public string CardNumber { get; set; }

        [Display(Name = "Телефонен номер")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string PhoneNumber { get; set; }
    }
    public class RegisterExternalViewModel
    {
        public RegisterExternalViewModel()
        {

        }

        [Display(Name = "Имейл")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [EmailAddress(ErrorMessage = "Въведеният имейл адрес е невалиден.")]
        public string Email { get; set; }

        [Display(Name = "Потребителска група")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        public int UserGroup { get; set; }

        public string UserGroupName { get; set; }

        [Display(Name = "Парола")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string Password { get; set; }

        [Display(Name = "Повторно въвеждане на паролата")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [Compare("Password", ErrorMessage = "Въведените данни за парола не съответстват.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Име")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string FirstName { get; set; }

        [Display(Name = "Презиме")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string MiddleName { get; set; }

        [Display(Name = "Фамилия")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string LastName { get; set; }

        [Display(Name = "Номер на читателска карта")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [Required(ErrorMessage="Полето \"{0}\" е задължително.")]
        public string CardNumber { get; set; }

        [Display(Name = "Телефонен номер")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string PhoneNumber { get; set; }
    }
}
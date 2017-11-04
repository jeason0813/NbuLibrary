using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NbuLibrary.Web.Models
{
    public class PasswordRecoveryModel
    {
        public string RecoveryCode { get; set; }

        [Display(Name = "Имейл")]
        [Required(ErrorMessage = "Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [EmailAddress(ErrorMessage = "Въведеният имейл адрес е невалиден.")]
        public string Email { get; set; }

        [Display(Name = "Парола")]
        [Required(ErrorMessage = "Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        public string Password { get; set; }

        [Display(Name = "Повторно въвеждане на паролата")]
        [Required(ErrorMessage = "Полето \"{0}\" е задължително.")]
        [StringLength(128, ErrorMessage = "Дължината на полето не трябва да превишава {1} символа.")]
        [Compare("Password", ErrorMessage = "Въведените данни за парола не съответстват.")]
        public string ConfirmPassword { get; set; }
    }
}
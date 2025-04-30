using System.ComponentModel.DataAnnotations;
using LTS.Services;

namespace LTS.Validators
{
    public class ValidRoleNameAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string roleName || string.IsNullOrWhiteSpace(roleName))
            {
                return new ValidationResult("직책을 입력해야 합니다.");
            }

            if (!StoreService.IsValidRole(roleName))
            {
                return new ValidationResult("유효하지 않은 직책입니다.");
            }

            return ValidationResult.Success;
        }
    }
}

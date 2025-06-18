using System.ComponentModel.DataAnnotations;

namespace LTS.Validators
{
    public class ValidNameAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string storeName || string.IsNullOrWhiteSpace(storeName) || storeName == "매장 선택")
            {
                return new ValidationResult("매장을 선택해 주세요");
            }

            if (StoreService.IsValidStoreName(storeName))
            {
                if (StoreService.IsValidStoreName(storeName))
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult("매장 이름이 올바르지 않습니다.");
        }
    }
}

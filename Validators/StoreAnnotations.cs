using System.ComponentModel.DataAnnotations;

namespace LTS.Validators
{
    public class ValidStoreNameAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string storeName || string.IsNullOrWhiteSpace(storeName) || storeName == "매장 선택")
            {
                return new ValidationResult("매장을 선택해 주세요");
            }

            if (Services.StoreService.IsValidStoreName(storeName))
            {
                if (Services.StoreService.IsValidStoreName(storeName))
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult("매장 이름이 올바르지 않습니다.");
        }
    }
}

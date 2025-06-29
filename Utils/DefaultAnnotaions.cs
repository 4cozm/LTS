using System.Text.RegularExpressions;

namespace LTS.Utils
{
    public static class ValidDefaultAttribute
    {
        public static bool IsValidPhoneNumber(string phone, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                errorMessage = "전화번호를 입력해 주세요.";
                return false;
            }

            if (!Regex.IsMatch(phone, @"^\d{11}$"))
            {
                errorMessage = "전화번호는 숫자만 포함하며 11자리여야 합니다.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }

}
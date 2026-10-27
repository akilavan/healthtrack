using System.Text.RegularExpressions;

namespace coursework_2.Models
{
    public static class ValidationHelper
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[A-Za-z0-9_%+-]+(?:\.[A-Za-z0-9_%+-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$",
            RegexOptions.Compiled);

        private static readonly Regex LocalPhoneRegex = new Regex(
            @"^0[0-9]{9}$", RegexOptions.Compiled);

        private static readonly Regex Plus94PhoneRegex = new Regex(
            @"^\+94[0-9]{9}$", RegexOptions.Compiled);

        private static readonly Regex Phone94Regex = new Regex(
            @"^94[0-9]{9}$", RegexOptions.Compiled);

        public const string PasswordRule =
            "Password must be at least 8 characters and include an uppercase letter, a number, and a special character.";

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;

            return Regex.IsMatch(password, "[A-Z]")
                && Regex.IsMatch(password, "[0-9]")
                && Regex.IsMatch(password, @"[^A-Za-z0-9\s]");
        }


        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 150) return false;

            email = email.Trim();
            string[] parts = email.Split('@');
            if (parts.Length != 2 || parts[0].Length > 64) return false;

            return EmailRegex.IsMatch(email);
        }

        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;

            string digits = Regex.Replace(phone.Trim(), @"[\s\-\(\)]", "");

            if (digits.StartsWith("+94") && digits.Length == 12)
                return "+94" + digits.Substring(3);

            if (digits.StartsWith("94") && digits.Length == 11)
                return "+94" + digits.Substring(2);

            if (digits.StartsWith("0") && digits.Length == 10)
                return "+94" + digits.Substring(1);

            return digits;
        }

        public static bool IsValidPhone(string phone)
        {
            string normalized = NormalizePhone(phone);
            return normalized != null && Regex.IsMatch(normalized, @"^\+94[0-9]{9}$");
        }
    }
}
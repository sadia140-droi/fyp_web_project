using System;
using System.Text.RegularExpressions;

namespace CloudKitchen.Services
{
    public static class PhoneValidationHelper
    {
        // Pakistani mobile regex: starts with 03, +923, or 923 followed by network code (0-5) and 8 digits (11 digits total for standard format)
        private static readonly Regex PakistaniPhoneRegex = new Regex(@"^(\+92|92|0)?3[0-5][0-9]{8}$", RegexOptions.Compiled);

        // Strict Google / Gmail email regex
        private static readonly Regex GoogleEmailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|googlemail\.com)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Validates that an email address is a valid Google Account (@gmail.com or @googlemail.com).
        /// </summary>
        public static bool IsValidGoogleEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var trimmed = email.Trim();
            return GoogleEmailRegex.IsMatch(trimmed);
        }

        /// <summary>
        /// Validates that a phone number matches standard Pakistani mobile networks.
        /// </summary>
        public static bool IsValidPakistaniPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var clean = Regex.Replace(phone.Trim(), @"[\s\-\(\)]", "");
            return PakistaniPhoneRegex.IsMatch(clean);
        }

        /// <summary>
        /// Normalizes a Pakistani phone number into standard 11-digit 03XXXXXXXXX format.
        /// Returns false if the input does not match a valid Pakistani mobile number.
        /// </summary>
        public static bool TryNormalizePakistaniPhone(string? phone, out string normalizedPhone)
        {
            normalizedPhone = string.Empty;
            if (string.IsNullOrWhiteSpace(phone)) return false;

            var clean = Regex.Replace(phone.Trim(), @"[\s\-\(\)]", "");

            if (clean.StartsWith("+92"))
            {
                clean = "0" + clean.Substring(3);
            }
            else if (clean.StartsWith("92") && clean.Length == 12)
            {
                clean = "0" + clean.Substring(2);
            }

            if (Regex.IsMatch(clean, @"^03[0-5][0-9]{8}$"))
            {
                normalizedPhone = clean;
                return true;
            }

            return false;
        }
    }
}

using GoHireNow.Models.CommonModels.Enums;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GoHireNow.Service.CommonServices
{
    public static class ExtensionMethods
    {
        public static string ToCountryName(this int id)
        {
            try
            {
                return LookupService.Countries.FirstOrDefault(x => x.Id == id).Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ToCountryCode(this string countryName)
        {
            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                RegionInfo region = new RegionInfo(culture.Name);
                if (string.Equals(region.EnglishName, countryName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return region.TwoLetterISORegionName;
                }
            }

            return null;
        }

        public static string CalculateMD5Hash(this string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static string ToGlobalPlanName(this int id)
        {
            try
            {
                return LookupService.GlobalPlans.FirstOrDefault(x => x.Id == id).Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ToGlobalSkillName(this int id)
        {
            try
            {
                return LookupService.GlobalSkills.FirstOrDefault(x => x.Id == id).Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ToJobStatuseName(this int id)
        {
            try
            {
                return LookupService.JobStatuses.FirstOrDefault(x => x.Id == id).Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ToJobTypeName(this int id)
        {
            try
            {
                return LookupService.JobTypes.FirstOrDefault(x => x.Id == id).Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ToSalaryTypeName(this int id)
        {
            try
            {
                return LookupService.SalaryTypes.FirstOrDefault(x => x.Id == id).Name;
            }
            catch (System.Exception)
            {
                return string.Empty;
            }

        }

        public static string ToUserTypeName(this int id)
        {
            try
            {
                return LookupService.UserTypes.FirstOrDefault(x => x.Id == id).Name;
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }

        public static string ToAvailabilityType(this string id)
        {
            switch (id)
            {
                case ("0"):
                    return "0";
                case ("1"):
                    return "Full-Time";
                case ("2"):
                    return "Part-Time";
                case ("3"):
                    return "Freelance";
                default:
                    return "0";
            }
        }
        public static string ReplaceInformation(this string text, int userType, bool isApplicable)
        {
            if (isApplicable is true)
            {
                string filteredText = text;
                string hiddenText = userType == (int)UserTypeEnum.Worker ? "[hidden]" : "[unlock]";
                const string emailPattern = @"([\w-.]+)@(([[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.)|(([\w-]+.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(]?)";
                const string domainPattern = @"(http[s]?:\/\/|[a-z]*\.[a-z]{3}\.[a-z]{2})([a-z]*\.[a-z]{3})|([a-z]*\.[a-z]*\.[a-z]{3}\.[a-z]{2})|([a-z]+\.[a-z]{3})";
                const string phonePattern = @"\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})";
                const string SkypePattern = @"((S|s){1}kype:? ?(is:? ?)?)([\w-]{2,})";
                const string WhatsappPattern = @"(whats ?app:? ?(is:? ?)?)([\w-]{2,})";
                const string numberPattern = @"\b[\d]{2} ([\d]{2,4} ?){4}";
                if (filteredText != null)
                {
                    // Replace emails.
                    filteredText = Regex.Replace(filteredText, emailPattern, hiddenText);
                    // Replace domain names
                    filteredText = Regex.Replace(filteredText, domainPattern, hiddenText);
                    // Replace Skype
                    filteredText = Regex.Replace(filteredText, SkypePattern, m => m.Groups[1].Value + hiddenText, RegexOptions.IgnoreCase);
                    // Replace Whatsapp
                    filteredText = Regex.Replace(filteredText, WhatsappPattern, m => m.Groups[1].Value + hiddenText, RegexOptions.IgnoreCase);
                    // Replace phone numbers
                    filteredText = Regex.Replace(filteredText, phonePattern, hiddenText);
                    // Replace number in the format format 49 89 412 07 269
                    filteredText = Regex.Replace(filteredText, numberPattern, hiddenText);
                }
                return filteredText;
            }
            else
            {
                return text;
            }
        }
        public static string ReplaceGlobalJobTitleInformation(this string text)
        {
            string filteredText = text;
            string hiddenText = "[hidden]";
            const string emailPattern = @"([\w-.]+)@(([[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.)|(([\w-]+.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(]?)";
            const string domainPattern = @"(http[s]?:\/\/|[a-z]*\.[a-z]{3}\.[a-z]{2})([a-z]*\.[a-z]{3})|([a-z]*\.[a-z]*\.[a-z]{3}\.[a-z]{2})|([a-z]+\.[a-z]{3})";
            const string phonePattern = @"\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})";
            const string SkypePattern = @"((S|s){1}kype:? ?(is:? ?)?)([\w-]{2,})";
            const string WhatsappPattern = @"(whats ?app:? ?(is:? ?)?)([\w-]{2,})";
            const string numberPattern = @"\b[\d]{2} ([\d]{2,4} ?){4}";
            if (filteredText != null)
            {
                // Replace emails.
                filteredText = Regex.Replace(filteredText, emailPattern, hiddenText);
                // Replace domain names
                filteredText = Regex.Replace(filteredText, domainPattern, hiddenText);
                // Replace Skype
                filteredText = Regex.Replace(filteredText, SkypePattern, m => m.Groups[1].Value + hiddenText, RegexOptions.IgnoreCase);
                // Replace Whatsapp
                filteredText = Regex.Replace(filteredText, WhatsappPattern, m => m.Groups[1].Value + hiddenText, RegexOptions.IgnoreCase);
                // Replace phone numbers
                filteredText = Regex.Replace(filteredText, phonePattern, hiddenText);
                // Replace number in the format format 49 89 412 07 269
                filteredText = Regex.Replace(filteredText, numberPattern, hiddenText);
            }
            return filteredText;
        }


    }
}

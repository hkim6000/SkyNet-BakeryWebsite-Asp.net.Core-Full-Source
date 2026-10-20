using System.Globalization;
using System.Net;

namespace Bakery.Core
{
    public static class Ui
    {
        public static string H(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        public static string Js(string? value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'").Replace("<", "\\x3C").Replace("\r", " ").Replace("\n", " ");
        }

        public static string Money(decimal value)
        {
            return "$" + value.ToString("#,0.00", CultureInfo.InvariantCulture);
        }

        public static string Count(int n, string one, string many)
        {
            return n.ToString(CultureInfo.InvariantCulture) + " " + (n == 1 ? one : many);
        }

        public static string Clean(string? value, int max)
        {
            string v = (value ?? string.Empty).Trim();
            return v.Length > max ? v.Substring(0, max) : v;
        }

        public static string Pick(string? value, IEnumerable<string> allowed)
        {
            string v = (value ?? string.Empty).Trim().ToLowerInvariant();
            return allowed.Contains(v) ? v : string.Empty;
        }

        public static int Bound(string? value, int min, int max, int fallback)
        {
            return int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? Math.Min(max, Math.Max(min, n)) : fallback;
        }

        public static bool Decimal(string? value, out decimal result)
        {
            return decimal.TryParse((value ?? string.Empty).Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        public static bool IsEmail(string v)
        {
            if (v.Length < 5 || v.Length > 80 || v.Contains(' '))
            {
                return false;
            }
            int at = v.IndexOf('@');
            int dot = v.LastIndexOf('.');
            return at > 0 && at == v.LastIndexOf('@') && dot > at + 1 && dot < v.Length - 1;
        }

        public static bool IsPhone(string v)
        {
            int digits = v.Count(char.IsDigit);
            return digits >= 7 && digits <= 15 && v.All(ch => char.IsDigit(ch) || " +-().".Contains(ch));
        }

        public static string Day(string iso)
        {
            return DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) ? d.ToString("ddd d MMM", CultureInfo.InvariantCulture) : iso;
        }

        public static string Stamp(DateTime when)
        {
            return when.ToLocalTime().ToString("d MMM, HH:mm", CultureInfo.InvariantCulture);
        }

        public static string StatusLabel(string status)
        {
            return status switch
            {
                "new" => "New",
                "baking" => "Baking",
                "ready" => "Ready",
                "done" => "Done",
                "cancelled" => "Cancelled",
                _ => status
            };
        }
    }
}

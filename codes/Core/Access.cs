using System.Security.Cryptography;
using Bakery.Models;

namespace Bakery.Core
{
    public static class Access
    {
        public static readonly Area[] Areas =
        {
            new Area { Key = "orders", Name = "Orders", Page = "AdminOrders", Extra = "Cancel orders" },
            new Area { Key = "products", Name = "Products", Page = "AdminProducts" },
            new Area { Key = "messages", Name = "Messages", Page = "AdminMessages" },
            new Area { Key = "activity", Name = "Activity log", Page = "AdminActivity", NoEdit = true },
            new Area { Key = "settings", Name = "Settings", Page = "AdminSettings", AdminOnly = true },
            new Area { Key = "users", Name = "Users", Page = "AdminUsers", AdminOnly = true }
        };

        private const string PassChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        public static Area? Find(string key)
        {
            return Areas.FirstOrDefault(a => a.Key == key);
        }

        public static string Cleaned(string? letters, Area area)
        {
            string v = letters ?? string.Empty;
            bool view = v.Contains('V') || v.Contains('E') || v.Contains('X');
            if (!view)
            {
                return string.Empty;
            }
            if (area.NoEdit)
            {
                return "V";
            }
            bool edit = v.Contains('E');
            bool extra = edit && v.Contains('X') && area.Extra != string.Empty;
            return "V" + (edit ? "E" : string.Empty) + (extra ? "X" : string.Empty);
        }

        public static string Level(Staff user, string areaKey)
        {
            Area? area = Find(areaKey);
            if (area == null || !user.Active)
            {
                return string.Empty;
            }
            if (user.Admin)
            {
                return Cleaned("VEX", area);
            }
            if (area.AdminOnly)
            {
                return string.Empty;
            }
            return user.Perms.TryGetValue(areaKey, out string? letters) ? Cleaned(letters, area) : string.Empty;
        }

        public static bool Can(Staff user, string areaKey, char right)
        {
            return Level(user, areaKey).Contains(right);
        }

        public static string NewPassword()
        {
            char[] chars = new char[12];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = PassChars[RandomNumberGenerator.GetInt32(PassChars.Length)];
            }
            string s = new string(chars);
            return s.Substring(0, 4) + "-" + s.Substring(4, 4) + "-" + s.Substring(8, 4);
        }

        public static string NewToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        }

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            return "pbkdf2$100000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            string[] parts = (stored ?? string.Empty).Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2" || !int.TryParse(parts[1], out int rounds))
            {
                return false;
            }
            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] want = Convert.FromBase64String(parts[3]);
                byte[] got = Rfc2898DeriveBytes.Pbkdf2(password, salt, rounds, HashAlgorithmName.SHA256, want.Length);
                return CryptographicOperations.FixedTimeEquals(want, got);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string PasswordProblem(string password, string current)
        {
            if (password.Length < 8)
            {
                return "Use at least 8 characters.";
            }
            if (password.Length > 100)
            {
                return "Use 100 characters or fewer.";
            }
            if (current != string.Empty && password == current)
            {
                return "Choose a different password from the current one.";
            }
            return string.Empty;
        }
    }
}

using System.Text.Json;
using Bakery.Models;

namespace Bakery.Core
{
    public static class Store
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim LogGate = new SemaphoreSlim(1, 1);
        private static readonly JsonSerializerOptions Opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

        public static string Root => Directory.GetCurrentDirectory();

        public static string InitialFile => Path.Combine(Root, "appConfig", "initial-admin.txt");

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim gate;

            public Releaser(SemaphoreSlim gate)
            {
                this.gate = gate;
            }

            public void Dispose()
            {
                gate.Release();
            }
        }

        public static async Task<IDisposable> Hold()
        {
            await Gate.WaitAsync();
            return new Releaser(Gate);
        }

        public static async Task<T> Read<T>(string folder, string name) where T : new()
        {
            string file = Path.Combine(Root, folder, name);
            if (!File.Exists(file))
            {
                return new T();
            }
            try
            {
                return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(file), Opts) ?? new T();
            }
            catch (JsonException)
            {
                return new T();
            }
        }

        public static async Task Write<T>(string folder, string name, T value)
        {
            string dir = Path.Combine(Root, folder);
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, name);
            string tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(value, Opts));
            File.Move(tmp, file, true);
        }

        public static Task<SiteData> LoadSite()
        {
            return Read<SiteData>("data", "site.json");
        }

        public static Task SaveSite(SiteData site)
        {
            return Write("data", "site.json", site);
        }

        public static Task<List<Staff>> LoadStaff()
        {
            return Read<List<Staff>>("data", "staff.json");
        }

        public static Task SaveStaff(List<Staff> staff)
        {
            return Write("data", "staff.json", staff);
        }

        public static Task<List<BakeOrder>> LoadOrders()
        {
            return Read<List<BakeOrder>>("data", "orders.json");
        }

        public static Task SaveOrders(List<BakeOrder> orders)
        {
            return Write("data", "orders.json", orders);
        }

        public static Task<List<Enquiry>> LoadMessages()
        {
            return Read<List<Enquiry>>("data", "messages.json");
        }

        public static Task SaveMessages(List<Enquiry> messages)
        {
            return Write("data", "messages.json", messages);
        }

        public static Task<List<ActivityEntry>> LoadLog()
        {
            return Read<List<ActivityEntry>>("data", "activity.json");
        }

        public static async Task Log(string userId, string action, string target, bool ok)
        {
            await LogGate.WaitAsync();
            try
            {
                List<ActivityEntry> log = await LoadLog();
                log.Add(new ActivityEntry { When = DateTime.UtcNow, UserId = userId, Action = action, Target = Ui.Clean(target, 120), Ok = ok });
                if (log.Count > 5000)
                {
                    log.RemoveRange(0, log.Count - 5000);
                }
                await Write("data", "activity.json", log);
            }
            finally
            {
                LogGate.Release();
            }
        }

        public static string InitialPassword()
        {
            if (!File.Exists(InitialFile))
            {
                return string.Empty;
            }
            string line = File.ReadAllLines(InitialFile).FirstOrDefault(l => l.StartsWith("password: ", StringComparison.Ordinal)) ?? string.Empty;
            return line.Length > 10 ? line.Substring(10).Trim() : string.Empty;
        }

        public static async Task EnsureFirstManager()
        {
            using (await Hold())
            {
                List<Staff> staff = await LoadStaff();
                if (staff.Any(u => u.Admin && u.Active && !u.MustChange && u.Hash != string.Empty))
                {
                    return;
                }
                if (File.Exists(InitialFile) && staff.Any(u => u.Admin && u.Active && u.MustChange))
                {
                    return;
                }
                string pass = Access.NewPassword();
                Staff? u = staff.FirstOrDefault(x => x.Login == "admin");
                if (u == null)
                {
                    u = new Staff { Id = "u-admin", Login = "admin", Name = "Administrator", Title = "Manager" };
                    staff.Insert(0, u);
                }
                u.Admin = true;
                u.Active = true;
                u.MustChange = true;
                u.Hash = Access.HashPassword(pass);
                await SaveStaff(staff);
                Directory.CreateDirectory(Path.GetDirectoryName(InitialFile) ?? Root);
                await File.WriteAllTextAsync(InitialFile, "user: admin" + Environment.NewLine + "password: " + pass + Environment.NewLine);
            }
        }

        public static int NextOrderNumber(List<BakeOrder> orders)
        {
            return orders.Count == 0 ? 1001 : orders.Max(o => o.Number) + 1;
        }
    }
}

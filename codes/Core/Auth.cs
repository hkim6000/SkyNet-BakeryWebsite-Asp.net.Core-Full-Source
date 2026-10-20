using System.Text;
using Bakery.Models;
using SkyNet;

namespace Bakery.Core
{
    public static class Auth
    {
        private static readonly SemaphoreSlim SessionGate = new SemaphoreSlim(1, 1);
        private static readonly Dictionary<string, List<DateTime>> Failures = new Dictionary<string, List<DateTime>>();
        private static readonly object FailLock = new object();

        private const string SessionFile = "sessions.json";
        private const string SessionFolder = "temp/admin";

        public static bool Paused(string login)
        {
            lock (FailLock)
            {
                if (!Failures.TryGetValue(login, out List<DateTime>? list))
                {
                    return false;
                }
                list.RemoveAll(t => t < DateTime.UtcNow.AddMinutes(-5));
                return list.Count >= 6;
            }
        }

        public static void Failed(string login)
        {
            lock (FailLock)
            {
                if (!Failures.TryGetValue(login, out List<DateTime>? list))
                {
                    list = new List<DateTime>();
                    Failures[login] = list;
                }
                list.Add(DateTime.UtcNow);
            }
        }

        public static void Cleared(string login)
        {
            lock (FailLock)
            {
                Failures.Remove(login);
            }
        }

        public static async Task<string> Open(string userId)
        {
            await SessionGate.WaitAsync();
            try
            {
                List<SessionEntry> list = await Store.Read<List<SessionEntry>>(SessionFolder, SessionFile);
                list.RemoveAll(s => s.Expires < DateTime.UtcNow);
                string token = Access.NewToken();
                list.Add(new SessionEntry { Token = token, UserId = userId, Expires = DateTime.UtcNow.AddHours(8) });
                await Store.Write(SessionFolder, SessionFile, list);
                return token;
            }
            finally
            {
                SessionGate.Release();
            }
        }

        public static async Task Close(string token)
        {
            await SessionGate.WaitAsync();
            try
            {
                List<SessionEntry> list = await Store.Read<List<SessionEntry>>(SessionFolder, SessionFile);
                if (list.RemoveAll(s => s.Token == token) > 0)
                {
                    await Store.Write(SessionFolder, SessionFile, list);
                }
            }
            finally
            {
                SessionGate.Release();
            }
        }

        public static async Task CloseAll(string userId, string keepToken)
        {
            await SessionGate.WaitAsync();
            try
            {
                List<SessionEntry> list = await Store.Read<List<SessionEntry>>(SessionFolder, SessionFile);
                if (list.RemoveAll(s => s.UserId == userId && s.Token != keepToken) > 0)
                {
                    await Store.Write(SessionFolder, SessionFile, list);
                }
            }
            finally
            {
                SessionGate.Release();
            }
        }

        public static async Task<Ctx?> Check(string? token)
        {
            string tok = (token ?? string.Empty).Trim();
            if (tok.Length != 32 || !tok.All(Uri.IsHexDigit))
            {
                return null;
            }
            string userId;
            await SessionGate.WaitAsync();
            try
            {
                List<SessionEntry> list = await Store.Read<List<SessionEntry>>(SessionFolder, SessionFile);
                SessionEntry? s = list.FirstOrDefault(x => x.Token == tok && x.Expires > DateTime.UtcNow);
                if (s == null)
                {
                    return null;
                }
                userId = s.UserId;
                if (s.Expires < DateTime.UtcNow.AddHours(7))
                {
                    s.Expires = DateTime.UtcNow.AddHours(8);
                    await Store.Write(SessionFolder, SessionFile, list);
                }
            }
            finally
            {
                SessionGate.Release();
            }
            List<Staff> staff = await Store.LoadStaff();
            Staff? u = staff.FirstOrDefault(x => x.Id == userId && x.Active && !x.MustChange);
            return u == null ? null : new Ctx { U = u, Token = tok };
        }

        public static async Task<string> Menu(string p, string current, Ctx c, SiteData site)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Item(p, current, "Admin", "Dashboard", 0));
            foreach (Area a in Access.Areas)
            {
                string level = Access.Level(c.U, a.Key);
                if (a.Key == "activity" && level == string.Empty)
                {
                    sb.Append(Item(p, current, a.Page, "My activity", 0));
                    continue;
                }
                if (level == string.Empty)
                {
                    continue;
                }
                int badge = 0;
                if (a.Key == "orders")
                {
                    badge = (await Store.LoadOrders()).Count(o => o.Status == "new");
                }
                else if (a.Key == "messages")
                {
                    badge = (await Store.LoadMessages()).Count(m => !m.Read);
                }
                sb.Append(Item(p, current, a.Page, a.Name, badge));
            }
            sb.Append(Item(p, current, "AdminAccount", "My account", 0));
            return sb.ToString();
        }

        private static string Item(string p, string current, string page, string label, int badge)
        {
            return "<a class=\"" + p + "-ml" + (page == current ? " " + p + "-on" : string.Empty) + "\" href=\"" + page + "\"><span>" + Ui.H(label) + "</span>" + (badge > 0 ? "<i class=\"" + p + "-badge\">" + badge + "</i>" : string.Empty) + "</a>";
        }

        public static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string ns, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript(ns + ".toast('" + Ui.Js(message) + "');");
            return response;
        }

        public static ApiResponse Expired(ApiResponse response, string ns)
        {
            response.ExecuteScript(ns + ".expired();");
            return response;
        }

        public static string NoAccess(string p, string area)
        {
            return "<div class=\"" + p + "-card\"><h2>No access</h2><p class=\"" + p + "-muted\">You do not have access to " + Ui.H(area) + ". Ask a manager if you need it.</p></div>";
        }

        public static string Who(string p, Ctx c)
        {
            return "<b>" + Ui.H(c.U.Name) + "</b><small>" + Ui.H(c.U.Admin ? "Manager" : c.U.Title) + "</small>";
        }
    }
}

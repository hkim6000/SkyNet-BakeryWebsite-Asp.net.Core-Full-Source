namespace Bakery.Models
{
    public class SiteData
    {
        public string Brand { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<HourLine> Hours { get; set; } = new List<HourLine>();
        public OrderRules Rules { get; set; } = new OrderRules();
        public List<ProductGroup> Categories { get; set; } = new List<ProductGroup>();
        public List<Product> Products { get; set; } = new List<Product>();
        public List<TeamMember> Team { get; set; } = new List<TeamMember>();
        public List<Faq> Faqs { get; set; } = new List<Faq>();
    }

    public class HourLine
    {
        public string Days { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

    public class OrderRules
    {
        public decimal DeliveryFee { get; set; } = 4.5m;
        public decimal FreeDeliveryOver { get; set; } = 40m;
        public decimal MinDelivery { get; set; } = 15m;
        public int LeadHours { get; set; } = 12;
        public int DaysAhead { get; set; } = 14;
        public int SlotCapacity { get; set; } = 6;
        public List<int> OpenDays { get; set; } = new List<int> { 2, 3, 4, 5, 6 };
        public List<string> Slots { get; set; } = new List<string>();
        public List<string> DeliveryZips { get; set; } = new List<string>();
        public List<string> ClosedDates { get; set; } = new List<string>();
    }

    public class ProductGroup
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class Product
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public bool Available { get; set; } = true;
        public bool Featured { get; set; }
    }

    public class TeamMember
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class Faq
    {
        public string Q { get; set; } = string.Empty;
        public string A { get; set; } = string.Empty;
    }

    public class OrderLine
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal Price { get; set; }
    }

    public class BakeOrder
    {
        public int Number { get; set; }
        public string Id { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public string Status { get; set; } = "new";
        public string Mode { get; set; } = "pickup";
        public string Date { get; set; } = string.Empty;
        public string Slot { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<OrderLine> Lines { get; set; } = new List<OrderLine>();
        public decimal Subtotal { get; set; }
        public decimal Delivery { get; set; }
        public decimal Total { get; set; }
        public string CancelReason { get; set; } = string.Empty;
    }

    public class Enquiry
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime When { get; set; }
        public bool Read { get; set; }
    }

    public class Staff
    {
        public string Id { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public bool Admin { get; set; }
        public bool Active { get; set; } = true;
        public bool MustChange { get; set; }
        public Dictionary<string, string> Perms { get; set; } = new Dictionary<string, string>();
    }

    public class ActivityEntry
    {
        public DateTime When { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public bool Ok { get; set; } = true;
    }

    public class SessionEntry
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
    }

    public class Area
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Page { get; init; } = string.Empty;
        public string Extra { get; init; } = string.Empty;
        public bool AdminOnly { get; init; }
        public bool NoEdit { get; init; }
    }

    public class Ctx
    {
        public Staff U { get; init; } = new Staff();
        public string Token { get; init; } = string.Empty;
    }
}

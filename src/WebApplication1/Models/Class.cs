namespace WebApplication1.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class StandardProduct : Product
    {
        public string Tier => "Standard";
    }

    public class PremiumProduct : Product
    {
        public string Tier => "Premium";
    }

    public class BasicProduct : Product
    {
        public string Tier => "Basic";
    }


}

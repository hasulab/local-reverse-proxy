using System.Text.Json;
using System.Text.Json.Serialization;
using WebApplication1.Models;

namespace WebApplication1.Converters
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ProductPolymorphicConverter : JsonConverter<Product>
    {
        public override Product? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            int id = root.GetProperty("id").GetInt32();
            string name = root.GetProperty("name").GetString() ?? "";
            decimal price = root.GetProperty("price").GetDecimal();

            return id switch
            {
                10 => new StandardProduct { Id = id, Name = name, Price = price },
                11 => new PremiumProduct { Id = id, Name = name, Price = price },
                _ => new BasicProduct { Id = id, Name = name, Price = price }
            };
        }

        public override void Write(Utf8JsonWriter writer, Product value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Id", value.Id);
            writer.WriteString("Name", value.Name);
            writer.WriteNumber("Price", value.Price);

            string tier = value switch
            {
                StandardProduct => "Standard",
                PremiumProduct => "Premium",
                BasicProduct => "Basic",
                _ => "Unknown"
            };

            writer.WriteString("Tier", tier);
            writer.WriteEndObject();
        }
    }


}

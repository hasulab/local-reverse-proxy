using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using WebApplication1.Converters;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
                var products = new List<Product>
            {
                new Product { Id = 1, Name = "Standard Widget", Price = 9.99m },
                new Product { Id = 42, Name = "Secret Gadget", Price = 999.99m }
            };

            return Ok(products); // Will use your custom converter
        }
        [HttpPost]
        public IActionResult Post([FromBody] ProductWrapper productWrapper)
        {
            if (productWrapper?.Product == null)
            {
                return BadRequest("Invalid product data.");
            }
            // Here you would typically save the product to a database
            // For demonstration, we just return it back
            return Ok(productWrapper.Product);
        }
    }


    public class ProductWrapper
    {
        [JsonConverter(typeof(ProductPolymorphicConverter))]
        public Product Product { get; set; }
    }

}

using Microsoft.EntityFrameworkCore;
using ElasticsearchQueryLucene.FunctionalTests.TestUtilities;

namespace ElasticsearchQueryLucene.FunctionalTests;

public class ComplexQueryTests : LuceneTestBase
{
    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public double Price { get; set; }
    }

    private class ProductContext : DbContext
    {
        public ProductContext(DbContextOptions options) : base(options) { }
        public DbSet<Product> Products => Set<Product>();
    }

    [Fact]
    public void Can_perform_projections_and_ordering()
    {
        var options = CreateOptions<ProductContext>();

        using (var context = new ProductContext(options))
        {
            context.Products.AddRange(
                new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 1200 },
                new Product { Id = 2, Name = "Phone", Category = "Electronics", Price = 800 },
                new Product { Id = 3, Name = "Coffee Maker", Category = "Appliances", Price = 100 },
                new Product { Id = 4, Name = "Toaster", Category = "Appliances", Price = 50 }
            );
            context.SaveChanges();
        }

        using (var context = new ProductContext(options))
        {
            // Simple projection
            var names = context.Products
                .OrderBy(p => p.Price)
                .Select(p => p.Name)
                .ToList();

            Assert.Equal(4, names.Count);
            Assert.Equal("Toaster", names[0]);
            Assert.Equal("Laptop", names[3]);

            // Anonymous type projection
            var query = context.Products
                .Where(p => p.Category == "Electronics")
                .OrderByDescending(p => p.Price)
                .Select(p => new { p.Name, p.Price })
                .ToList();

            Assert.Equal(2, query.Count);
            Assert.Equal("Laptop", query[0].Name);
            Assert.Equal(1200, query[0].Price);
            Assert.Equal("Phone", query[1].Name);
        }
    }

    [Fact]
    public void Can_perform_paging()
    {
        var options = CreateOptions<ProductContext>();

        using (var context = new ProductContext(options))
        {
            for (int i = 1; i <= 20; i++)
            {
                context.Products.Add(new Product { Id = i, Name = $"Product {i}", Price = i * 10 });
            }
            context.SaveChanges();
        }

        using (var context = new ProductContext(options))
        {
            var page = context.Products
                .OrderBy(p => p.Price)
                .Skip(5)
                .Take(5)
                .ToList();

            Assert.Equal(5, page.Count);
            Assert.Equal("Product 6", page[0].Name);
            Assert.Equal("Product 10", page[4].Name);
        }
    }
}

using System.Reflection;
using Lucene.Net.Store;
using Microsoft.EntityFrameworkCore;
using ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;
using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup Lucene and Entity Framework
var indexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demo_index");
var luceneDir = FSDirectory.Open(indexPath);

builder.Services.AddDbContext<DemoContext>(options =>
{
    options.UseLucene(luceneDir, "products");
    options.LogTo(Console.WriteLine, LogLevel.Information); // Diagnostic Logging
});

var app = builder.Build();

// 2. Enable Lucene Explorer Dashboard
app.UseLuceneExplorer<DemoContext>("/diagnostics/lucene");

// 3. API Endpoints
app.MapGet("/", () => "🚀 Lucene DX Pack Web Demo. \n- Dashboard: /diagnostics/lucene \n- Guide: See Example/ElasticsearchQueryLucene.WebDemo/GUIDE.md");

app.MapPost("/seed", async (DemoContext db) =>
{
    await db.Database.EnsureCreatedAsync();
    
    // Clear existing
    var all = await db.Products.ToListAsync();
    db.Products.RemoveRange(all);
    await db.SaveChangesAsync();

    db.Products.AddRange(
        new Product { Name = "iPhone 15 Pro", Category = "Smartphones", Description = "Latest Apple flagship with Titanium design.", Price = 999 },
        new Product { Name = "Samsung Galaxy S23", Category = "Smartphones", Description = "Powerful Android flagship with great camera.", Price = 899 },
        new Product { Name = "MacBook Air M2", Category = "Laptops", Description = "Thinner, lighter laptop with Apple silicon.", Price = 1199 },
        new Product { Name = "Dell XPS 15", Category = "Laptops", Description = "Premium Windows laptop with InfinityEdge display.", Price = 1599 },
        new Product { Name = "Sony WH-1000XM5", Category = "Accessories", Description = "Industry-leading noise canceling headphones.", Price = 399 }
    );
    
    await db.SaveChangesAsync();
    return Results.Ok("Seed success! 5 products added.");
});

app.MapGet("/search", async (DemoContext db, string q, float? nameBoost, float? descBoost) =>
{
    // Demonstrate Field Boosting
    // We want to find products where the query matches Name (high priority) OR Description (low priority)
    var query = db.Products.AsNoTracking();

    if (!string.IsNullOrEmpty(q))
    {
        query = query.Where(p => 
            EF.Functions.Boost(p.Name, nameBoost ?? 2.0f).Contains(q) || 
            EF.Functions.Boost(p.Description, descBoost ?? 0.5f).Contains(q));
    }

    var results = await query.ToListAsync();
    return Results.Ok(new {
        Query = q,
        Results = results
    });
});

app.Run();

// 4. Data Model
public class DemoContext : DbContext
{
    public DemoContext(DbContextOptions options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
}

public class Product
{
    public int Id { get; set; }

    [LuceneField(Stored = true, Tokenized = true)]
    public string Name { get; set; } = "";

    [LuceneField(Stored = true, Tokenized = false)]
    public string Category { get; set; } = "";

    [LuceneField(Stored = true, Tokenized = true)]
    public string Description { get; set; } = "";

    [LuceneField(Stored = true)]
    public double Price { get; set; }
}

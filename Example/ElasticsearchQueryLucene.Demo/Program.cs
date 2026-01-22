using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Lucene.Net.Store;
using Lucene.Net.Util;
using ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;
using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;

namespace ElasticsearchQueryLucene.Demo;

public class Program
{
    public static void Main()
    {
        try
        {
            RunDemo();
        }
        catch (Exception ex)
        {
            var errorMsg = $@"
            ========== CRITICAL ERROR ==========
            Message: {ex.Message}
            Type: {ex.GetType().FullName}
            Stack Trace:
            {ex.StackTrace}

            {(ex.InnerException != null ? $@"--- Inner Exception ---
            Message: {ex.InnerException.Message}
            Type: {ex.InnerException.GetType().FullName}
            Stack Trace:
            {ex.InnerException.StackTrace}" : "")}

            ====================================";
            
            Console.WriteLine(errorMsg);
            System.IO.File.WriteAllText("error_details.txt", errorMsg);
        }
    }

    public static void RunDemo()
    {
        Console.WriteLine("🐰 Lucenet's Pet Shelter Demo 🐶");
        Console.WriteLine("=================================");

        // Index Storage Path
        var indexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pet_index");
        Console.WriteLine($"Index Path: {indexPath}");
        
        // Ensure clean slate
        // if (System.IO.Directory.Exists(indexPath)) 
        //    System.IO.Directory.Delete(indexPath, true);

        // Open Lucene Directory
        using var luceneDir = new RAMDirectory();

        // 1. CREATE
        Console.WriteLine("\n[1] Seeding Data...");
        var options = new DbContextOptionsBuilder<PetContext>()
            .UseLucene(luceneDir, "pets")
            .Options;

        try
        {
            Console.WriteLine("Creating context...");
            using (var context = new PetContext(options))
            {
                Console.WriteLine("Context created successfully!");
                context.Database.EnsureCreated();
            
            context.Pets.AddRange(
                new Pet { Id = 1, Name = "Buddy", Breed = "Golden Retriever", Age = 3, Description = "Very friendly and loves playing fetch.", IsAdopted = false },
                new Pet { Id = 2, Name = "Whiskers", Breed = "Siamese Cat", Age = 2, Description = "Independent but loves cuddles at night.", IsAdopted = false },
                new Pet { Id = 3, Name = "Polly", Breed = "Parrot", Age = 10, Description = "Talkative and loves crackers.", IsAdopted = false },
                new Pet { Id = 4, Name = "Rex", Breed = "German Shepherd", Age = 5, Description = "Loyal guard dog, very intelligent.", IsAdopted = true }
            );
            context.SaveChanges();
            Console.WriteLine("   -> 4 Pets added.");
        }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n!!! Error creating/using context !!!");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Type: {ex.GetType().FullName}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
                Console.WriteLine($"Inner Type: {ex.InnerException.GetType().FullName}");
            }
            throw;
        }

        // 2. READ & SEARCH
        using (var context = new PetContext(options))
        {
            Console.WriteLine("\n[2] Search Demonstrations:");

            // A. Basic LINQ (Full Text)
            var term = "loves";
            Console.WriteLine($"   A. Searching for '{term}' (Contains):");
            var results = context.Pets.AsNoTracking()
                .Where(p => p.Description.Contains("loves"))
                .ToList();
            PrintPets(results);

            // B. Exact Match
            Console.WriteLine("   B. Filter by Breed == 'Siamese Cat':");
            var cats = context.Pets.AsNoTracking()
                .Where(p => p.Breed == "Siamese Cat")
                .ToList();
            PrintPets(cats);

            // C. Ordering
            Console.WriteLine("   C. Ordered by Age Descending:");
            var ordered = context.Pets.AsNoTracking()
                .OrderByDescending(p => p.Age)
                .ToList();
            PrintPets(ordered);

            // D. Raw Lucene Match (Fuzzy)
            // 'inteligent' is typo for 'intelligent'
            var fuzzyQuery = "inteligent~"; 
            Console.WriteLine($"   D. Raw Lucene Fuzzy Search ('{fuzzyQuery}'):");
            var fuzzyResults = context.Pets.AsNoTracking()
                .Where(p => EF.Functions.LuceneMatch(p.Description, "inteligent~"))
                .ToList();
            PrintPets(fuzzyResults);
        }

        // 3. UPDATE
        Console.WriteLine("\n[3] Update Operation:");
        using (var context = new PetContext(options))
        {
            var buddy = context.Pets.FirstOrDefault(p => p.Name == "Buddy");
            if (buddy != null)
            {
                Console.WriteLine($"   Updating {buddy.Name}...");
                buddy.IsAdopted = true;
                buddy.Description += " (Adopted!)";
                context.SaveChanges();
                Console.WriteLine("   -> Buddy marked as adopted and description updated.");
            }
        }

        // Verify Update
        using (var context = new PetContext(options))
        {
            var buddy = context.Pets.First(p => p.Name == "Buddy");
            Console.WriteLine($"   Verified: {buddy.Name} - Adopted: {buddy.IsAdopted}, Desc: {buddy.Description}");
        }

        // 4. DELETE
        Console.WriteLine("\n[4] Delete Operation:");
        using (var context = new PetContext(options))
        {
            var polly = context.Pets.FirstOrDefault(p => p.Name == "Polly");
            if (polly != null)
            {
                Console.WriteLine($"   Deleting {polly.Name}...");
                context.Pets.Remove(polly);
                context.SaveChanges();
                Console.WriteLine("   -> Polly removed.");
            }
        }

        // Verify Delete
        using (var context = new PetContext(options))
        {
            var count = context.Pets.Count();
            Console.WriteLine($"   Total Pets Remaining: {count} (Expected 3)");

            var any = context.Pets.Any(p => p.Name == "Rex");
            Console.WriteLine($"   Any Check (Rex): {any} (Expected True)");

            var notAny = context.Pets.Any(p => p.Name == "Polly");
            Console.WriteLine($"   Any Check (Polly): {notAny} (Expected False - Deleted)");

            var longCount = context.Pets.LongCount();
            Console.WriteLine($"   Long Count: {longCount} (Expected 3)");
        }
        
        Console.WriteLine("\nDemo Complete! 🚀");
    }

    static void PrintPets(System.Collections.Generic.List<Pet> pets)
    {
        if (pets.Count == 0) Console.WriteLine("      (No results)");
        foreach (var p in pets)
        {
            Console.WriteLine($"      - [{p.Id}] {p.Name} ({p.Breed}, {p.Age}yo): {p.Description}");
        }
    }
}

public class PetContext : DbContext
{
    public PetContext(DbContextOptions options) : base(options) { }
    public DbSet<Pet> Pets => Set<Pet>();
}

public class Pet
{
    public int Id { get; set; }

    [LuceneField(Stored = true, Tokenized = true)]
    public string Name { get; set; } = "";

    // Not Tokenized = Exact Match
    [LuceneField(Stored = true, Tokenized = false)] 
    public string Breed { get; set; } = "";

    [LuceneField(Stored = true, Tokenized = true, Analyzer = "StandardAnalyzer")]
    public string Description { get; set; } = "";

    [LuceneField(Stored = true)]
    public int Age { get; set; }

    [LuceneField(Stored = true)]
    public bool IsAdopted { get; set; }
}

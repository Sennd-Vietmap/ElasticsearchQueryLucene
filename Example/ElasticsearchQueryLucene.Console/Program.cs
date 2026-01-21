using ElasticsearchQueryLucene.Core.Converters;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

Console.WriteLine("Elasticsearch DSL to Lucene Real-Data Testing");
Console.WriteLine("============================================");

// 1. Setup Lucene Index with Book Data
var luceneVersion = LuceneVersion.LUCENE_48;
using var analyzer = new StandardAnalyzer(luceneVersion);
var directory = new RAMDirectory();
var config = new IndexWriterConfig(luceneVersion, analyzer);

using var writer = new IndexWriter(directory, config);

var books = new[]
{
    new { Id = "1", Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Year = 1925, Category = "Fiction", Price = 15.99 },
    new { Id = "2", Title = "To Kill a Mockingbird", Author = "Harper Lee", Year = 1960, Category = "Fiction", Price = 12.50 },
    new { Id = "3", Title = "1984", Author = "George Orwell", Year = 1949, Category = "Dystopian", Price = 10.00 },
    new { Id = "4", Title = "The Hobbit", Author = "J.R.R. Tolkien", Year = 1937, Category = "Fantasy", Price = 25.00 },
    new { Id = "5", Title = "Harry Potter and the Sorcerer's Stone", Author = "J.K. Rowling", Year = 1997, Category = "Fantasy", Price = 20.00 }
};

foreach (var book in books)
{
    var doc = new Document
    {
        new StringField("id", book.Id, Field.Store.YES),
        new TextField("title", book.Title, Field.Store.YES),
        new TextField("author", book.Author, Field.Store.YES),
        new Int32Field("year", book.Year, Field.Store.YES),
        new TextField("category", book.Category, Field.Store.YES),
        new DoubleField("price", book.Price, Field.Store.YES)
    };
    writer.AddDocument(doc);
}
writer.Commit();

// 2. Define Elasticsearch DSL Queries to Test
var queries = new[]
{
    new { Name = "Term Query (Filter by Category)", DSL = "{\"term\": {\"category\": \"Fantasy\"}}" },
    new { Name = "Match Query (Full-text Title Search)", DSL = "{\"match\": {\"title\": \"Great Gatsby\"}}" },
    new { Name = "Range Query (Price <= 15)", DSL = "{\"range\": {\"price\": {\"lte\": 15.0}}}" },
    new { Name = "Bool Query (Fiction AND Author like Lee)", DSL = @"{
        ""bool"": {
            ""must"": [
                { ""term"": { ""category"": ""Fiction"" } },
                { ""match"": { ""author"": ""Lee"" } }
            ]
        }
    }" }
};

// 3. Convert and Execute on Lucene
using var reader = writer.GetReader(applyAllDeletes: true);
var searcher = new IndexSearcher(reader);
var parser = new ElasticsearchQueryLucene.Core.Converters.QueryParser();
var converter = new LuceneQueryVisitor();

foreach (var q in queries)
{
    Console.WriteLine($"\n--- Testing: {q.Name} ---");
    Console.WriteLine($"DSL: {q.DSL}");

    try
    {
        // Conversion
        var queryNode = parser.Parse(q.DSL);
        var visitor = new LuceneQueryVisitor();
        queryNode.Accept(visitor);
        var luceneQueryString = visitor.GetResult();
        Console.WriteLine($"Lucene Syntax: {luceneQueryString}");

        // Lucene Execution
        var luceneParser = new Lucene.Net.QueryParsers.Classic.QueryParser(luceneVersion, "title", analyzer);
        var query = luceneParser.Parse(luceneQueryString);
        
        var hits = searcher.Search(query, 10).ScoreDocs;
        Console.WriteLine($"Results Found: {hits.Length}");

        foreach (var hit in hits)
        {
            var foundDoc = searcher.Doc(hit.Doc);
            Console.WriteLine($"- [{foundDoc.Get("id")}] {foundDoc.Get("title")} by {foundDoc.Get("author")} ({foundDoc.Get("category")})");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

Console.WriteLine("\nTesting Complete.");

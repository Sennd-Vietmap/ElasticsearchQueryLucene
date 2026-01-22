# 🚀 Lucene DX Pack: Web Demo Guide

Welcome to the **ElasticsearchQueryLucene** Developer Experience (DX) Pack demo! This project showcases the powerful search capabilities and diagnostic tools available in version 2.0.2.

---

## 1. Quick Start

### Run the application
Open your terminal in the root of the repository and run:
```powershell
dotnet run --project Example/ElasticsearchQueryLucene.WebDemo
```

Once running, the application is available at: `http://localhost:5000`

---

## 2. Step-by-Step Use Cases

### Case A: Seeding the Index
Before searching, you need data. We've provided an endpoint to initialize and seed the Lucene index with sample products.

*   **Action**: Send a POST request to `/seed`.
*   **Tool**: `curl -X POST http://localhost:5000/seed` or use Postman.
*   **Result**: The index is wiped and 5 premium tech products (iPhones, MacBooks, etc.) are added.

### Case B: Visual Index Explorer (The Dashboard)
The "Lucene Explorer" allows you to look inside the Lucene "black box" without writing any code.

*   **Action**: Open your browser to `http://localhost:5000/diagnostics/lucene`.
*   **Features**:
    *   **Document List**: See all stored documents in a table.
    *   **Field View**: See how different fields (Name, Category, Description) are stored.
    *   **Search Playground**: Type raw Lucene queries (e.g., `Name:Apple AND Price:[1000 TO *]`) and see results instantly.

### Case C: Searching with Field Boosting
This is a "Pro" feature where you can tell the search engine which fields are more important.

*   **Endpoint**: `GET /search?q={query}&nameBoost={factor}&descBoost={factor}`
*   **Try This**:
    *   Find "Apple": `http://localhost:5000/search?q=apple`
    *   **Boost Name**: `http://localhost:5000/search?q=pro&nameBoost=5.0`
        *   Matches in the **Name** property will appear higher/more relevant than matches in the **Description**.

### Case D: Observing Diagnostics
While the app is running, watch your terminal/console.

*   **What to look for**: Every time you hit the `/search` endpoint or use the Dashboard, the console will output:
    *   The **exact Lucene Query** generated (e.g., `Description:*loves*`).
    *   The **Sort Criteria** (e.g., `<int: "Age">!`).
    *   The **Execution Time** in milliseconds.

---

## 3. Under the Hood (For Developers)

Check out `Program.cs` to see how easy it is to enable these features:

```csharp
// Enable Dashboard
app.UseLuceneExplorer<DemoContext>("/diagnostics/lucene");

// Use Boosting in LINQ
query = query.Where(p => 
    EF.Functions.Boost(p.Name, 2.0f).Contains(q) || 
    EF.Functions.Boost(p.Description, 0.5f).Contains(q));
```

Happy Searching! 🐾

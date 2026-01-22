using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ElasticsearchQueryLucene.EntityFrameworkCore.Storage;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Diagnostics;

public class LuceneExplorerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _path;
    private readonly Type? _contextType;

    public LuceneExplorerMiddleware(RequestDelegate next, string path = "/_lucene/explorer", Type? contextType = null)
    {
        _next = next;
        _path = path;
        _contextType = contextType;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(_path))
        {
            ILuceneDatabase? db = null;
            if (_contextType != null)
            {
                var dbContext = context.RequestServices.GetService(_contextType) as DbContext;
                db = dbContext?.GetService<ILuceneDatabase>();
            }
            else
            {
                db = context.RequestServices.GetService<ILuceneDatabase>();
            }

            if (db == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"ILuceneDatabase not found in services. ContextType: {_contextType?.Name ?? "None"}");
                return;
            }

            if (context.Request.Path.Value?.EndsWith("/data") == true)
            {
                await HandleDataRequest(context, db);
                return;
            }

            await HandleHtmlRequest(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleDataRequest(HttpContext context, ILuceneDatabase db)
    {
        int skip = 0;
        int take = 100;
        string? query = null;
        
        if (context.Request.Query.TryGetValue("skip", out var skipStr)) int.TryParse(skipStr, out skip);
        if (context.Request.Query.TryGetValue("take", out var takeStr)) int.TryParse(takeStr, out take);
        if (context.Request.Query.TryGetValue("q", out var q)) query = q.ToString();

        var docs = db.GetIndexDocuments(skip, take, query);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(docs));
    }

    private async Task HandleHtmlRequest(HttpContext context)
    {
        context.Response.ContentType = "text/html";
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Lucene Index Explorer</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 20px; background: #f5f5f7; }
        h1 { color: #1d1d1f; }
        .card { background: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { text-align: left; padding: 12px; border-bottom: 1px solid #e5e5e5; }
        th { background: #f9f9fb; font-weight: 600; color: #86868b; text-transform: uppercase; font-size: 12px; }
        pre { background: #f0f0f2; padding: 5px; border-radius: 4px; font-size: 13px; margin: 0; white-space: pre-wrap; }
        .tag { display: inline-block; background: #e1f5fe; color: #01579b; padding: 2px 8px; border-radius: 12px; font-size: 11px; margin-right: 4px; }
    </style>
</head>
<body>
    <h1>🔍 Lucene Index Explorer</h1>
    <div class=""card"">
        <h3>Search Playground</h3>
        <div style=""display: flex; gap: 10px; margin-bottom: 20px;"">
            <input type=""text"" id=""query"" placeholder=""e.g. Name:Buddy AND Age:[3 TO *]"" style=""flex: 1; padding: 10px; border-radius: 8px; border: 1px solid #ddd;"">
            <button onclick=""performSearch()"" style=""padding: 10px 20px; background: #0071e3; color: white; border: none; border-radius: 8px; cursor: pointer;"">Search</button>
        </div>
        <div id=""stats"">Loading index...</div>
        <div style=""overflow-x: auto;"">
            <table id=""docs-table"">
                <thead><tr id=""header""></tr></thead>
                <tbody id=""body""></tbody>
            </table>
        </div>
    </div>

    <script>
        let allFields = new Set();

        async function performSearch() {
            const query = document.getElementById('query').value;
            const stats = document.getElementById('stats');
            stats.innerText = 'Searching...';
            
            const url = new URL(window.location.pathname + '/data', window.location.origin);
            if (query) url.searchParams.append('q', query);
            
            const res = await fetch(url);
            const data = await res.json();
            render(data);
        }

        function render(data) {
            const stats = document.getElementById('stats');
            const header = document.getElementById('header');
            const body = document.getElementById('body');
            
            header.innerHTML = '';
            body.innerHTML = '';

            if (!data || data.length === 0) {
                stats.innerText = 'No documents found.';
                return;
            }

            stats.innerText = `Found ${data.length} documents.`;
            
            // Re-collect fields to ensure new fields are shown if index changed
            allFields = new Set();
            data.forEach(d => Object.keys(d).forEach(k => allFields.add(k)));
            
            allFields.forEach(f => {
                const th = document.createElement('th');
                th.innerText = f;
                header.appendChild(th);
            });

            data.forEach(doc => {
                const tr = document.createElement('tr');
                allFields.forEach(f => {
                    const td = document.createElement('td');
                    if (doc[f]) {
                        doc[f].forEach(val => {
                            const span = document.createElement('div');
                            span.className = 'tag';
                            span.innerText = val;
                            td.appendChild(span);
                        });
                    }
                    tr.appendChild(td);
                });
                body.appendChild(tr);
            });
        }

        async function load() {
            await performSearch();
        }
        load();
    </script>
</body>
</html>";
        await context.Response.WriteAsync(html);
    }
}

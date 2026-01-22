using Microsoft.EntityFrameworkCore;
using ElasticsearchQueryLucene.FunctionalTests.TestUtilities;
using System.ComponentModel.DataAnnotations;

namespace ElasticsearchQueryLucene.FunctionalTests;

public class BasicCrudTests : LuceneTestBase
{
    private class Blog
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private class BlogContext : DbContext
    {
        public BlogContext(DbContextOptions options) : base(options) { }
        public DbSet<Blog> Blogs => Set<Blog>();
    }

    [Fact]
    public void Can_add_find_delete_blog()
    {
        var options = CreateOptions<BlogContext>();

        // 1. ADD
        using (var context = new BlogContext(options))
        {
            var blog = new Blog { Id = 1, Title = "First Blog", Content = "Hello World" };
            context.Blogs.Add(blog);
            context.SaveChanges();
        }

        // 2. FIND & VERIFY
        using (var context = new BlogContext(options))
        {
            var blog = context.Blogs.FirstOrDefault(b => b.Id == 1);
            Assert.NotNull(blog);
            Assert.Equal("First Blog", blog.Title);
            Assert.Equal("Hello World", blog.Content);
        }

        // 3. DELETE
        using (var context = new BlogContext(options))
        {
            var blog = context.Blogs.FirstOrDefault(b => b.Id == 1);
            Assert.NotNull(blog);
            context.Blogs.Remove(blog);
            context.SaveChanges();
        }

        // 4. VERIFY DELETED
        using (var context = new BlogContext(options))
        {
            var blog = context.Blogs.FirstOrDefault(b => b.Id == 1);
            Assert.Null(blog);
        }
    }

    [Fact]
    public void Can_update_blog_title()
    {
        var options = CreateOptions<BlogContext>();

        // 1. ADD
        using (var context = new BlogContext(options))
        {
            context.Blogs.Add(new Blog { Id = 1, Title = "Initial Title" });
            context.SaveChanges();
        }

        // 2. UPDATE
        using (var context = new BlogContext(options))
        {
            var blog = context.Blogs.FirstOrDefault(b => b.Id == 1);
            Assert.NotNull(blog);
            blog.Title = "Updated Title";
            context.SaveChanges();
        }

        // 3. VERIFY UPDATE
        using (var context = new BlogContext(options))
        {
            var blog = context.Blogs.FirstOrDefault(b => b.Id == 1);
            Assert.NotNull(blog);
            Assert.Equal("Updated Title", blog.Title);
        }
    }

    [Fact]
    public void Can_add_range_of_blogs()
    {
        var options = CreateOptions<BlogContext>();

        using (var context = new BlogContext(options))
        {
            context.Blogs.AddRange(
                new Blog { Id = 1, Title = "Blog 1" },
                new Blog { Id = 2, Title = "Blog 2" },
                new Blog { Id = 3, Title = "Blog 3" }
            );
            context.SaveChanges();
        }

        using (var context = new BlogContext(options))
        {
            Assert.Equal(3, context.Blogs.Count());
            Assert.True(context.Blogs.Any(b => b.Title == "Blog 2"));
        }
    }
}

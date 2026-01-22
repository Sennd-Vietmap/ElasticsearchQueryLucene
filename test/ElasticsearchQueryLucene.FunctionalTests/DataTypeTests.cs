using Microsoft.EntityFrameworkCore;
using ElasticsearchQueryLucene.FunctionalTests.TestUtilities;

namespace ElasticsearchQueryLucene.FunctionalTests;

public class DataTypeTests : LuceneTestBase
{
    private class AllTypesEntity
    {
        public int Id { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
        public string StringValue { get; set; } = "";
        public DateTime DateTimeValue { get; set; }
        public Guid GuidValue { get; set; }
    }

    private class AllTypesContext : DbContext
    {
        public AllTypesContext(DbContextOptions options) : base(options) { }
        public DbSet<AllTypesEntity> Entities => Set<AllTypesEntity>();
    }

    [Fact]
    public void Can_store_and_retrieve_all_primitive_types()
    {
        var options = CreateOptions<AllTypesContext>();
        var now = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var guid = Guid.NewGuid();

        using (var context = new AllTypesContext(options))
        {
            context.Entities.Add(new AllTypesEntity
            {
                Id = 1,
                IntValue = 42,
                LongValue = 1234567890L,
                FloatValue = 3.14f,
                DoubleValue = 2.718281828,
                BoolValue = true,
                StringValue = "Hello Lucene",
                DateTimeValue = now,
                GuidValue = guid
            });
            context.SaveChanges();
        }

        using (var context = new AllTypesContext(options))
        {
            var entity = context.Entities.FirstOrDefault(e => e.Id == 1);
            Assert.NotNull(entity);
            Assert.Equal(42, entity.IntValue);
            Assert.Equal(1234567890L, entity.LongValue);
            Assert.Equal(3.14f, entity.FloatValue, 3);
            Assert.Equal(2.718281828, entity.DoubleValue, 6);
            Assert.True(entity.BoolValue);
            Assert.Equal("Hello Lucene", entity.StringValue);
            Assert.Equal(now, entity.DateTimeValue);
            Assert.Equal(guid, entity.GuidValue);
        }
    }

    [Fact]
    public void Can_filter_by_various_types()
    {
        var options = CreateOptions<AllTypesContext>();
        var guid = Guid.NewGuid();

        using (var context = new AllTypesContext(options))
        {
            context.Entities.Add(new AllTypesEntity { Id = 1, IntValue = 10, BoolValue = true, StringValue = "Target", GuidValue = guid });
            context.Entities.Add(new AllTypesEntity { Id = 2, IntValue = 20, BoolValue = false, StringValue = "Other", GuidValue = Guid.NewGuid() });
            context.SaveChanges();
        }

        using (var context = new AllTypesContext(options))
        {
            Assert.Equal(1, context.Entities.Count(e => e.IntValue == 10));
            Assert.Equal(1, context.Entities.Count(e => e.IntValue > 15));
            Assert.Equal(1, context.Entities.Count(e => e.BoolValue == true));
            Assert.Equal(1, context.Entities.Count(e => e.GuidValue == guid));
            Assert.Equal(1, context.Entities.Count(e => e.StringValue == "Target"));
        }
    }
}

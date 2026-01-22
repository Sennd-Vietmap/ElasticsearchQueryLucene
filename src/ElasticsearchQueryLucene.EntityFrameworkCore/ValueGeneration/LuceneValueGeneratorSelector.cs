using System;
using System.Threading;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.ValueGeneration;

public class LuceneValueGeneratorSelector : ValueGeneratorSelector
{
    public LuceneValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies)
        : base(dependencies)
    {
    }

    protected override ValueGenerator? FindForType(IProperty property, ITypeBase typeBase, Type clrType)
    {
        if (property.ClrType == typeof(int))
        {
            return new LuceneIntValueGenerator();
        }
        
        if (property.ClrType == typeof(long))
        {
             return new LuceneLongValueGenerator();
        }

        return base.FindForType(property, typeBase, clrType);
    }

    private class LuceneIntValueGenerator : ValueGenerator<int>
    {
        private int _current;
        public override bool GeneratesTemporaryValues => false;
        public override int Next(EntityEntry entry) => Interlocked.Increment(ref _current);
    }

    private class LuceneLongValueGenerator : ValueGenerator<long>
    {
        private long _current;
        public override bool GeneratesTemporaryValues => false;
        public override long Next(EntityEntry entry) => Interlocked.Increment(ref _current);
    }
}

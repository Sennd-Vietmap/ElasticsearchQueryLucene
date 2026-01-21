using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneEntityMaterializerSource : IStructuralTypeMaterializerSource
{
    public virtual Expression CreateMaterializeExpression(
        StructuralTypeMaterializerSourceParameters parameters,
        Expression valueBuffer)
    {
        // For Phase 4, we'll return a simple expression.
        // In a real provider, this would use Document.Get and build the object.
        return Expression.Default(parameters.ClrType);
    }

    public virtual Func<MaterializationContext, object> GetMaterializer(IEntityType entityType)
    {
        return context => Activator.CreateInstance(entityType.ClrType)!;
    }

    public virtual Func<MaterializationContext, object> GetMaterializer(IComplexType complexType)
    {
        return context => Activator.CreateInstance(complexType.ClrType)!;
    }

    public virtual Func<MaterializationContext, object> GetEmptyMaterializer(IEntityType entityType)
    {
        return context => Activator.CreateInstance(entityType.ClrType)!;
    }

    public virtual Func<MaterializationContext, object> GetEmptyMaterializer(IComplexType complexType)
    {
        return context => Activator.CreateInstance(complexType.ClrType)!;
    }
}

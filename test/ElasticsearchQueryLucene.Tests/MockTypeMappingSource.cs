using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace ElasticsearchQueryLucene.Tests;

public class MockTypeMappingSource : ITypeMappingSource
{
    public CoreTypeMapping? FindMapping(Type type) => null;
    public CoreTypeMapping? FindMapping(IProperty property) => null;
    public CoreTypeMapping? FindMapping(MemberInfo member) => null;
    public CoreTypeMapping? FindMapping(Type type, IModel model, CoreTypeMapping? referenceMapping = null) => null;
    public CoreTypeMapping? FindMapping(MemberInfo member, IModel model, bool useRawDefault = false) => null;
    public CoreTypeMapping? FindMapping(IElementType elementType) => null;
}

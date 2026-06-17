using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SprintPlanner.Infrastructure.Persistence;

/// <summary>
/// Helpers to persist complex CLR members (value objects, collections) as PostgreSQL
/// <c>jsonb</c> columns with proper change-tracking comparers.
/// </summary>
internal static class JsonConversions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static PropertyBuilder<T> AsJsonb<T>(this PropertyBuilder<T> builder) where T : class?
    {
        var converter = new ValueConverter<T, string>(
            v => JsonSerializer.Serialize(v, Options),
            v => JsonSerializer.Deserialize<T>(v, Options)!);

        var comparer = new ValueComparer<T>(
            (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
            v => v == null ? 0 : JsonSerializer.Serialize(v, Options).GetHashCode(),
            v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options)!);

        builder.HasConversion(converter).Metadata.SetValueComparer(comparer);
        builder.HasColumnType("jsonb");
        return builder;
    }

    public static PropertyBuilder<List<TItem>> AsJsonbList<TItem>(this PropertyBuilder<List<TItem>> builder)
    {
        var converter = new ValueConverter<List<TItem>, string>(
            v => JsonSerializer.Serialize(v, Options),
            v => JsonSerializer.Deserialize<List<TItem>>(v, Options) ?? new List<TItem>());

        var comparer = new ValueComparer<List<TItem>>(
            (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
            v => v == null ? 0 : JsonSerializer.Serialize(v, Options).GetHashCode(),
            v => JsonSerializer.Deserialize<List<TItem>>(JsonSerializer.Serialize(v, Options), Options)!);

        builder.HasConversion(converter).Metadata.SetValueComparer(comparer);
        builder.HasColumnType("jsonb");
        return builder;
    }
}

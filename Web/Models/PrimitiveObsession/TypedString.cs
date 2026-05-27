using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace Web.Models;

public abstract record TypedString : IComparable<TypedString>
{
  private readonly string value;

  protected TypedString(string value)
  {
    this.value = value;
  }

public static implicit operator string(TypedString typedString) => typedString.value;

  public sealed override string ToString() => value;

  [ModuleInitializer]
  internal static void RegisterDapperTypeHandlers()
  {
    var assembly = typeof(TypedString).Assembly;
    var concreteTypes = assembly
      .GetTypes()
      .Where(t => typeof(TypedString).IsAssignableFrom(t) && !t.IsAbstract && t.IsSealed)
      .ToList();

    // AddTypeHandler has multiple overloads – find the generic one with a single parameter
    var addHandlerMethod = typeof(SqlMapper)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .First(m =>
        m.Name == nameof(SqlMapper.AddTypeHandler)
        && m.IsGenericMethodDefinition
        && m.GetParameters().Length == 1
      );

    foreach (var type in concreteTypes)
    {
      var genericMethod = addHandlerMethod.MakeGenericMethod(type);
      var handlerType = typeof(TypedStringTypeHandler<>).MakeGenericType(type);
      var handler = Activator.CreateInstance(handlerType);
      genericMethod.Invoke(null, [handler!]);
    }
  }

  public int CompareTo(TypedString? other) => string.CompareOrdinal(value, other?.value ?? "");

  public override int GetHashCode() => HashCode.Combine(GetType(), value);
}

public sealed class TypedStringJsonConverter<TTypedString> : JsonConverter<TTypedString>
  where TTypedString : TypedString
{
  private static readonly string JsonValueName = typeof(TTypedString).Name;

  private static readonly System.Reflection.ConstructorInfo StringConstructor =
    typeof(TTypedString).GetConstructor([typeof(string)])
    ?? throw new InvalidOperationException(
      $"Typed string converter could not find a public string constructor for '{typeof(TTypedString).FullName}'."
    );

  public override TTypedString Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    if (reader.TokenType != JsonTokenType.String)
    {
      throw new JsonException(
        $"Expected {JsonValueName} JSON string but received token '{reader.TokenType}'."
      );
    }

    var rawValue =
      reader.GetString()
      ?? throw new JsonException(
        $"{JsonValueName} JSON value was null where a string was required."
      );
    return StringConstructor.Invoke([rawValue]) as TTypedString
      ?? throw new InvalidOperationException(
        $"Typed string converter created an invalid value for '{typeof(TTypedString).FullName}' from '{rawValue}'."
      );
  }

  public override void Write(
    Utf8JsonWriter writer,
    TTypedString value,
    JsonSerializerOptions options
  )
  {
    writer.WriteStringValue(value);
  }
}

/// <summary>
/// Dapper type handler that converts typed-string values to SQL parameters via ToString().
/// Each concrete typed-string registers an instance in its own static constructor.
/// </summary>
public class TypedStringTypeHandler<T> : SqlMapper.TypeHandler<T>
  where T : TypedString
{
  public override T? Parse(object value)
  {
    if (value is not string stringValue)
      throw new InvalidOperationException(
        $"TypedStringTypeHandler<{typeof(T).Name}>: expected SQL type to be a string."
      );

    var constructor =
      typeof(T).GetConstructor([typeof(string)])
      ?? throw new InvalidOperationException(
        $"TypedStringTypeHandler<{typeof(T).Name}>: public string constructor not found on '{typeof(T).FullName}'."
      );
    return constructor.Invoke([stringValue]) as T;
  }

  public override void SetValue(IDbDataParameter parameter, T? value)
  {
    if (value is null)
      throw new InvalidOperationException(
        $"TypedStringTypeHandler<{typeof(T).Name}>: cannot set null value for non-nullable type '{typeof(T).FullName}' in database parameter '{parameter.ParameterName}'."
      );
    parameter.Value = value;
  }
}

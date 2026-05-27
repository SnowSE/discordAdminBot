using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace Web.Models;

public abstract record TypedInt : IComparable<TypedInt>
{
  private readonly int _value;

  protected TypedInt(int value)
  {
    _value = value;
  }

  public int Value => _value;

  public static implicit operator int?(TypedInt? typedInt) => typedInt?._value;

  public sealed override string ToString() => _value.ToString();

  public int CompareTo(TypedInt? other)
  {
    if (other is null)
      return 1;
    return _value.CompareTo(other._value);
  }

  [ModuleInitializer]
  internal static void RegisterDapperTypeHandlers()
  {
    var assembly = typeof(TypedInt).Assembly;
    var concreteTypes = assembly
      .GetTypes()
      .Where(t => typeof(TypedInt).IsAssignableFrom(t) && !t.IsAbstract && t.IsSealed)
      .ToList();

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
      var handlerType = typeof(TypedIntTypeHandler<>).MakeGenericType(type);
      var handler = Activator.CreateInstance(handlerType);
      genericMethod.Invoke(null, [handler!]);
    }
  }
}

public sealed class TypedIntJsonConverter<TTypedInt> : JsonConverter<TTypedInt>
  where TTypedInt : TypedInt
{
  private static readonly string JsonValueName = typeof(TTypedInt).Name;

  private static readonly System.Reflection.ConstructorInfo IntConstructor =
    typeof(TTypedInt).GetConstructor([typeof(int)])
    ?? throw new InvalidOperationException(
      $"Typed int converter could not find a public int constructor for '{typeof(TTypedInt).FullName}'."
    );

  public override TTypedInt Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    if (reader.TokenType != JsonTokenType.Number)
    {
      throw new JsonException(
        $"Expected {JsonValueName} JSON number but received token '{reader.TokenType}'."
      );
    }

    var rawValue = reader.GetInt32();
    return IntConstructor.Invoke([rawValue]) as TTypedInt
      ?? throw new InvalidOperationException(
        $"Typed int converter created an invalid value for '{typeof(TTypedInt).FullName}' from '{rawValue}'."
      );
  }

  public override void Write(Utf8JsonWriter writer, TTypedInt value, JsonSerializerOptions options)
  {
    writer.WriteNumberValue(value.Value);
  }
}

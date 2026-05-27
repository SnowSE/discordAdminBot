using System.Text.Json;
using System.Text.Json.Serialization;

namespace Web.Models;

public abstract record TypedString
{
  private readonly string _value;

  protected TypedString(string value)
  {
    _value = value;
  }

  public static implicit operator string(TypedString typedString) => typedString._value;

  public override string ToString() => _value;
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
    writer.WriteStringValue((string)value);
  }
}

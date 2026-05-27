using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedIntJsonConverter<RoleColor>))]
public sealed record RoleColor(int value) : TypedInt(value)
{
  public static implicit operator RoleColor(int value2) => new(value2);

  public string ToHexString() => Value.ToString("x6");

  public bool HasColor() => Value != 0;
}

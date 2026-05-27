using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedIntJsonConverter<RolePosition>))]
public sealed record RolePosition(int value) : TypedInt(value)
{
  public static implicit operator RolePosition(int value) => new(value);
}

using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<RoleName>))]
public sealed record RoleName(string value) : TypedString(value)
{
  public static implicit operator RoleName(string value) => new(value);
}

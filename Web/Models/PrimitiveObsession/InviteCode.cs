using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<InviteCode>))]
public sealed record InviteCode(string Value) : TypedString(Value)
{
  public static implicit operator InviteCode(string value) => new(value);
}

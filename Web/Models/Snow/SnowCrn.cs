using System.Text.Json.Serialization;

namespace Web.Models.Snow;

[JsonConverter(typeof(TypedStringJsonConverter<SnowCrn>))]
public sealed record SnowCrn(string value) : TypedString(value)
{
  public static implicit operator SnowCrn(string value) => new(value);
}

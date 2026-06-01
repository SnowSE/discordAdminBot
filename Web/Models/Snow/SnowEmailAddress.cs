using System.Text.Json.Serialization;

namespace Web.Models.Snow;

[JsonConverter(typeof(TypedStringJsonConverter<SnowEmailAddress>))]
public sealed record SnowEmailAddress(string value) : TypedString(value);

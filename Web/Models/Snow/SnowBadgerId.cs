using System.Text.Json.Serialization;

namespace Web.Models.Snow;

[JsonConverter(typeof(TypedStringJsonConverter<SnowBadgerId>))]
public sealed record SnowBadgerId(string value) : TypedString(value);

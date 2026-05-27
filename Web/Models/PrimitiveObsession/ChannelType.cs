using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedIntJsonConverter<ChannelType>))]
public sealed record ChannelType(int value) : TypedInt(value)
{
  public static implicit operator ChannelType(int v) => new(v);

  public string TypeName =>
    ((int?)(this!))!.Value switch
    {
      0 => "Text",
      2 => "Voice",
      4 => "Category",
      5 => "Announcement",
      13 => "Stage",
      15 => "Forum",
      16 => "Media",
      _ => $"Type {((int?)(this!))!.Value}",
    };
}

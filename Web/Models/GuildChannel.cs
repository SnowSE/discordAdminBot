namespace Web.Models;

public record GuildChannel(string Id, string? Name, int Type, int? Position, string? ParentId)
{
  public string TypeName =>
    Type switch
    {
      0 => "Text",
      2 => "Voice",
      4 => "Category",
      5 => "Announcement",
      13 => "Stage",
      15 => "Forum",
      16 => "Media",
      _ => $"Type {Type}",
    };
}

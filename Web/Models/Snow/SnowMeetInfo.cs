namespace Web.Models.Snow;

public record SnowMeetInfo(
  List<string> Days,
  string? StartTime,
  string? EndTime,
  string? Building,
  string? BuildingCode,
  string? Room
);

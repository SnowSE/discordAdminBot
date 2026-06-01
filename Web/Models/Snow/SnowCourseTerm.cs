namespace Web.Models.Snow;

public record SnowCourseTerm(
  string Name,
  string StartAt,
  string EndAt,
  SnowTermCode Code,
  bool? IsRegistered
);

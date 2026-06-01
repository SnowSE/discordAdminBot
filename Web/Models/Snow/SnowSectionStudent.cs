using System.Text.Json.Serialization;

namespace Web.Models.Snow;

public record SnowSectionStudent(
  [property: JsonPropertyName("badgerid")] SnowBadgerId? BadgerId,
  [property: JsonPropertyName("first_name")] string? FirstName,
  [property: JsonPropertyName("last_name")] string? LastName,
  [property: JsonPropertyName("email")] SnowEmailAddress? Email,
  [property: JsonPropertyName("photo")] string? PhotoUrl,
  [property: JsonPropertyName("grade")] string? Grade,
  [property: JsonPropertyName("waitlist_priority")] int? WaitlistPriority,
  [property: JsonPropertyName("major")] string? Major,
  [property: JsonPropertyName("registration_date")] DateTime? RegistrationDate
);

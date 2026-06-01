namespace Web.Models.Snow;

public record SnowCourse(
  string Name,
  SnowCourseTerm Term,
  SnowSubjectCode SubjectCode,
  SnowCourseNumber CourseNumber,
  SnowSectionNumber SectionNumber,
  SnowCrn Crn,
  decimal CreditHours,
  string StartDate,
  string EndDate,
  string Campus,
  string PartOfTerm,
  string? GradeMode,
  List<SnowMeetInfo> MeetInfo,
  List<SnowInstructor> Instructors,
  SnowEnrollment Enrollment,
  string? Requisite
);

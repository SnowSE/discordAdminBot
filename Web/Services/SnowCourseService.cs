using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web.Models;
using Web.Models.Snow;

namespace Web.Services;

public class SnowCourseService(IHttpClientFactory httpClientFactory, SnowCourseDb db)
{
  public static event Func<Task>? DataChanged;

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
  };

  private static readonly string[] AllDepartmentCodes =
  [
    "AD",
    "BSCI",
    "BIOL",
    "BUS",
    "CHEM",
    "COMM",
    "ENCS",
    "CM",
    "CED",
    "DANC",
    "EDFS",
    "ENPH",
    "EXSC",
    "GEOL",
    "AHNA",
    "HONR",
    "INDM",
    "ITEC",
    "LALI",
    "MATH",
    "MUSC",
    "NR",
    "NURS",
    "PHSX",
    "STEC",
    "SS",
    "THEA",
    "TRAN",
    "ART",
  ];

  public async Task RefreshCoursesAsync(
    SnowTermCode termCode,
    string jwtToken,
    CancellationToken ct = default
  )
  {
    var client = httpClientFactory.CreateClient("snow");

    var requestBody = new
    {
      division_codes = Array.Empty<string>(),
      department_codes = AllDepartmentCodes,
      subject_codes = Array.Empty<string>(),
      instructor_codes = Array.Empty<string>(),
    };

    using var request = new HttpRequestMessage(HttpMethod.Post, $"faculty/sections/{termCode}");
    request.Headers.Add("Cookie", $"jwt={jwtToken}");
    request.Content = new StringContent(
      JsonSerializer.Serialize(requestBody, JsonOptions),
      Encoding.UTF8,
      "application/json"
    );

    var response = await client.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();

    var responseJson = await response.Content.ReadAsStringAsync(ct);
    var courses =
      JsonSerializer.Deserialize<List<SnowCourse>>(responseJson, JsonOptions)
      ?? throw new InvalidOperationException(
        $"my.snow.edu returned null course list for term '{termCode}'"
      );

    var termName = BuildTermDisplayName(termCode);
    await db.SaveCoursesAsync(termCode, termName, courses);
    DataChanged?.Invoke();
  }

  public async Task RefreshSectionStudentsAsync(
    SnowTermCode termCode,
    SnowCrn crn,
    string jwtToken,
    CancellationToken ct = default
  )
  {
    var client = httpClientFactory.CreateClient("snow");

    using var request = new HttpRequestMessage(
      HttpMethod.Get,
      $"faculty/section/students?term_code={termCode}&crn={crn}"
    );
    request.Headers.Add("Cookie", $"jwt={jwtToken}");

    var response = await client.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();

    var students =
      (await response.Content.ReadFromJsonAsync<List<SnowSectionStudent>>(JsonOptions, ct))
      ?? throw new InvalidOperationException(
        $"my.snow.edu returned null student list for CRN '{crn}' term '{termCode}'"
      );

    await db.SaveSectionStudentsAsync(crn, termCode, students);
    DataChanged?.Invoke();
  }

  public Task<List<SnowSectionStudent>> GetCachedSectionStudentsAsync(
    SnowCrn crn,
    SnowTermCode termCode
  ) => db.GetSectionStudentsAsync(crn, termCode);

  public Task<DateTime?> GetLastSyncTimeForSectionAsync(SnowCrn crn, SnowTermCode termCode) =>
    db.GetLastSyncTimeForSectionAsync(crn, termCode);

  public Task<List<SnowTerm>> GetTermsAsync() => db.GetTermsAsync();

  public Task<List<SnowCourse>> GetCoursesForTermAsync(SnowTermCode termCode) =>
    db.GetCoursesForTermAsync(termCode);

  public async Task<
    Dictionary<(SnowCrn Crn, SnowTermCode TermCode), string?>
  > GetCourseNamesBatchAsync(List<CourseChannelAssignment> assignments)
  {
    var pairs = assignments.Select(a => (a.Crn, a.TermCode)).ToList();
    return await db.GetCourseNamesBatchAsync(pairs);
  }

  public async Task<
    Dictionary<(SnowCrn Crn, SnowTermCode TermCode), DateTime?>
  > GetSyncTimesBatchAsync(List<CourseChannelAssignment> assignments)
  {
    var pairs = assignments.Select(a => (a.Crn, a.TermCode)).ToList();
    return await db.GetSyncTimesBatchAsync(pairs);
  }

  public async Task<
    Dictionary<(SnowCrn Crn, SnowTermCode TermCode), int>
  > GetUnmappedStudentCountsBatchAsync(
    List<CourseChannelAssignment> assignments,
    HashSet<string> mappedBadgerIds
  )
  {
    var pairs = assignments.Select(a => (a.Crn, a.TermCode)).ToList();
    return await db.GetUnmappedStudentCountsBatchAsync(pairs, mappedBadgerIds);
  }

  public static string BuildTermDisplayName(SnowTermCode termCode)
  {
    if (termCode.Value.Length < 6)
      throw new ArgumentException(
        $"Term code '{termCode}' is too short to parse into year/semester."
      );

    var year = termCode.Value[..4];
    var semesterCode = termCode.Value[4..];
    var semesterName = semesterCode switch
    {
      "10" => "Spring",
      "30" => "Summer",
      "40" => "Fall",
      _ => throw new ArgumentException(
        $"Unrecognised semester code '{semesterCode}' in term '{termCode}'"
      ),
    };
    return $"{semesterName} {year}";
  }

  public static (
    List<(SnowTermCode TermCode, string DisplayName)> Terms,
    int CurrentTermIndex
  ) GenerateTermOptionsWithCurrent()
  {
    var now = DateTime.UtcNow;
    (int year, int semesterCode) = GetCurrentTerm(now);

    // Start 3 semesters before the current term so the list includes previous-year terms.
    int startYear = year;
    int startSemester = semesterCode;
    for (int i = 0; i < 3; i++)
    {
      (startYear, startSemester) = PreviousTerm(startYear, startSemester);
    }

    var terms = new List<(SnowTermCode TermCode, string DisplayName)>();
    int currentTermIndex = -1;

    // Walk forward chronologically: 3 past + current + 6 future = 10 terms total.
    for (int i = 0; i < 10; i++)
    {
      if (startYear == year && startSemester == semesterCode)
      {
        currentTermIndex = i;
      }

      SnowTermCode code = $"{startYear}{startSemester:D2}";
      terms.Add((code, BuildTermDisplayName(code)));

      (startYear, startSemester) = NextTerm(startYear, startSemester);
    }

    if (currentTermIndex < 0)
    {
      throw new InvalidOperationException(
        "Current semester was not found while generating term options list"
      );
    }

    return (terms, currentTermIndex);
  }

  private static (int Year, int Semester) GetCurrentTerm(DateTime date)
  {
    int year = date.Year;
    int semesterCode = date.Month switch
    {
      >= 1 and <= 5 => 10,
      >= 6 and <= 8 => 30,
      _ => 40,
    };
    return (year, semesterCode);
  }

  private static (int Year, int Semester) NextTerm(int year, int semesterCode)
  {
    return semesterCode switch
    {
      10 => (year, 30),
      30 => (year, 40),
      _ => (year + 1, 10),
    };
  }

  private static (int Year, int Semester) PreviousTerm(int year, int semesterCode)
  {
    return semesterCode switch
    {
      30 => (year, 10),
      40 => (year, 30),
      10 => (year - 1, 40),
      _ => throw new InvalidOperationException(
        $"Unexpected semester code '{semesterCode}' when computing previous term"
      ),
    };
  }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Web.Models.Snow;

namespace Web.Services;

public class SnowCourseDb(CacheDb cache)
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
  };

  public async Task<List<SnowTerm>> GetTermsAsync()
  {
    await using var scope = cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<(
      string termCode,
      string name,
      string updatedAt
    )>(
      "SELECT term_code, name, updated_at FROM snow_terms ORDER BY term_code DESC",
      transaction: scope.Session.Transaction
    );
    return rows.Select(r => new SnowTerm(
        r.termCode,
        r.name,
        DateTime.Parse(r.updatedAt, null, System.Globalization.DateTimeStyles.RoundtripKind)
      ))
      .ToList();
  }

  public async Task<List<SnowCourse>> GetCoursesForTermAsync(SnowTermCode termCode)
  {
    await using var scope = cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<string>(
      "SELECT data FROM snow_courses WHERE term_code = @termCode",
      new { termCode },
      transaction: scope.Session.Transaction
    );
    return rows.Select(json =>
        JsonSerializer.Deserialize<SnowCourse>(json, JsonOptions)
        ?? throw new InvalidOperationException($"Deserialized null course for term '{termCode}'")
      )
      .ToList();
  }

  public async Task SaveCoursesAsync(
    SnowTermCode termCode,
    string termName,
    List<SnowCourse> courses
  )
  {
    await using var scope = await cache.BeginTransactionAsync();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var now = DateTime.UtcNow.ToString("O");
    await conn.ExecuteAsync(
      """
      INSERT INTO snow_terms (term_code, name, updated_at) VALUES (@termCode, @name, @now)
      ON CONFLICT(term_code) DO UPDATE SET name = excluded.name, updated_at = excluded.updated_at
      """,
      new
      {
        termCode,
        name = termName,
        now,
      },
      transaction: tx
    );

    await conn.ExecuteAsync(
      "DELETE FROM snow_courses WHERE term_code = @termCode",
      new { termCode },
      transaction: tx
    );

    var courseRows = courses
      .Select(course => new { termCode, data = JsonSerializer.Serialize(course, JsonOptions) })
      .ToList();

    await conn.ExecuteAsync(
      "INSERT INTO snow_courses (term_code, data) VALUES (@termCode, @data)",
      courseRows,
      transaction: tx
    );

    await scope.CommitAsync();
  }

  public async Task SaveSectionStudentsAsync(
    SnowCrn crn,
    SnowTermCode termCode,
    List<SnowSectionStudent> students
  )
  {
    await using var scope = await cache.BeginTransactionAsync();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var now = DateTime.UtcNow.ToString("O");
    await conn.ExecuteAsync(
      "DELETE FROM snow_section_students WHERE crn = @crn AND term_code = @termCode",
      new { crn, termCode },
      transaction: tx
    );

    var studentRows = students
      .Select(s => new
      {
        crn,
        termCode,
        data = JsonSerializer.Serialize(s, JsonOptions),
        lastSyncedAt = now,
      })
      .ToList();

    await conn.ExecuteAsync(
      "INSERT INTO snow_section_students (crn, term_code, data, last_synced_at) VALUES (@crn, @termCode, @data, @lastSyncedAt)",
      studentRows,
      transaction: tx
    );

    await scope.CommitAsync();
  }

  public async Task<List<SnowSectionStudent>> GetSectionStudentsAsync(
    SnowCrn crn,
    SnowTermCode termCode
  )
  {
    await using var scope = cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<string>(
      "SELECT data FROM snow_section_students WHERE crn = @crn AND term_code = @termCode",
      new { crn, termCode },
      transaction: scope.Session.Transaction
    );
    return rows.Select(json =>
        JsonSerializer.Deserialize<SnowSectionStudent>(json, JsonOptions)
        ?? throw new InvalidOperationException(
          $"Deserialized null student for CRN '{crn}' term '{termCode}'"
        )
      )
      .ToList();
  }

  public async Task<DateTime?> GetLastSyncTimeForSectionAsync(SnowCrn crn, SnowTermCode termCode)
  {
    await using var scope = cache.OpenSession();
    var ts = await scope.Session.Connection.QuerySingleOrDefaultAsync<string?>(
      "SELECT MAX(last_synced_at) FROM snow_section_students WHERE crn = @crn AND term_code = @termCode",
      new { crn, termCode },
      transaction: scope.Session.Transaction
    );
    return ts is null
      ? null
      : DateTime.Parse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind);
  }

  public async Task<
    Dictionary<(SnowCrn Crn, SnowTermCode TermCode), string?>
  > GetCourseNamesBatchAsync(List<(SnowCrn Crn, SnowTermCode TermCode)> assignments)
  {
    if (assignments.Count == 0)
      return [];

    await using var scope = cache.OpenSession();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var result = new Dictionary<(SnowCrn, SnowTermCode), string?>();
    foreach (var (crn, termCode) in assignments)
      result[(crn, termCode)] = null;

    var uniqueTermCodes = assignments.Select(a => a.TermCode.Value).Distinct().ToList();
    var courseRows = await conn.QueryAsync<(string termCode, string data)>(
      "SELECT term_code, data FROM snow_courses WHERE term_code IN @termCodes",
      new { termCodes = uniqueTermCodes },
      transaction: tx
    );

    foreach (var row in courseRows)
    {
      try
      {
        var course = JsonSerializer.Deserialize<SnowCourse>(row.data, JsonOptions);
        if (course is null)
          continue;

        foreach (var (crn, termCode) in assignments.Where(a => a.TermCode.Value == row.termCode))
        {
          if (crn == course.Crn)
            result[(crn, termCode)] =
              $"{course.SubjectCode.Value} {course.CourseNumber.Value} - {course.Name}";
        }
      }
      catch (JsonException ex)
      {
        var dataPreview = row.data.Length > 200 ? row.data[..200] + "..." : row.data;
        Console.WriteLine(
          $"Failed to deserialize snow_courses row for term_code '{row.termCode}'. "
            + $"Data preview: '{dataPreview}'. "
            + $"Error: {ex.Message}"
        );
        throw new InvalidOperationException(
          $"Could not deserialize course data for term_code '{row.termCode}' in batch query.",
          ex
        );
      }
    }

    return result;
  }

  public async Task<
    Dictionary<(SnowCrn Crn, SnowTermCode TermCode), DateTime?>
  > GetSyncTimesBatchAsync(List<(SnowCrn Crn, SnowTermCode TermCode)> assignments)
  {
    if (assignments.Count == 0)
      return [];

    await using var scope = cache.OpenSession();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var syncTasks = assignments.Select(async assignment =>
    {
      var ts = await conn.QuerySingleOrDefaultAsync<string?>(
        "SELECT MAX(last_synced_at) FROM snow_section_students WHERE crn = @crn AND term_code = @termCode",
        new { crn = assignment.Crn.Value, termCode = assignment.TermCode.Value },
        transaction: tx
      );
      return (assignment.Crn, assignment.TermCode, Timestamp: ts);
    });

    var results = await Task.WhenAll(syncTasks);

    var result = new Dictionary<(SnowCrn, SnowTermCode), DateTime?>();
    foreach (var (crn, termCode, timestamp) in results)
    {
      result[(crn, termCode)] = timestamp is null
        ? null
        : DateTime.Parse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    return result;
  }

  public async Task<
    Dictionary<(SnowCrn Crn, SnowTermCode TermCode), int>
  > GetUnmappedStudentCountsBatchAsync(
    List<(SnowCrn Crn, SnowTermCode TermCode)> assignments,
    HashSet<string> mappedBadgerIds
  )
  {
    if (assignments.Count == 0)
      return [];

    await using var scope = cache.OpenSession();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var result = new Dictionary<(SnowCrn, SnowTermCode), int>();
    foreach (var (crn, termCode) in assignments)
      result[(crn, termCode)] = 0;

    foreach (var (crn, termCode) in assignments.Distinct())
    {
      var rows = await conn.QueryAsync<string>(
        "SELECT data FROM snow_section_students WHERE crn = @crn AND term_code = @termCode",
        new { crn = crn.Value, termCode = termCode.Value },
        transaction: tx
      );

      var count = 0;
      foreach (var json in rows)
      {
        try
        {
          var student = JsonSerializer.Deserialize<SnowSectionStudent>(json, JsonOptions);
          if (student?.BadgerId is not null && !mappedBadgerIds.Contains(student.BadgerId.Value))
            count++;
        }
        catch (JsonException ex)
        {
          Console.WriteLine(
            $"Failed to deserialize snow_section_students row for crn '{crn}' term_code '{termCode}'. "
              + $"Error: {ex.Message}"
          );
        }
      }

      result[(crn, termCode)] = count;
    }

    return result;
  }
}

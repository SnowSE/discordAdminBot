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
    using var conn = cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<(string termCode, string name, string updatedAt)>(
      "SELECT term_code, name, updated_at FROM snow_terms ORDER BY term_code DESC"
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
    using var conn = cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<string>(
      "SELECT data FROM snow_courses WHERE term_code = @termCode",
      new { termCode }
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
    using var conn = cache.OpenConnection();
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    try
    {
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

      await tx.CommitAsync();
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
  }

  public async Task SaveSectionStudentsAsync(
    SnowCrn crn,
    SnowTermCode termCode,
    List<SnowSectionStudent> students
  )
  {
    using var conn = cache.OpenConnection();
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    try
    {
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

      await tx.CommitAsync();
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
  }

  public async Task<List<SnowSectionStudent>> GetSectionStudentsAsync(
    SnowCrn crn,
    SnowTermCode termCode
  )
  {
    using var conn = cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<string>(
      "SELECT data FROM snow_section_students WHERE crn = @crn AND term_code = @termCode",
      new { crn, termCode }
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
    using var conn = cache.OpenConnection();
    await conn.OpenAsync();
    var ts = await conn.QuerySingleOrDefaultAsync<string?>(
      "SELECT MAX(last_synced_at) FROM snow_section_students WHERE crn = @crn AND term_code = @termCode",
      new { crn, termCode }
    );
    return ts is null
      ? null
      : DateTime.Parse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind);
  }
}

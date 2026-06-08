using System.Collections.Generic;
using Web.Models.Snow;

namespace Web.Services;

public static class FuzzyMatcher
{
  private static readonly Dictionary<string, string> Abbreviations = new(
    StringComparer.OrdinalIgnoreCase
  )
  {
    ["adv"] = "advanced",
    ["dist"] = "distributed",
    ["prac"] = "practicum",
    ["app"] = "application",
    ["apps"] = "applications",
    ["appl"] = "applied",
    ["dev"] = "development",
    ["eng"] = "engineering",
    ["intro"] = "introduction",
    ["prog"] = "programming",
    ["maint"] = "maintenance",
    ["db"] = "database",
    ["op"] = "operations",
    ["ops"] = "operations",
    ["net"] = "networking",
    ["soft"] = "software",
    ["se"] = "software engineering",
    ["cs"] = "computer science",
    ["algor"] = "algorithm",
    ["algos"] = "algorithms",
  };

  public static string StripTermSuffix(string channelName)
  {
    var parts = channelName.Split('-');
    for (int partIndex = 0; partIndex < parts.Length; partIndex++)
    {
      if (
        int.TryParse(parts[partIndex], out int year)
        && year >= 2000
        && year <= 2100
        && partIndex + 1 < parts.Length
      )
      {
        var nextPart = parts[partIndex + 1];
        if (nextPart is "spring" or "summer" or "fall")
          return string.Join(" ", parts[..partIndex]);
      }
    }

    return channelName.Replace('-', ' ');
  }

  public static double ScoreCourse(SnowCourse course, string query)
  {
    if (string.IsNullOrWhiteSpace(query))
      return 0;

    if (IsEmailSearch(query))
      return ScoreInstructorEmailPrefixes(course, query);

    var normalizedQuery = Normalize(query);
    var tokens = Tokenize(normalizedQuery);
    var expandedWords = ExpandAbbreviations(tokens).ToHashSet();

    var courseText = BuildCourseSearchText(course);
    var normalizedCourse = Normalize(courseText);
    var courseWords = Tokenize(normalizedCourse);

    double score = 0;

    if (normalizedCourse.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
      score += 100;

    foreach (var word in expandedWords)
    {
      bool exactWordMatch = false;

      foreach (var courseWord in courseWords)
      {
        if (word.Equals(courseWord, StringComparison.OrdinalIgnoreCase))
        {
          score += 15;
          exactWordMatch = true;
          break;
        }

        int minLength = Math.Min(word.Length, courseWord.Length);
        if (minLength >= 3)
        {
          bool queryPrefixOfCourse = courseWord.StartsWith(
            word,
            StringComparison.OrdinalIgnoreCase
          );
          bool coursePrefixOfQuery = word.StartsWith(
            courseWord,
            StringComparison.OrdinalIgnoreCase
          );

          if (queryPrefixOfCourse || coursePrefixOfQuery)
          {
            score += 8;
            break;
          }
        }
      }

      if (!exactWordMatch && word.Length > 1)
      {
        double bestSimilarity = 0;
        foreach (var courseWord in courseWords)
        {
          var sim = CalculateSimilarity(word, courseWord);
          if (sim > bestSimilarity)
            bestSimilarity = sim;
        }

        if (bestSimilarity >= 0.6)
          score += bestSimilarity * 10;
      }
    }

    return score;
  }

  private static string BuildCourseSearchText(SnowCourse course)
  {
    var instructorText = course.Instructors.Select(instructor => instructor.Name);

    return string.Join(
      " ",
      [course.Name, course.SubjectCode.Value, course.CourseNumber.Value, .. instructorText]
    );
  }

  private static bool IsEmailSearch(string query)
  {
    var trimmedQuery = query.Trim();
    return trimmedQuery.Contains('@') && trimmedQuery.IndexOf('@') > 0;
  }

  private static double ScoreInstructorEmailPrefixes(SnowCourse course, string query)
  {
    var queryLocalPart = BuildEmailLocalPart(query);
    if (string.IsNullOrWhiteSpace(queryLocalPart))
      return 0;

    var normalizedQueryLocalPart = NormalizeEmailPrefix(queryLocalPart);
    if (string.IsNullOrWhiteSpace(normalizedQueryLocalPart))
      return 0;

    return course.Instructors.Any(instructor =>
      InstructorEmailStartsWith(instructor.Email, queryLocalPart, normalizedQueryLocalPart)
    )
      ? 120
      : 0;
  }

  private static bool InstructorEmailStartsWith(
    string? instructorEmail,
    string queryLocalPart,
    string normalizedQueryLocalPart
  )
  {
    var instructorEmailLocalPart = BuildEmailLocalPart(instructorEmail);
    if (string.IsNullOrWhiteSpace(instructorEmailLocalPart))
      return false;

    return instructorEmailLocalPart.StartsWith(queryLocalPart, StringComparison.OrdinalIgnoreCase)
      || NormalizeEmailPrefix(instructorEmailLocalPart)
        .StartsWith(normalizedQueryLocalPart, StringComparison.OrdinalIgnoreCase);
  }

  private static string BuildEmailLocalPart(string? email)
  {
    if (string.IsNullOrWhiteSpace(email))
      return "";

    var trimmedEmail = email.Trim();
    var domainStartIndex = trimmedEmail.IndexOf('@');
    if (domainStartIndex < 0)
      return trimmedEmail;

    return trimmedEmail[..domainStartIndex];
  }

  private static string NormalizeEmailPrefix(string emailLocalPart)
  {
    var prefixCharacters = emailLocalPart.Select(character =>
      char.IsLetterOrDigit(character) ? character : ' '
    );

    return new string(prefixCharacters.ToArray()).Replace(" ", "");
  }

  private static string Normalize(string input)
  {
    var searchableCharacters = input.Select(character =>
      char.IsLetterOrDigit(character) ? character : ' '
    );

    return new string(searchableCharacters.ToArray()).Trim();
  }

  private static IEnumerable<string> Tokenize(string normalized)
  {
    return normalized.Split(
      ' ',
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    );
  }

  private static IEnumerable<string> ExpandAbbreviations(IEnumerable<string> tokens)
  {
    foreach (var token in tokens)
    {
      if (Abbreviations.TryGetValue(token, out string? expanded))
        yield return expanded;
      yield return token;
    }
  }

  private static double CalculateSimilarity(string firstText, string secondText)
  {
    int distance = LevenshteinDistance(
      firstText.ToLowerInvariant(),
      secondText.ToLowerInvariant()
    );
    int maxLength = Math.Max(firstText.Length, secondText.Length);
    return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
  }

  private static int LevenshteinDistance(string firstText, string secondText)
  {
    if (firstText.Length == 0)
      return secondText.Length;
    if (secondText.Length == 0)
      return firstText.Length;

    var previousRow = new int[secondText.Length + 1];
    for (int columnIndex = 0; columnIndex <= secondText.Length; columnIndex++)
      previousRow[columnIndex] = columnIndex;

    for (int firstTextIndex = 1; firstTextIndex <= firstText.Length; firstTextIndex++)
    {
      var currentRow = new int[secondText.Length + 1];
      currentRow[0] = firstTextIndex;

      for (int secondTextIndex = 1; secondTextIndex <= secondText.Length; secondTextIndex++)
      {
        int substitutionCost =
          firstText[firstTextIndex - 1] == secondText[secondTextIndex - 1] ? 0 : 1;

        currentRow[secondTextIndex] = Math.Min(
          Math.Min(currentRow[secondTextIndex - 1] + 1, previousRow[secondTextIndex] + 1),
          previousRow[secondTextIndex - 1] + substitutionCost
        );
      }

      previousRow = currentRow;
    }

    return previousRow[secondText.Length];
  }
}

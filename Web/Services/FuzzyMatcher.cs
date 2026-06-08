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
    for (int i = 0; i < parts.Length; i++)
    {
      if (
        int.TryParse(parts[i], out int year)
        && year >= 2000
        && year <= 2100
        && i + 1 < parts.Length
      )
      {
        var next = parts[i + 1];
        if (next is "spring" or "summer" or "fall")
          return string.Join(" ", parts[..i]);
      }
    }

    return channelName.Replace('-', ' ');
  }

  public static double ScoreCourse(SnowCourse course, string query)
  {
    if (string.IsNullOrWhiteSpace(query))
      return 0;

    var normalizedQuery = Normalize(query);
    var tokens = Tokenize(normalizedQuery);
    var expandedWords = ExpandAbbreviations(tokens).ToHashSet();

    var courseText = $"{course.Name} {course.SubjectCode.Value} {course.CourseNumber.Value}";
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

  private static string Normalize(string input) => input.Replace('-', ' ').Replace('_', ' ').Trim();

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

  private static double CalculateSimilarity(string a, string b)
  {
    int distance = LevenshteinDistance(a.ToLowerInvariant(), b.ToLowerInvariant());
    int maxLength = Math.Max(a.Length, b.Length);
    return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
  }

  private static int LevenshteinDistance(string a, string b)
  {
    if (a.Length == 0)
      return b.Length;
    if (b.Length == 0)
      return a.Length;

    var previousRow = new int[b.Length + 1];
    for (int j = 0; j <= b.Length; j++)
      previousRow[j] = j;

    for (int i = 1; i <= a.Length; i++)
    {
      var currentRow = new int[b.Length + 1];
      currentRow[0] = i;

      for (int j = 1; j <= b.Length; j++)
      {
        int cost = a[i - 1] == b[j - 1] ? 0 : 1;

        currentRow[j] = Math.Min(
          Math.Min(currentRow[j - 1] + 1, previousRow[j] + 1),
          previousRow[j - 1] + cost
        );
      }

      previousRow = currentRow;
    }

    return previousRow[b.Length];
  }
}

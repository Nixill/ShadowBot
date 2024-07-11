/*
THIS FILE IS NOT ASSOCIATED WITH, ENDORSED BY, OR APPROVED BY ANY WEBSITE,
COMPANY, OR PERSON NAMED HEREIN.
*/

using System.Text.RegularExpressions;
using Nixill.Utils;

namespace Nixill;

public static class UrlShortener
{
  public static IEnumerable<Uri> Shorten(string input) => Shorten(new Uri(input));

  public static IEnumerable<Uri> Shorten(Uri input)
  {
    if (Regex.IsMatch(input.Host, @"^(www\.)?amazon\.(com|co\.uk|de|ca)"))
    {
      // Console.WriteLine("Amazon link...");
      return ShortenAmazon(input);
    }

    else if (Regex.IsMatch(input.Host, @"^(www\.)?humble(bundle)?\.com"))
    {
      return ShortenHumble(input);
    }

    return Enumerable.Empty<Uri>();
  }

  public static IEnumerable<Uri> ShortenAmazon(Uri input)
  {
    List<string> segments = input.Segments.ToList();

    // segments.Do(x => Console.WriteLine(x));

    if (segments[2] == "dp/")
    {
      yield return new Uri($"{input.Scheme}://{input.Host}/{segments[2]}{segments[3]}");
    }
    else if (segments[1] == "dp/")
    {
      yield return new Uri($"{input.Scheme}://{input.Host}/{segments[1]}{segments[2]}");
    }
  }

  public static IEnumerable<Uri> ShortenHumble(Uri input)
  {
    // For legal reasons: This is an affiliate link. If you buy something
    // from this link, I will earn a small cut.
    yield return new Uri($"https://humblebundle.com{input.AbsolutePath}?partner=nixill&charity=1599605");
    yield return new Uri($"https://humblebundle.com{input.AbsolutePath}");
  }
}
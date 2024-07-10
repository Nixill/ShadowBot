/*
THIS FILE IS NOT ASSOCIATED WITH, ENDORSED BY, OR APPROVED BY ANY WEBSITE,
COMPANY, OR PERSON NAMED HEREIN.
*/

using System.Text.RegularExpressions;
using Nixill.Utils;

namespace Nixill;

public static class UrlShortener
{
  public static Uri Shorten(string input) => Shorten(new Uri(input));

  public static Uri Shorten(Uri input)
  {
    if (Regex.IsMatch(input.Host, @"^(www\.)?amazon\.(com|co\.uk|de|ca)"))
    {
      // Console.WriteLine("Amazon link...");
      return ShortenAmazon(input);
    }

    return input;
  }

  public static Uri ShortenAmazon(Uri input)
  {
    List<string> segments = input.Segments.ToList();

    // segments.Do(x => Console.WriteLine(x));

    if (segments[2] == "dp/")
    {
      return new Uri($"{input.Scheme}://{input.Host}/{segments[2]}{segments[3]}");
    }
    else if (segments[1] == "dp/")
    {
      return new Uri($"{input.Scheme}://{input.Host}/{segments[1]}{segments[2]}");
    }

    return input;
  }
}
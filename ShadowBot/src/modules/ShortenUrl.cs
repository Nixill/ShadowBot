/*
THIS FILE IS NOT ASSOCIATED WITH, ENDORSED BY, OR APPROVED BY ANY WEBSITE,
COMPANY, OR PERSON NAMED HEREIN.
*/

using System.Text.RegularExpressions;
using Nixill.Utils;

namespace Nixill;

public static class UrlShortener
{
  public static IEnumerable<Uri> Shorten(string input, string options)
    => Shorten(new Uri(input), (options ?? "").Split(",").Select(o =>
    {
      var pair = o.Split("=", 2);
      if (pair.Length == 2)
        return (pair[0].ToLower(), pair[1]);
      else
        return (pair[0].ToLower(), "");
    }).ToDictionary());

  public static IEnumerable<Uri> Shorten(Uri input, IDictionary<string, string> options)
  {
    if (Regex.IsMatch(input.Host, @"^(www\.)?amazon\.(com|co\.uk|de|ca)"))
    {
      // Console.WriteLine("Amazon link...");
      return ShortenAmazon(input);
    }

    else if (Regex.IsMatch(input.Host, @"^(www\.)?humble(bundle)?\.com"))
    {
      return ShortenHumble(input, options.ContainsKey("noad"));
    }

    else if (Regex.IsMatch(input.Host, @"^(www\.)?(twitter|x)\.com"))
    {
      return FixUpTwitter(input);
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

  public static IEnumerable<Uri> ShortenHumble(Uri input, bool noAd)
  {
    // For legal reasons: The first one is an affiliate link. If you buy
    // something from that link, I will earn a small cut.
    var ad = new Uri($"https://humblebundle.com{input.AbsolutePath}?partner=nixill&charity=1599605");
    if (!noAd) yield return ad;
    yield return new Uri($"https://humblebundle.com{input.AbsolutePath}");
    if (noAd) yield return ad;
  }

  public static IEnumerable<Uri> FixUpTwitter(Uri input)
  {
    yield return new Uri($"https://fixupx.com{input.AbsolutePath}");
  }
}
using System.Text.RegularExpressions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Nixill.Utils;
using NodaTime;
using NodaTime.Text;
using NodaTime.TimeZones;

namespace Nixill.Discord.ShadowBot;

public class TimeZoneAutoCompleteProvider : IAutoCompleteProvider
{
  public static readonly LocalTimePattern TimePtn = LocalTimePattern.CreateWithInvariantCulture("HH:mm");
  public static readonly LocalTimePattern OffsetPtn = LocalTimePattern.CreateWithInvariantCulture("h:mm tt");
  public static readonly Regex PartialTimeRegex = new(@"^(\d\d?)(?::?$|:(\d$|\d\d))? ?(?:([ap])\.?m?\.?)$?");

  static readonly Period Filter = Period.FromSeconds(450);
  static readonly Period HalfDay = Period.FromHours(12);
  static readonly TzdbDateTimeZoneSource TZDB = TzdbDateTimeZoneSource.Default;

  public ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
  {
    string input = ctx.UserInput;
    return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(GetZonesFor(input).DistinctBy(x => x.Name).Take(25).ToDictionary().AsReadOnly());
  }

  public IEnumerable<(string Name, object Key)> GetZonesFor(string input)
  {
    Instant now = SystemClock.Instance.GetCurrentInstant();

    if (input == null || input == "")
    {
      var DefaultZone = Settings.TimeZone;
      yield return ($"Default zone: {DefaultZone.Id} ({TimeInZone(now, DefaultZone)})", DefaultZone.Id);
      yield return ($"Type your current time to find by time", "UTC");
      yield return ($"Or start typing a name to search", "UTC");
      yield break;
    }

    else if (PartialTimeRegex.TryMatch(input, out Match mtc))
    {
      int hour = int.Parse(mtc.Groups[1].Value);

      if (mtc.Groups[3].Success)
      {
        if (hour == 12)
        {
          if (mtc.Groups[3].Value == "a") hour = 0;
        }
        else if (mtc.Groups[3].Value == "p") hour += 12;
      }

      if (mtc.Groups[2].Success)
      {
        int minute = int.Parse(mtc.Groups[2].Value);
        foreach (var v in ZonesForTime(hour, minute, now))
          yield return ($"{v.Zone.Id} ({TimePtn.Format(v.Time)})", v.Zone.Id);

        if (!mtc.Groups[3].Success)
        {
          foreach (var v in ZonesForTime((hour + 12) % 24, minute, now))
            yield return ($"{v.Zone.Id} ({OffsetPtn.Format(v.Time)})", v.Zone.Id);
        }
      }

      else
      {
        foreach (var v in ZonesForTime(hour, now))
          yield return ($"{v.Zone.Id} ({TimePtn.Format(v.Time)})", v.Zone.Id);

        if (!mtc.Groups[3].Success)
        {
          foreach (var v in ZonesForTime((hour + 12) % 24, now))
            yield return ($"{v.Zone.Id} ({OffsetPtn.Format(v.Time)})", v.Zone.Id);
        }
      }
    }

    else
    {
      foreach (var v in ZonesForQuery(input, now))
        yield return ($"{v.Zone.Id} ({TimePtn.Format(v.Time)})", v.Zone.Id);
    }
  }

  IEnumerable<string> AllZoneIDs => TZDB.GetIds();
  IEnumerable<(string ZoneID, int Level /* (1 = key, 2 = group element) */)>
    AliasListIDs => TZDB.Aliases
      .SelectMany(g => g.Select(i => (i, 2)).Append((g.Key, 1)));
  IEnumerable<(string ZoneID, int Level /* (0 = not in aliases, 1-2 = above) */)>
    AllAliasListIDs => AliasListIDs.Concat(AllZoneIDs.Except(AliasListIDs.Select(a => a.ZoneID)).Select(z => (z, 0)));
  IEnumerable<string> NonAliasZoneIDs => AllAliasListIDs.Where(p => p.Level != 2).Select(p => p.ZoneID);

  string TimeInZone(Instant now, DateTimeZone zone)
    => TimePtn.Format(now.InZone(zone).LocalDateTime.TimeOfDay);

  IEnumerable<(DateTimeZone Zone, LocalTime Time)> NonAliasZoneTimes(Instant now)
    => NonAliasZoneIDs // IEnumerable<string>
      .Select(g => TZDB.ForId(g)) // IEnumerable<DateTimeZone>
      .Select(z => (z, now.InZone(z).LocalDateTime.TimeOfDay)); // IEnumerable<(DateTimeZone, LocalTime)>

  IEnumerable<(DateTimeZone Zone, LocalTime Time)> ZonesForTime(int hour, Instant now)
  {
    LocalTime minimum = new LocalTime((24 + hour - 1) % 24, 50);
    LocalTime maximum = new LocalTime((hour + 1) % 24, 10);

    if (minimum > maximum) return NonAliasZoneTimes(now)
      .Where(v => v.Time < maximum || v.Time > maximum);
    else return NonAliasZoneTimes(now)
      .Where(v => v.Time < maximum && v.Time > minimum);
  }

  IEnumerable<(DateTimeZone Zone, LocalTime Time)> ZonesForTime(int hour, int minute, Instant now)
  {
    LocalTime minimum = new LocalTime(hour, minute) - Filter;
    LocalTime maximum = new LocalTime(hour, minute) + Filter;

    if (minimum > maximum) return NonAliasZoneTimes(now)
      .Where(v => v.Time < maximum || v.Time > maximum);
    else return NonAliasZoneTimes(now)
      .Where(v => v.Time < maximum && v.Time > minimum);
  }

  IEnumerable<(DateTimeZone Zone, LocalTime Time, string Id, int SearchLevel, int StartIndex)> ZonesForQuery(string name, Instant now)
    => QueryZones(name, now)
      .Where(sr => sr.SearchLevel != 0)
      .OrderByDescending(sr => sr.SearchLevel)
      .ThenBy(sr => sr.StartIndex)
      .ThenBy(sr => sr.Id);

  IEnumerable<(DateTimeZone Zone, LocalTime Time, string Id, int SearchLevel, int StartIndex)> QueryZones(string name, Instant now)
  {
    var search = name.ToLower()
      .Where(c => c >= 'a' && c <= 'z' || c == '_' || c == '/').FormString();

    foreach (string id in AllZoneIDs)
    {
      var lowId = id.ToLower();
      var zone = TZDB.ForId(id);
      var time = now.InZone(zone).LocalDateTime.TimeOfDay;

      var index = lowId.IndexOf(search);
      if (index != -1) yield return (zone, time, id, 2, index);

      int chr = 0;
      foreach ((char c, int i) in lowId.Select((c, i) => (c, i)))
      {
        if (c == search[chr])
        {
          if (chr == 0)
          {
            index = i;
          }

          chr++;
          if (chr == search.Length)
          {
            yield return (zone, time, id, 1, index);
            break;
          }
        }
      }
    }
  }
}
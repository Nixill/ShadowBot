using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using Nixill.Utils;
using NodaTime;
using NodaTime.TimeZones;

namespace Nixill.Discord.ShadowBot;

public enum NixTimestampFormat
{
  ShortDate,
  LongDate,
  ShortDateTime,
  LongDateTime,
  ShortTime,
  LongTime,
  RelativeTime,
  UnixTimestamp
}

[InteractionAllowedContexts(
  DiscordInteractionContextType.Guild,
  DiscordInteractionContextType.BotDM,
  DiscordInteractionContextType.PrivateChannel
)]
[InteractionInstallType(
  DiscordApplicationIntegrationType.UserInstall
)]
[Command("time")]
[TopLevelCommand]
public static class TimeCommand
{
  static readonly Regex TimeRegex = new(@"^(\d?\d):(\d\d)(?::(\d\d))?(?: ?([ap])\.?m?\.?)?$");
  static readonly Regex DateRegex = new(@"^(?:(\d{4})[-/\.])?(\d?\d)[-/\.](\d\d)$");
  static readonly Regex DayOffsetRegex = new(@"^([-+]\d+|0)$");

  static readonly ReadOnlyDictionary<string, string> DaysOfWeek = new Dictionary<string, string>()
  {
    ["su"] = "sunday",
    ["mo"] = "monday",
    ["tu"] = "tuesday",
    ["we"] = "wednesday",
    ["th"] = "thursday",
    ["fr"] = "friday",
    ["sa"] = "saturday",
    ["to"] = "today",
    ["ye"] = "yesterday",
    ["ub"] = "ubermorgen",
    ["üb"] = "übermorgen"
  }.AsReadOnly();

  [Command("code")]
  [Description("Get the <t:...> code for a given time of day")]
  public static async Task TimeCodeCommand(SlashCommandContext ctx,
    [Description("The time to get")] string time,
    [Description("The date to get")] string date = null,
    [Description("What time zone to use")][SlashAutoCompleteProvider<TimeZoneAutoCompleteProvider>] string timezone = null,
    [Description("What format to use")] NixTimestampFormat? format = null,
    [Description("Is entered time daylight saving time? Only has any effect for ambiguous times.")] bool? daylightSaving = null,
    [Description("Hide from others? (May be forced by the server anyway.)")] bool ephemeral = true
  )
  {
    DateTimeZone zone = Settings.TimeZone;

    if (timezone != null)
    {
      DateTimeZone zoneForId = TzdbDateTimeZoneSource.Default.ForId(timezone);
      if (zoneForId != null) zone = zoneForId;
      else
      {
        await ctx.RespondAsync($"`{timezone}` is not a valid time zone.", true);
        return;
      }
    }

    LocalTime lTime;

    { // just to make mtc reusable
      if (TimeRegex.TryMatch(time.ToLower(), out Match mtc))
      {
        int hour = int.Parse(mtc.Groups[1].Value);
        int minute = int.Parse(mtc.Groups[2].Value);
        int second = 0;

        if (mtc.Groups[3].Success)
          second = int.Parse(mtc.Groups[3].Value);

        if (mtc.Groups[4].Success)
        {
          if (hour == 12)
          {
            if (mtc.Groups[4].Value == "a") hour = 0;
          }
          else if (mtc.Groups[4].Value == "p") hour += 12;
        }

        lTime = new(hour, minute, second);
      }
      else
      {
        await ctx.RespondAsync($"`{time}` isn't a valid time!", true);
        return;
      }
    }

    LocalDate lDate;

    date = date.ToLower();

    var now = SystemClock.Instance.GetCurrentInstant();
    var zonedNow = now.InZone(zone);
    var localNow = zonedNow.LocalDateTime - Period.FromHours(1);

    if (date != null)
    {
      if (DateRegex.TryMatch(date, out Match mtc))
      {
        int year = zonedNow.Year;
        int month = int.Parse(mtc.Groups[2].Value);
        int day = int.Parse(mtc.Groups[3].Value);

        if (mtc.Groups[1].Success) year = int.Parse(mtc.Groups[1].Value);

        lDate = new(year, month, day);
      }
      else if (date.Length >= 3
        && date.Length <= 8
        && ("tomorrow")[0..date.Length] == date)
      {
        lDate = zonedNow.Date + Period.FromDays(1);
      }
      else if (date.Length >= 2
        && DaysOfWeek.TryGetValue(date[0..2], out string pickedDay)
        && date.Length <= pickedDay.Length
        && pickedDay[0..date.Length] == date)
      {
        lDate = pickedDay switch
        {
          "today" or "yesterday" or "ubermorgen" or "übermorgen" => zonedNow.Date + Period.FromDays(pickedDay switch
          {
            "today" => 0,
            "yesterday" => -1,
            _ => 2
          }),
          _ => zonedNow.Date + Period.FromDays((pickedDay switch
          {
            "monday" => 1,
            "tuesday" => 2,
            "wednesday" => 3,
            "thursday" => 4,
            "friday" => 5,
            "saturday" => 6,
            _ => 7
          } - (int)zonedNow.DayOfWeek + 8) % 7 - 1)
        };
      }
      else if (DayOffsetRegex.TryMatch(date, out mtc))
      {
        lDate = zonedNow.Date + Period.FromDays(int.Parse(mtc.Value));
      }
      else
      {
        await ctx.RespondAsync($"{date} isn't a valid date!", true);
        return;
      }
    }
    else
    {
      var testTime = localNow.With((LocalTime t) => lTime);

      if (testTime < localNow) lDate = localNow.Date.PlusDays(1);
      else lDate = localNow.Date;
    }

    // Lastly, decide on a dst value
    bool isDst = now.InZone(zone).IsDaylightSavingTime();
    bool asDst = daylightSaving ?? isDst;

    // now put it all back together
    LocalDateTime ldt = lDate + lTime;

    var map = zone.MapLocal(ldt);

    if (map.Count == 0)
    {
      await ctx.RespondAsync($"The date/time {ldt} is not a valid time in the time zone {zone}!", true);
      return;
    }

    var zonedTime = asDst ? map.First() : map.Last();

    // And lastly, get the unix timestamp value
    var unix = zonedTime.ToInstant().ToUnixTimeSeconds();

    // char formatChar = (format ?? Settings.TimeFormat) switch
    // {
    //   NixTimestampFormat.ShortDate => 'd',
    //   NixTimestampFormat.LongDate => 'D',
    //   NixTimestampFormat.LongDateTime => 'F',
    //   NixTimestampFormat.ShortTime => 't',
    //   NixTimestampFormat.LongTime => 'T',
    //   NixTimestampFormat.RelativeTime => 'r',
    //   _ => 'f' // default because saying nothing defaults to this too
    // };

    DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
    {
      Title = "Your requested timecode",
      Description = $"You requested <t:{unix}:f> as a timecode. Here are your options:",
      Color = new DiscordColor("#b42b42")
    };

    var formatChars = EnumerableUtils.Of(
      ('d', "Short date"), ('D', "Long date"), ('f', "Short date/time"), ('f', "Long date/time"), ('t', "Short time"),
      ('T', "Long time"), ('R', "Relative time"), ('\0', "Unix timestamp"));

    if (format.HasValue)
    {
      formatChars = formatChars.Where(p => p.Item1 == format switch
      {
        NixTimestampFormat.ShortDate => 'd',
        NixTimestampFormat.LongDate => 'D',
        NixTimestampFormat.LongDateTime => 'F',
        NixTimestampFormat.ShortTime => 't',
        NixTimestampFormat.LongTime => 'T',
        NixTimestampFormat.RelativeTime => 'r',
        NixTimestampFormat.UnixTimestamp => '\0',
        _ => 'f' // default because saying nothing defaults to this too
      });
    }

    foreach ((char formatChar, string name) in formatChars)
    {
      if (formatChar == '\0')
        embed.AddField(unix.ToString(), $"{name}: ```\n{unix.ToString()}\n```", true);
      else
        embed.AddField($"<t:{unix}:{formatChar}>", $"{name}: ```\n<t:{unix}:{formatChar}>\n```", true);
    }

    await ctx.RespondAsync(embed, ephemeral);
  }

  [Command("set")]
  public static class TimeSetCommand
  {
    [Command("zone")]
    [Description("Set the default time zone")]
    public static async Task SetZoneCommand(SlashCommandContext ctx,
      [Description("What time zone to set as default")][SlashAutoCompleteProvider<TimeZoneAutoCompleteProvider>] string timezone
    )
    {
      DateTimeZone zoneForId = TzdbDateTimeZoneSource.Default.ForId(timezone);
      if (zoneForId != null)
      {
        Settings.TimeZone = zoneForId;
        await ctx.RespondAsync($"Set your time zone to `{timezone}`!", true);
      }
      else
      {
        await ctx.RespondAsync($"`{timezone}` is not a valid time zone.", true);
        return;
      }
    }
  }
}
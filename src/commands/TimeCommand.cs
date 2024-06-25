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
  RelativeTime
}

[InteractionAllowedContexts(new DiscordInteractionContextType[] {
  DiscordInteractionContextType.Guild,
  DiscordInteractionContextType.BotDM,
  DiscordInteractionContextType.PrivateChannel
})]
[InteractionInstallType(new DiscordApplicationIntegrationType[] {
  // DiscordApplicationIntegrationType.GuildInstall,
  DiscordApplicationIntegrationType.UserInstall
})]
[Command("time")]
[TopLevelCommand]
public static class TimeCommand
{
  static Regex TimeRegex = new(@"^(\d?\d):(\d\d)(?::(\d\d))?(?: ?([ap])\.?m?\.?)?$");
  static Regex DateRegex = new(@"^(?:(\d{4})[-/\.])?(\d?\d)[-/\.](\d\d)$");

  [InteractionInstallType(new DiscordApplicationIntegrationType[] {
    // DiscordApplicationIntegrationType.GuildInstall,
    DiscordApplicationIntegrationType.UserInstall
  })]
  [InteractionAllowedContexts(new DiscordInteractionContextType[] {
    DiscordInteractionContextType.Guild,
    DiscordInteractionContextType.BotDM,
    DiscordInteractionContextType.PrivateChannel
  })]
  [Command("code")]
  [Description("Get the <t:...> code for a given time of day")]
  public static async Task TimeCodeCommand(SlashCommandContext ctx,
    [Description("The time to get")] string time,
    [Description("The date to get")] string date = null,
    [Description("What time zone to use")][SlashAutoCompleteProvider<TimeZoneAutoCompleteProvider>] string timezone = null,
    [Description("What format to use")] NixTimestampFormat? format = null,
    [Description("Is entered time daylight saving time? Only has any effect for ambiguous times.")] bool? daylightSaving = null
  )
  {
    DateTimeZone zone = Settings.TimeZone;

    if (timezone != null)
    {
      DateTimeZone zoneForId = TzdbDateTimeZoneSource.Default.ForId(timezone);
      if (zoneForId != null) zone = zoneForId;
      else
      {
        await ctx.RespondAsync($"`{timezone}` is not a valid time zone.");
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
        await ctx.RespondAsync($"`{time}` isn't a valid time!");
        return;
      }
    }

    LocalDate lDate;

    var now = SystemClock.Instance.GetCurrentInstant();
    var zonedNow = now.InZone(zone);
    var localNow = zonedNow.LocalDateTime;

    if (date != null)
    {
      if (DateRegex.TryMatch(date, out Match mtc))
      {
        int year = 0;
        int month = int.Parse(mtc.Groups[2].Value);
        int day = int.Parse(mtc.Groups[3].Value);

        if (mtc.Groups[1].Success) year = int.Parse(mtc.Groups[1].Value);
        else
        {
          year = localNow.Year;
        }

        lDate = new(year, month, day);
      }
      else
      {
        await ctx.RespondAsync($"{date} isn't a valid date!");
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
      await ctx.RespondAsync($"The date/time {ldt} is not a valid time in the time zone {zone}!");
      return;
    }

    var zonedTime = asDst ? map.First() : map.Last();

    // And lastly, get the unix timestamp value
    var unix = zonedTime.ToInstant().ToUnixTimeSeconds();

    char formatChar = (format ?? Settings.TimeFormat) switch
    {
      NixTimestampFormat.ShortDate => 'd',
      NixTimestampFormat.LongDate => 'D',
      NixTimestampFormat.LongDateTime => 'F',
      NixTimestampFormat.ShortTime => 't',
      NixTimestampFormat.LongTime => 'T',
      NixTimestampFormat.RelativeTime => 'r',
      _ => 'f' // default because saying nothing defaults to this too
    };

    await ctx.RespondAsync($@"<t:{unix}:{formatChar}>", true);
    await ctx.FollowupAsync($@"\<t\:{unix}\:{formatChar}\>", true);
  }

  [Command("set")]
  [InteractionInstallType(new DiscordApplicationIntegrationType[] {
    // DiscordApplicationIntegrationType.GuildInstall,
    DiscordApplicationIntegrationType.UserInstall
  })]
  [InteractionAllowedContexts(new DiscordInteractionContextType[] {
    DiscordInteractionContextType.Guild,
    DiscordInteractionContextType.BotDM,
    DiscordInteractionContextType.PrivateChannel
  })]
  public static class TimeSetCommand
  {
    [Command("zone")]
    [Description("Set the default time zone")]
    [InteractionInstallType(new DiscordApplicationIntegrationType[] {
      // DiscordApplicationIntegrationType.GuildInstall,
      DiscordApplicationIntegrationType.UserInstall
    })]
    [InteractionAllowedContexts(new DiscordInteractionContextType[] {
      DiscordInteractionContextType.Guild,
      DiscordInteractionContextType.BotDM,
      DiscordInteractionContextType.PrivateChannel
    })]
    public static async Task SetZoneCommand(SlashCommandContext ctx,
      [Description("What time zone to set as default")][SlashAutoCompleteProvider<TimeZoneAutoCompleteProvider>] string timezone
    )
    {
      DateTimeZone zoneForId = TzdbDateTimeZoneSource.Default.ForId(timezone);
      if (zoneForId != null)
      {
        Settings.TimeZone = zoneForId;
        await ctx.RespondAsync($"Set your time zone to `{timezone}`!");
      }
      else
      {
        await ctx.RespondAsync($"`{timezone}` is not a valid time zone.");
        return;
      }
    }

    [Command("format")]
    [Description("Set the default format")]
    [InteractionInstallType(new DiscordApplicationIntegrationType[] {
      // DiscordApplicationIntegrationType.GuildInstall,
      DiscordApplicationIntegrationType.UserInstall
    })]
    [InteractionAllowedContexts(new DiscordInteractionContextType[] {
      DiscordInteractionContextType.Guild,
      DiscordInteractionContextType.BotDM,
      DiscordInteractionContextType.PrivateChannel
    })]
    public static async Task SetFormatCommand(SlashCommandContext ctx,
      [Description("What format to set as default")] NixTimestampFormat format
    )
    {
      Settings.TimeFormat = format;
      await ctx.RespondAsync($"Set your time format to {format}!");
    }
  }
}
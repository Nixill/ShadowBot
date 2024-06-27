using System.ComponentModel;
using System.Text.RegularExpressions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using Nixill.Utils;
using NodaTime;

namespace Nixill.Discord.ShadowBot;

[InteractionAllowedContexts(
  DiscordInteractionContextType.Guild,
  DiscordInteractionContextType.BotDM,
  DiscordInteractionContextType.PrivateChannel
)]
[InteractionInstallType(
  DiscordApplicationIntegrationType.UserInstall
)]
[Command("Snowflake")]
[TopLevelCommand]
public static class SnowflakeCommand
{
  static Regex SnowflakeRegex = new(@"^https:\/\/discord\.com\/channels\/(@me|\d+)\/(\d+)(?:\/(\d+))$");

  static readonly Instant Epoch = Instant.FromUtc(2015, 1, 1, 0, 0);

  [Command("to_time")]
  [Description("Get the timestamp of a snowflake or URL object.")]
  public static async Task SnowflakeToTimeCommand(SlashCommandContext ctx,
    [Description("The snowflake OR url for which to get one or more timestamps.")] string input,
    [Description("Hide from others? (May be forced by the server anyway.)")] bool ephemeral = true
  )
  {
    DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
    {
      Description = $"Your input: `{input}`",
      Color = new DiscordColor("b42b42")
    };

    foreach ((Instant time, string label, ulong stamp) in GetInstantForSnowflakes(input))
    {
      builder.AddField(label, Description(stamp));
    }

    if (builder.Fields.Count == 0) builder.AddField("Oops!", "There are no recognized snowflakes in your input.");

    await ctx.RespondAsync(builder, ephemeral);
  }

  [Command("to Time")]
  [Description("Get the timestamps of a snowflake associated with a message.")]
  [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
  public static async Task SnowflakeToTimeCommand(SlashCommandContext ctx,
    DiscordMessage msg
  )
  {
    DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
    {
      Description = $"Your selected message: [This one]({msg.JumpLink})",
      Color = new DiscordColor("b42b42")
    };

    embed.AddField("Message", Description(msg.Id), true);

    ulong? id = msg.Author?.Id;
    if (id.HasValue) embed.AddField("Author", Description(id.Value), true);

    embed.AddField("Channel", Description(msg.ChannelId), true);

    id = msg.Channel?.GuildId;
    if (id.HasValue) embed.AddField("Server", Description(id.Value), true);

    id = msg.WebhookId;
    if (id.HasValue) embed.AddField("Webhook", Description(id.Value), true);

    await ctx.RespondAsync(embed, true);
  }

  static IEnumerable<(Instant Time, string Label, ulong Stamp)> GetInstantForSnowflakes(string input)
  {
    if (ulong.TryParse(input, out ulong stamp))
    {
      yield return (InstantOf(stamp), "Input", stamp);
    }
    else if (SnowflakeRegex.TryMatch(input, out Match mtc))
    {
      ulong val;

      if (mtc.Groups[1].Success && ulong.TryParse(mtc.Groups[1].Value, out val))
        yield return (InstantOf(val), "Server", val);

      if (mtc.Groups[2].Success && ulong.TryParse(mtc.Groups[2].Value, out val))
        yield return (InstantOf(val), "Channel", val);

      if (mtc.Groups[3].Success && ulong.TryParse(mtc.Groups[3].Value, out val))
        yield return (InstantOf(val), "Message", val);
    }
  }

  static ulong Milliseconds(ulong snowflake) => snowflake >> 22;
  static Instant InstantOf(ulong snowflake) => Epoch + Duration.FromMilliseconds(Milliseconds(snowflake));
  static string Description(ulong stamp) => Description(stamp, InstantOf(stamp));
  static string Description(ulong stamp, Instant time) => $"{stamp}: <t:{time.ToUnixTimeSeconds()}:f> (+ `{time.ToUnixTimeMilliseconds() % 1000}` ms)";
}

public static class SnowflakeMessageCommand
{

}
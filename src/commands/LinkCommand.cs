using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using Nixill.Utils;

namespace Nixill.Discord.ShadowBot;

[TopLevelCommand]
[Command("link")]
[InteractionAllowedContexts(
  DiscordInteractionContextType.Guild,
  DiscordInteractionContextType.BotDM,
  DiscordInteractionContextType.PrivateChannel
)]
[InteractionInstallType(
  DiscordApplicationIntegrationType.UserInstall
)]
public static class LinkCommand
{
  [Command("shorten")]
  [Description("Strip extraneous fluff out of a URL")]
  public static async Task ShortenCommand(SlashCommandContext ctx,
    [Description("The original URL")] string url
  )
  {
    Uri[] shortened = UrlShortener.Shorten(url).ToArray();

    if (shortened.Length == 0)
    {
      await ctx.RespondAsync($"(Not yet available for this domain.)");
      return;
    }

    await ctx.RespondAsync($"{shortened[0]}", true);
    if (shortened.Count() > 1)
      await ctx.FollowupAsync($"Others:\n<{shortened.Skip(1).SJoin(">\n<")}>", true);
  }
}
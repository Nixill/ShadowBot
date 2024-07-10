using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;

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
    Uri shortened = UrlShortener.Shorten(url);
    await ctx.RespondAsync($"{shortened}", true);
  }
}
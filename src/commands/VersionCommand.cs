using System.Diagnostics;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;

namespace Nixill.Discord.ShadowBot;

[TopLevelCommand]
public static class VersionCommandClass
{
  static ValueTask<(bool Success, string Output, string Error)> Run(string prog, string args, bool throwOnError = true)
  {
    Process proc = new Process()
    {
      StartInfo = {
        FileName = prog,
        Arguments = args,
        WorkingDirectory = Directory.GetCurrentDirectory(),
        RedirectStandardError = true,
        RedirectStandardOutput = true
      }
    };

    proc.Start();
    proc.WaitForExit();

    return ValueTask.FromResult((proc.ExitCode == 0, proc.StandardOutput.ReadToEnd(), proc.StandardError.ReadToEnd()));
  }

  [InteractionAllowedContexts(
    DiscordInteractionContextType.Guild,
    DiscordInteractionContextType.BotDM,
    DiscordInteractionContextType.PrivateChannel
  )]
  [InteractionInstallType(
    DiscordApplicationIntegrationType.UserInstall
  )]
  [Command("version")]
  public static async Task VersionCommand(SlashCommandContext ctx)
  {
    await ctx.DeferResponseAsync(true);

    var hash = Run("git", "rev-parse HEAD");
    var msg = Run("git", "log -1 --pretty=%B");
    var tag = Run("git", "describe --tags --abbrev=0");

    await ctx.EditResponseAsync($"I am running {(
      tag.Result.Success ? $"version `{tag.Result.Output.Trim()}`" : "an untagged repository"
    )} on commit hash `{hash.Result.Output.Trim()}` (\"{(msg.Result.Output.Trim())}\")");
  }
}
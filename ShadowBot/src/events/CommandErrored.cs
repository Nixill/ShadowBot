using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Processors.SlashCommands;

namespace Nixill.Discord.ShadowBot;

public class CommandErrorHandler
{
  internal static async Task OnCommandErrored(CommandsExtension sender, CommandErroredEventArgs args)
  {
    if (args.Context is SlashCommandContext sctx)
    {
      var ex = args.Exception;
      var intr = sctx.Interaction;
      var state = intr.ResponseState;

      var content = $"{args.Exception.Message}{(ex is UserInputException ? "" : "\n```{args.Exception.StackTrace}\n```)")}";

      if (state == DSharpPlus.Entities.DiscordInteractionResponseState.Unacknowledged)
      {
        await intr.CreateResponseAsync(
          DSharpPlus.Entities.DiscordInteractionResponseType.ChannelMessageWithSource,
          new DSharpPlus.Entities.DiscordInteractionResponseBuilder()
          {
            Content = content,
            IsEphemeral = true
          }
        );
      }
      else if (state == DSharpPlus.Entities.DiscordInteractionResponseState.Deferred)
      {
        await intr.EditOriginalResponseAsync(
          new DSharpPlus.Entities.DiscordWebhookBuilder()
          {
            Content = content
          }
        );
      }
      else
      {
        await intr.CreateFollowupMessageAsync(
          new DSharpPlus.Entities.DiscordFollowupMessageBuilder()
          {
            Content = content,
            IsEphemeral = true
          }
        );
      }
    }
  }
}
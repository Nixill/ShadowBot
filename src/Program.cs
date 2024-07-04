using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nixill.Discord.ShadowBot;

public class ShadowBotMain
{
  internal static CancellationTokenSource QuitTokenSource = new();

  static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();

  public static async Task MainAsync()
  {
    // Let's get the bot set up
    string botToken = File.ReadAllText("cfg/token");

    ulong OwnerID = ulong.Parse(File.ReadAllText("cfg/owner"));

    IServiceProvider serviceProvider = new ServiceCollection().AddLogging(x => x.AddConsole()).BuildServiceProvider();

    DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(botToken, DiscordIntents.None);
    builder.SetLogLevel(LogLevel.Trace);

    // builder.ConfigureEventHandlers(
    // );

    DiscordClient discord = builder.Build();

    await discord.ConnectAsync();

    CommandsExtension commands = discord.UseCommands(
      new CommandsConfiguration()
      {
        RegisterDefaultCommandProcessors = false,
        UseDefaultCommandErrorHandler = false,
        DebugGuildId = Settings.DebugGuildId
      }
    );

    await commands.AddProcessorAsync(new SlashCommandProcessor());
    await commands.AddProcessorAsync(new MessageCommandProcessor());

    commands.CommandErrored += CommandErrorHandler.OnCommandErrored;

    commands.AddCommands(TopLevelCommandAttribute.GetTypesWith(typeof(ShadowBotMain).Assembly));

    try
    {
      await Task.Delay(-1, QuitTokenSource.Token);
    }
    catch (TaskCanceledException)
    {
      Settings.SaveObject();
    }
  }
}
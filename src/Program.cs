using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
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
#if DEBUG
    Console.WriteLine("Debug mode active");
#else
    Console.WriteLine("Debug mode not active");
#endif
    string botToken = File.ReadAllText("cfg/token");

    ulong OwnerID = ulong.Parse(File.ReadAllText("cfg/owner"));

    IServiceProvider serviceProvider = new ServiceCollection().AddLogging(x => x.AddConsole()).BuildServiceProvider();

    DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(botToken, DiscordIntents.None);
    DiscordClient discord = builder.Build();

    CommandsExtension commands = discord.UseCommands(
      new CommandsConfiguration()
      {
        DebugGuildId = 299573383836729344L,
        RegisterDefaultCommandProcessors = false
      }
    );

    await commands.AddProcessorAsync(new SlashCommandProcessor());

    commands.AddCommands(TopLevelCommandAttribute.GetTypesWith(typeof(ShadowBotMain).Assembly));

    await discord.ConnectAsync();

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
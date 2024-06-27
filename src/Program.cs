﻿using DSharpPlus;
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

    await discord.ConnectAsync();

    CommandsExtension commands = discord.UseCommands(
      new CommandsConfiguration()
      {
        RegisterDefaultCommandProcessors = false
      }
    );

    await commands.AddProcessorAsync(new SlashCommandProcessor());
    await commands.AddProcessorAsync(new MessageCommandProcessor());

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
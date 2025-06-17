using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace Nixill.Discord.ShadowBot;

public class LinkGetAutoCompleteProvider : SimpleAutoCompleteProvider
{
  static DiscordAutoCompleteChoice[] Links = File.ReadAllLines("cfg/links.txt") // string[]
    .Where(l => l.Length > 0 && !l.StartsWith("#")) // IEnumerable<string>
    .Select(l => l.Split(" ", 2)) // IEnumerable<string[]>
    .Where(a => a.Length == 2) // IEnumerable<string[]>
    .Select(a => new DiscordAutoCompleteChoice(a[1], a[0])) // IEnumerable<DiscordAutoCompleteChoice>
    .ToArray();

  protected override IEnumerable<DiscordAutoCompleteChoice> Choices => Links;
  protected override bool AllowDuplicateValues => false;
}
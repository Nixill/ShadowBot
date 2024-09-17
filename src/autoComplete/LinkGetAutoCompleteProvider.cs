using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace Nixill.Discord.ShadowBot;

public class LinkGetAutoCompleteProvider : IAutoCompleteProvider
{
  IDictionary<string, object> Links = File.ReadAllLines("cfg/links.txt")
    .Where(l => l.Length > 0 && !l.StartsWith("#"))
    .Select(l => l.Split(" ", 2)) // IEnumerable<string[]>
    .Where(a => a.Length == 2)
    .Select(a => (a[1], (object)a[0])) // IEnumerable<(string, object)>
    .ToDictionary();

  public ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
  {
    string input = ctx.UserInput;
    IReadOnlyDictionary<string, object> output = Links
      .Where(kvp => kvp.Key.Contains(input, StringComparison.CurrentCultureIgnoreCase))
      .ToDictionary()
      .AsReadOnly();
    return ValueTask.FromResult(output);
  }
}
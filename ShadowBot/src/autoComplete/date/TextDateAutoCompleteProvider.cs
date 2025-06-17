using System.Text.RegularExpressions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Nixill.Utils;
using NodaTime;
using NodaTime.Text;

namespace Nixill.Discord.ShadowBot;

public partial class DateAutoCompleteProvider : IAutoCompleteProvider
{
  StringComparison icic = StringComparison.InvariantCultureIgnoreCase;

  private IEnumerable<(string, LocalDate, int)> GetDatesText(string input)
  {
    LocalDate today = Today;

    foreach (var kvp in MonthKeys)
    {
      int priority = 0;
      if (kvp.Key.StartsWith(input, icic)) priority = 15;
      else if (kvp.Key.Contains(input, icic)) priority = 10;
      else if (Regex.IsMatch(kvp.Key, string.Join(".*", input), RegexOptions.IgnoreCase)) priority = 5;
      else continue;

      LocalDate previous = DateMath.Previous(DateMath.SafeAnnualDate(kvp.Value, today.Day));
      yield return DateTuple($"Previous {kvp.Key}", previous, priority);
      LocalDate next = DateMath.Next(DateMath.SafeAnnualDate(kvp.Value, today.Day));
      yield return DateTuple($"Next {kvp.Key}", next, priority);
      if (next.Year - previous.Year == 2) yield return DateTuple($"Current {kvp.Key}", today, priority);
    }

    foreach (var kvp in DaysOfWeek)
    {
      int priority = 0;
      if (kvp.Key.StartsWith(input, icic)) priority = 15;
      else if (kvp.Key.Contains(input, icic)) priority = 10;
      else if (Regex.IsMatch(kvp.Key, string.Join(".*", input), RegexOptions.IgnoreCase)) priority = 5;
      else continue;

      yield return DateTuple($"Previous {kvp.Value}", today.Previous(kvp.Value), priority);
      yield return DateTuple($"Next {kvp.Value}", today.Next(kvp.Value), priority);
      if (today.DayOfWeek == kvp.Value) yield return DateTuple($"Current {kvp.Value}", today, priority);
    }

    foreach (var kvp in OffsetDays)
    {
      int priority = 0;
      if (kvp.Key.StartsWith(input, icic)) priority = 15;
      else if (kvp.Key.Contains(input, icic)) priority = 10;
      else if (Regex.IsMatch(kvp.Key, string.Join(".*", input), RegexOptions.IgnoreCase)) priority = 5;
      else continue;

      yield return DateTuple(kvp.Key, today + kvp.Value, priority);
    }
  }
}
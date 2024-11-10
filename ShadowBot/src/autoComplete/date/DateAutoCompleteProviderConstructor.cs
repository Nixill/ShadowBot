using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Nixill.Utils;
using Nixill.Utils.Extensions;
using NodaTime;

namespace Nixill.Discord.ShadowBot;

public partial class DateAutoCompleteProvider : IAutoCompleteProvider
{
  static DateAutoCompleteProvider()
  {
    IEnumerable<LocalDate> dates = AllDatesInRange(new LocalDate(2024, 1, 1), new LocalDate(2024, 12, 31));

    var fours = dates
      .SelectMany(d => ((IEnumerable<(string, AnnualDate)>)[
          ($"{d.Month:00}{d.Day:00}", new AnnualDate(d.Month, d.Day)),
          ($"{d.Day:00}{d.Month:00}", new AnnualDate(d.Month, d.Day))])
        .Distinct());
    var threes = fours.SelectMany(p => ((IEnumerable<(string, AnnualDate)>)[(p.Item1[0..3], p.Item2), (p.Item1[1..4], p.Item2)]).Distinct());
    FourDigitDates = fours.GroupBy(t => t.Item1, t => t.Item2).ToDictionary().AsReadOnly();
    ThreeDigitDates = threes.GroupBy(t => t.Item1, t => t.Item2).ToDictionary().AsReadOnly();
  }
}
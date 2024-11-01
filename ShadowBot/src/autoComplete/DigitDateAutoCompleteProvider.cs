using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using NodaTime;

namespace Nixill.Discord.ShadowBot;

public partial class DateAutoCompleteProvider : IAutoCompleteProvider
{
  private IEnumerable<(string, LocalDate)> GetDatesDigits(string input)
  {
    if (!int.TryParse(input, out int number)) return [];

    switch (input.Length)
    {
      case 1: return GetDates1Digit(input, number);
      case 2: return GetDates2Digits(input, number);
      case 3: return GetDates3Digits(input, number);
      case 4: return GetDates4Digits(input, number);
      case 5: return GetDates5Digits(input, number);
      case 6: return GetDates6Digits(input, number);
      case 7: case 8: return GetDates7Digits(input, number);
    }
  }

  private IEnumerable<(string, LocalDate)> GetDates1Digit(string input, int number)
  {
    LocalDate today = Today;
    LocalDate start = Today - Period.FromWeeks(1);
    LocalDate end = Today + Period.FromMonths(1);

    int yearMonthToday = today.Year * 12 + today.Month;

    for (LocalDate date = start; date <= end; date += Day)
    {
      int yearMonthThen = date.Year * 12 + date.Month;
      if (date.Day.ToString("00").Contains(input))
        yield return DateTuple($"The {Ordinal(date.Day)}{(
          (yearMonthThen < yearMonthToday) ? " of last month"
          : (yearMonthThen > yearMonthToday) ? " of next month"
          : ""
        )}", date);
    }

    for (int i = -12; i <= 12; i++)
    {
      LocalDate date = today.PlusMonths(i);
      if (date.Month.ToString("00").Contains(input))
        yield return DateTuple($"{(
          (date.Year < today.Year) ? "Last "
          : (date.Year > today.Year) ? "Next "
          : ""
        )} {}")
    }
  }
}

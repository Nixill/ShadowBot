using System.Text;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Nixill.Utils.Extensions;
using NodaTime;
using NodaTime.Text;

namespace Nixill.Discord.ShadowBot;

public partial class DateAutoCompleteProvider : IAutoCompleteProvider
{
  private static IReadOnlyDictionary<string, IEnumerable<AnnualDate>> ThreeDigitDates;
  private static IReadOnlyDictionary<string, IEnumerable<AnnualDate>> FourDigitDates;

  static LocalDatePattern MonthName => LocalDatePattern.CreateWithInvariantCulture("MMMM");
  static LocalDatePattern MMMD => LocalDatePattern.CreateWithInvariantCulture("MMM d");

  private IEnumerable<(string, LocalDate, int)> GetDatesDigits(string input)
  {
    if (!int.TryParse(input, out int number)) return [];

    IEnumerable<(string, LocalDate, int)> dates = input.Length switch
    {
      1 => GetDates1Digit(input, number),
      2 => GetDates2Digits(input, number),
      3 => GetDates3Digits(input, number),
      4 => GetDates4Digits(input, number),
      8 => GetDates8Digits(input, number),
      _ => []
    };

    if (number != 0)
    {
      try
      {
        LocalDate before = Today.PlusDays(-number);
        if (before.Year >= 1) dates = dates.Append(DateTuple($"{number} day{(number != 1 ? "s" : "")} ago", before, 0));
      }
      catch { }
      try
      {
        LocalDate after = Today.PlusDays(number);
        if (after.Year <= 9999) dates = dates.Append(DateTuple($"{number} day{(number != 1 ? "s" : "")} from now", after, 0));
      }
      catch { }
    }
    else
    {
      dates = dates.Append(DateTuple("0 days ago/from now", Today, 0));
    }

    return dates;
  }

  private IEnumerable<(string, LocalDate, int)> GetDates1Digit(string input, int number)
  {
    LocalDate today = Today;

    // Day within -1 week .. +1 month where day contains digit
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

    // Day within -1 year .. +1 year where day matches today and month
    // contains digit
    for (int i = -12; i <= 12; i++)
    {
      LocalDate date = today.PlusMonths(i);
      if (date.Month.ToString("00").Contains(input))
        yield return DateTuple($"{(
          (date.Year < today.Year) ? "Last "
          : (date.Year > today.Year) ? "Next "
          : ""
        )} {MonthName.Format(date)}", date);
    }
  }

  private IEnumerable<(string, LocalDate, int)> GetDates2Digits(string input, int number)
  {
    LocalDate today = Today;

    if (number >= 1 && number <= 31)
    {
      LocalDate start = Today.PlusMonths(-1);
      LocalDate end = Today.PlusMonths(2);

      IEnumerable<string> DateName(LocalDate date)
      {
        yield return Ordinal(date.Day);
        yield return " of ";
        if (date.Year < today.Year) yield return "last ";
        if (date.Year > today.Year) yield return "next ";
        yield return MonthName.Format(date);
      }

      foreach (LocalDate date in AllDatesInRange(start, end).Where(d => d.Day == number))
        yield return DateTuple(d => DateName(d).StringJoin(""), date);
    }

    if (number >= 1 && number <= 12)
    {
      AnnualDate date = DateMath.SafeAnnualDate(number, today.Day);
      LocalDate previousDate = DateMath.Previous(date, false);
      LocalDate nextDate = DateMath.Next(date, false);
      yield return DateTuple(d => $"Previous {MonthName.Format(d)}", previousDate);
      yield return DateTuple(d => $"Next {MonthName.Format(d)}", nextDate);
      if (nextDate.Year - previousDate.Year == 2)
        yield return DateTuple(d => $"Current {MonthName.Format(d)}", today);
    }
  }

  private IEnumerable<(string, LocalDate, int)> GetDates3Digits(string input, int number)
  {
    LocalDate today = Today;
    if (ThreeDigitDates.TryGetValue(input, out var dates))
      foreach (AnnualDate date in dates)
      {
        LocalDate previousDate = DateMath.Previous(date, false);
        if (previousDate >= today.PlusDays(-7))
          yield return DateTuple(d => $"Previous {MMMD.Format(d)}", previousDate);

        yield return DateTuple(d => $"Next {MMMD.Format(d)}", DateMath.Next(date, false));
        if (today == date.InYear(today.Year))
          yield return DateTuple($"Today!", today);
      }
  }

  private IEnumerable<(string, LocalDate, int)> GetDates4Digits(string input, int number)
  {
    LocalDate today = Today;
    if (FourDigitDates.TryGetValue(input, out var dates))
      foreach (AnnualDate date in dates)
      {
        yield return DateTuple(d => $"Previous {d}", DateMath.Previous(date, false));
        yield return DateTuple(d => $"Next {d}", DateMath.Next(date, false));
        if (today == date.InYear(today.Year))
          yield return DateTuple($"Today!", today);
      }

    yield return DateTuple(d => $"Today in {number}", DateMath.SafeLocalDate(number, today.Month, today.Day));
  }

  private IEnumerable<(string, LocalDate, int)> GetDates8Digits(string input, int number)
  {
    try
    {
      yield return DateTuple("Typed date",
        new LocalDate(int.Parse(input[0..4]), int.Parse(input[4..6]), int.Parse(input[6..8])));
    }
    finally { }
  }
}

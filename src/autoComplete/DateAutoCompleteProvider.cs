using System.Text.RegularExpressions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Nixill.Utils;
using Nixill.Utils.Temp;
using NodaTime;
using NodaTime.Text;

namespace Nixill.Discord.ShadowBot;

public class DateAutoCompleteProvider : IAutoCompleteProvider
{
  static readonly IReadOnlyDictionary<string, IsoDayOfWeek> DaysOfWeek = Enum.GetValues<IsoDayOfWeek>()
    .Except([IsoDayOfWeek.None])
    .Select(e => (e.ToString().ToLower(), e))
    .ToDictionary();

  static readonly IReadOnlyDictionary<string, Period> OffsetDays = new Dictionary<string, Period> {
    ["today"] = Period.Zero,
    ["yesterday"] = Days(-1),
    ["tomorrow"] = Day,
    ["ubermorgen"] = Days(2)
  };

  static readonly IReadOnlyDictionary<string, int> MonthNames = new Dictionary<string, int> {
    ["january"] = 1,
    ["february"] = 2,
    ["march"] = 3,
    ["april"] = 4,
    ["may"] = 5,
    ["june"] = 6,
    ["july"] = 7,
    ["august"] = 8,
    ["september"] = 9,
    ["october"] = 10,
    ["november"] = 11,
    ["december"] = 12
  };

  static Instant Now => SystemClock.Instance.GetCurrentInstant();
  static LocalDate Today => Now.InZone(Settings.TimeZone).LocalDateTime.Date;
  static Period Day => Period.FromDays(1);
  static Period Days(int i) => Period.FromDays(i);

  public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
  {
    await Task.Delay(0);
    return GetDates(ctx).DistinctBy(x => x.Item2).ToDictionary();
  }

  static string Iso(LocalDate date) => LocalDatePattern.Iso.Format(date);

  static readonly Regex DigitsOnly = new(@"^\d+$");
  static readonly Regex Signed = new(@"^[-+]\d+$");
  static readonly Regex Hyphen = new(@"^(\d\d?)[\.\-\/](\d*)$");

  static (string, object) DateTuple(LocalDate date) => (Iso(date), Iso(date));
  static (string, object) DateTuple(string comment, LocalDate date) => ($"{comment} ({Iso(date)})", Iso(date));
  static (string, object) DateTuple(Func<LocalDate, object> comment, LocalDate date) => ($"{comment(date)} ({Iso(date)})", Iso(date));

  static string Ordinal(int i) => (i % 100 >= 11 && i % 100 <= 13) ? $"{i}th"
    : (i % 10 == 1) ? $"{i}st"
    : (i % 10 == 2) ? $"{i}nd"
    : (i % 10 == 3) ? $"{i}rd"
    : $"{i}th";

  private IEnumerable<(string, object)> GetDates(AutoCompleteContext ctx)
  {
    string input = ctx.UserInput;
    LocalDate today = Today;

    Match mtc;
    
    if (input == "")
    {
      // Output -1 to +7 days
      yield return DateTuple("Today", today);
      yield return DateTuple("Tomorrow", today + Day);
      yield return DateTuple("Yesterday", today - Day);
      foreach (int i in Enumerable.Range(2, 4))
        yield return DateTuple(d => d.DayOfWeek, today + Days(i));
      foreach (int i in Enumerable.Range(6, 2))
        yield return DateTuple(d => $"Next {d.DayOfWeek}", today + Days(i));
      yield break;
    }
    else if (DigitsOnly.TryMatch(input, out mtc))
    {
      // Only output anything if it can be parsed to a number
      if (!int.TryParse(input, out int number)) yield break;

      if (input.Length == 1)
      {
        // Current month, given day?
        var daysInMonth = CalendarSystem.Gregorian.GetDaysInMonth(today.Year, today.Month);

        for (LocalDate date = today - Day; date <= today.PlusMonths(1); date += Day)
        {
          
        }

        // Current day, given month?
      }
    }
  }
}
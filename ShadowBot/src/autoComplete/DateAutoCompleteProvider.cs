using System.Text.RegularExpressions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Nixill.Utils;
using NodaTime;
using NodaTime.Text;

namespace Nixill.Discord.ShadowBot;

public partial class DateAutoCompleteProvider : IAutoCompleteProvider
{
  static readonly IReadOnlyDictionary<string, IsoDayOfWeek> DaysOfWeek = Enum.GetValues<IsoDayOfWeek>()
    .Except([IsoDayOfWeek.None])
    .Select(e => (e.ToString().ToLower(), e))
    .ToDictionary();

  static readonly IReadOnlyDictionary<string, Period> OffsetDays = new Dictionary<string, Period>
  {
    ["today"] = Period.Zero,
    ["yesterday"] = Days(-1),
    ["tomorrow"] = Day,
    ["ubermorgen"] = Days(2)
  };

  static readonly IReadOnlyDictionary<string, int> MonthNames = new Dictionary<string, int>
  {
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
    IEnumerable<(string, LocalDate)> dates = GetDates(ctx);

    try {
      LocalDate date = TimeCommand.ParseDate(ctx.UserInput, Now.InZone(Settings.TimeZone));
      dates.Prepend(DateTuple("Current input", date));
    } catch (UserInputException) {}

    LocalDate today = Today;

    return dates
      .DistinctBy(p => p.Item2)
      .Order(Comparer<(string, LocalDate)>.Create((l, r) => {

        int leftPeriod = int.Abs(Period.DaysBetween(today, l.Item2));
        int rightPeriod = int.Abs(Period.DaysBetween(today, r.Item2));

        if (leftPeriod != rightPeriod) return leftPeriod.CompareTo(rightPeriod);
        return -(l.CompareTo(r));
      }))
      .Take(25)
      .Select(p => ((string,object))(p.Item1, Iso(p.Item2)))
      .ToDictionary();
  }

  static string Iso(LocalDate date) => LocalDatePattern.Iso.Format(date);

  static readonly Regex DigitsOnly = new(@"^\d+$");
  static readonly Regex Signed = new(@"^[-+]\d+$");
  static readonly Regex Hyphen = new(@"^(\d\d?)[\.\-\/](\d*)$");

  static (string, LocalDate) DateTuple(LocalDate date) => (Iso(date), date);
  static (string, LocalDate) DateTuple(string comment, LocalDate date) => ($"{comment} ({Iso(date)})", date);
  static (string, LocalDate) DateTuple(Func<LocalDate, object> comment, LocalDate date) => ($"{comment(date)} ({Iso(date)})", date);

  static string Ordinal(int i) => (i % 100 >= 11 && i % 100 <= 13) ? $"{i}th"
    : (i % 10 == 1) ? $"{i}st"
    : (i % 10 == 2) ? $"{i}nd"
    : (i % 10 == 3) ? $"{i}rd"
    : $"{i}th";

  private IEnumerable<(string, LocalDate)> GetDates(AutoCompleteContext ctx)
  {
    string input = ctx.UserInput;
    LocalDate today = Today;

    Match mtc;

    if (input == "")
      return GetDatesBlank();
    else if (DigitsOnly.TryMatch(input, out mtc))
      return GetDatesDigits(input);
  }

  private IEnumerable<(string, LocalDate)> GetDatesBlank()
  {
    LocalDate today = Today;

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
}
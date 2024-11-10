using System.Text.RegularExpressions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Nixill.Utils;
using Nixill.Utils.Extensions;
using NodaTime;
using NodaTime.Text;

namespace Nixill.Discord.ShadowBot;

public partial class DateAutoCompleteProvider : IAutoCompleteProvider
{
  static readonly IReadOnlyDictionary<string, IsoDayOfWeek> DaysOfWeek = Enum.GetValues<IsoDayOfWeek>()
    .Except([IsoDayOfWeek.None])
    .Select(e => (e.ToString(), e))
    .ToDictionary();

  static readonly IReadOnlyDictionary<string, Period> OffsetDays = new Dictionary<string, Period>
  {
    ["Today"] = Period.Zero,
    ["Yesterday"] = Days(-1),
    ["Tomorrow"] = Day,
    ["Ubermorgen"] = Days(2)
  };

  static readonly IReadOnlyDictionary<string, int> MonthKeys = new Dictionary<string, int>
  {
    ["January"] = 1,
    ["February"] = 2,
    ["March"] = 3,
    ["April"] = 4,
    ["May"] = 5,
    ["June"] = 6,
    ["July"] = 7,
    ["August"] = 8,
    ["September"] = 9,
    ["October"] = 10,
    ["November"] = 11,
    ["December"] = 12
  };

  static readonly IReadOnlyDictionary<int, string> MonthNames = MonthKeys
    .Select(kvp => (kvp.Value, kvp.Key))
    .ToDictionary();

  static Instant Now => SystemClock.Instance.GetCurrentInstant();
  static LocalDate Today => Now.InZone(Settings.TimeZone).LocalDateTime.Date;
  static Period Day => Period.FromDays(1);
  static Period Days(int i) => Period.FromDays(i);

  public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
  {
    await Task.Delay(0);
    IEnumerable<(string, LocalDate, int)> dates = GetDates(ctx);

    try
    {
      LocalDate date = TimeCommand.ParseDate(ctx.UserInput, Now.InZone(Settings.TimeZone));
      dates.Prepend(DateTuple("Current input", date, 100));
    }
    catch (UserInputException) { }

    LocalDate today = Today;

    return dates
      .DistinctBy(p => p.Item2)
      .OrderByDescending(p => p.Item3)
      .ThenBy(i => i.Item2, Comparer<LocalDate>.Create((l, r) =>
      {
        int leftPeriod = int.Abs(Period.DaysBetween(today, l));
        int rightPeriod = int.Abs(Period.DaysBetween(today, r));

        if (leftPeriod != rightPeriod) return leftPeriod.CompareTo(rightPeriod);
        else return -(l.CompareTo(r));
      }))
      .Take(25)
      .Select(p => ((string, object))(p.Item1, Iso(p.Item2)))
      .ToDictionary();
  }

  static string Iso(LocalDate date) => LocalDatePattern.Iso.Format(date);

  static IEnumerable<LocalDate> AllDatesInRange(LocalDate start, LocalDate end)
    => Sequence.For(start, d => d <= end, d => d.PlusDays(1));

  static readonly Regex DigitsOnly = new(@"^\d+$");
  static readonly Regex Signed = new(@"^[-+]\d+$");
  static readonly Regex Text = new(@"^[A-Za-z]+$");

  static (string, LocalDate, int) DateTuple(LocalDate date, int priority = 0) => (Iso(date), date, priority);
  static (string, LocalDate, int) DateTuple(string comment, LocalDate date, int priority = 0)
    => ($"{comment} ({Iso(date)})", date, priority);
  static (string, LocalDate, int) DateTuple(Func<LocalDate, object> comment, LocalDate date, int priority = 0)
    => ($"{comment(date)} ({Iso(date)})", date, priority);

  static string Ordinal(int i) => (i % 100 >= 11 && i % 100 <= 13) ? $"{i}th"
    : (i % 10 == 1) ? $"{i}st"
    : (i % 10 == 2) ? $"{i}nd"
    : (i % 10 == 3) ? $"{i}rd"
    : $"{i}th";

  private IEnumerable<(string, LocalDate, int)> GetDates(AutoCompleteContext ctx)
  {
    string input = ctx.UserInput;
    LocalDate today = Today;

    Match mtc;

    if (input == "")
      return GetDatesBlank();
    else if (DigitsOnly.TryMatch(input, out mtc))
      return GetDatesDigits(input);
    else if (Signed.TryMatch(input, out mtc))
      return GetDatesSigned(input);
    else if (Text.TryMatch(input, out mtc))
      return GetDatesText(input);
    else
      return GetDatesMultiPart(input);
  }

  private IEnumerable<(string, LocalDate, int)> GetDatesBlank()
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

  private IEnumerable<(string, LocalDate, int)> GetDatesSigned(string input)
  {
    LocalDate today = Today;

    if (int.TryParse(input, out int number))
      try
      {
        yield return DateTuple($"{input} day{(number == 1 || number == -1 ? "" : "s")}", today.PlusDays(number));
      }
      finally { }
  }
}
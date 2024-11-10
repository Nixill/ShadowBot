using System.Text.RegularExpressions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Nixill.Utils.Extensions;
using NodaTime;
using NodaTime.Text;

namespace Nixill.Discord.ShadowBot;

public partial class DateAutoCompleteProvider : IAutoCompleteProvider
{
  Regex rgxMultipart = new(@"([A-Za-z]+|[0-9]+)");

  private IEnumerable<(string, LocalDate, int)> GetDatesMultiPart(string input)
  {
    var parts = GetDateParts(input).ToArray();
    if (parts.Length == 0) return [];
    var chains = GetDateChains(parts).ToArray();
    if (chains.Length == 0) return [];
    var dates = ParseDateChains(chains).ToArray();
    return dates;
  }

  IEnumerable<IEnumerable<(int Number, DatePartType Type)>> GetDateParts(string input)
  {
    MatchCollection coll = rgxMultipart.Matches(input);
    if (coll.Count > 4 || coll.Count < 2) yield break; // chains must have 2-4 parts
    foreach (Match mtc in rgxMultipart.Matches(input))
    {
      var datePart = GetDatePart(mtc.Value).ToArray();
      if (datePart.Length == 0) yield break; // can never form a valid chain if any date part is empty
      yield return datePart;
    }
  }

  IEnumerable<(int Number, DatePartType Type)> GetDatePart(string piece)
  {
    if (Text.IsMatch(piece))
    {
      foreach (var kvp in MonthKeys.Where(kvp => kvp.Key.Contains(piece, icic)))
        yield return (kvp.Value, DatePartType.Month);
      if (!"day".Contains(piece, icic))
        foreach (var kvp in DaysOfWeek.Where(kvp => kvp.Key.Contains(piece, icic)))
          yield return ((int)kvp.Value, DatePartType.DayOfWeek);
      else if (piece.Equals("d", icic))
        yield return ((int)IsoDayOfWeek.Wednesday, DatePartType.DayOfWeek);
      else if (piece.Equals("a", icic))
        yield return ((int)IsoDayOfWeek.Saturday, DatePartType.DayOfWeek);
    }
    else // will be a number
      if (piece.Length == 1)
    {
      foreach (var kvp in MonthKeys.Where(kvp => kvp.Value.ToString("00").Contains(piece)))
        yield return (kvp.Value, DatePartType.Month);
      yield return (int.Parse(piece), DatePartType.Number);
    }
    else if (piece.Length == 2)
    {
      int num = int.Parse(piece);
      if (num >= 1 && num <= 12) yield return (num, DatePartType.Month);
      yield return ((Today.Year - 39) + (num + 100 - (Today.Year - 39) % 100) % 100, DatePartType.Year);
      yield return (num, DatePartType.Number);
    }
    else if (piece.Length == 3)
    {
      yield return (int.Parse(piece), DatePartType.Number);
    }
    else if (piece.Length == 4)
    {
      yield return (int.Parse(piece), DatePartType.Year);
    }
  }

  IEnumerable<IEnumerable<(int Number, DatePartType Type)>> GetDateChains(IEnumerable<IEnumerable<(int Number, DatePartType Type)>> parts)
  {
    var partList = parts.ToList();
    IEnumerable<IEnumerable<(int Number, DatePartType Type)>> chains = partList.Pop()
      .Select(p => (IEnumerable<(int Number, DatePartType Type)>)[p]);

    while (partList.Count > 0)
    {
      var nextPart = partList.Pop();
      chains = chains.Product(nextPart).Where(t =>
      {
        var types = t.Item1.Select(p => p.Type).Append(t.Item2.Type).ToArray();
        return types.Length == types.Distinct().Count();
      }).Select(t => t.Item1.Append(t.Item2));
    }

    return chains;
  }

  IEnumerable<(string, LocalDate, int)> ParseDateChains(IEnumerable<IEnumerable<(int Number, DatePartType Type)>> chains)
  {
    IEnumerable<(string, LocalDate, int)> suggestions = [];

    foreach (var chain in chains.Select(c => c.OrderBy(i => i.Type).ToArray()))
    {
      DatePartType totalTypes = chain.Select(i => i.Type).Aggregate((l, r) => l | r);

      switch (totalTypes)
      {
        case DatePartType.Year | DatePartType.Month:
          suggestions = suggestions.Concat(ParseYearMonth(chain[0].Number, chain[1].Number));
          break;
        case DatePartType.Month | DatePartType.DayOfWeek:
          suggestions = suggestions.Concat(ParseMonthDow(chain[0].Number, (IsoDayOfWeek)chain[1].Number));
          break;
        case DatePartType.Year | DatePartType.Month | DatePartType.DayOfWeek:
          suggestions = suggestions.Concat(ParseYearMonthDow(chain[0].Number, chain[1].Number, (IsoDayOfWeek)chain[2].Number));
          break;
        case DatePartType.Year | DatePartType.Number:
          suggestions = suggestions.Concat(ParseYearNumber(chain[0].Number, chain[1].Number));
          break;
        case DatePartType.Month | DatePartType.Number:
          suggestions = suggestions.Concat(ParseMonthNumber(chain[0].Number, chain[1].Number));
          break;
        case DatePartType.Year | DatePartType.Month | DatePartType.Number:
          suggestions = suggestions.Concat(ParseYearMonthNumber(chain[0].Number, chain[1].Number, chain[2].Number));
          break;
        case DatePartType.DayOfWeek | DatePartType.Number:
          suggestions = suggestions.Concat(ParseDowNumber((IsoDayOfWeek)chain[0].Number, chain[1].Number));
          break;
        case DatePartType.Year | DatePartType.DayOfWeek | DatePartType.Number:
          suggestions = suggestions.Concat(ParseYearDowNumber(chain[0].Number, (IsoDayOfWeek)chain[1].Number, chain[2].Number));
          break;
        case DatePartType.Month | DatePartType.DayOfWeek | DatePartType.Number:
          suggestions = suggestions.Concat(ParseMonthDowNumber(chain[0].Number, (IsoDayOfWeek)chain[1].Number, chain[2].Number));
          break;
        case DatePartType.Year | DatePartType.Month | DatePartType.DayOfWeek | DatePartType.Number:
          suggestions = suggestions.Concat(ParseYearMonthDowNumber(chain[0].Number, chain[1].Number, (IsoDayOfWeek)chain[2].Number, chain[3].Number));
          break;
      }
    }

    return suggestions;
  }

  IEnumerable<(string, LocalDate, int)> ParseYearMonth(int year, int month)
  {
    yield return DateTuple(d => $"Today in {MonthName.Format(d)} {year}", DateMath.SafeLocalDate(year, month, Today.Day));
  }

  IEnumerable<(string, LocalDate, int)> ParseMonthDow(int month, IsoDayOfWeek dow)
  {
    int previous = DateMath.YearOfPrevious(month);
    int next = DateMath.YearOfNext(month);

    if (next - previous == 2)
      return ParseYearMonthDow(previous, month, dow, "last")
        .Concat(ParseYearMonthDow(Today.Year, month, dow, "current"))
        .Concat(ParseYearMonthDow(next, month, dow, "next"));

    else return ParseYearMonthDow(previous, month, dow, "last")
      .Concat(ParseYearMonthDow(next, month, dow, "next"));
  }

  IEnumerable<(string, LocalDate, int)> ParseYearMonthDow(int year, int month, IsoDayOfWeek dow, string yearOverride = null)
  {
    string template = (yearOverride != null) ? $"{{0}} {dow} of {yearOverride} {MonthNames[month]}"
      : $"{{0}} {dow} of {MonthNames[month]} {year}";

    yield return DateTuple(string.Format(template, "First"), LocalDate.FromYearMonthWeekAndDay(year, month, 1, dow));
    yield return DateTuple(string.Format(template, "Second"), LocalDate.FromYearMonthWeekAndDay(year, month, 2, dow));
    yield return DateTuple(string.Format(template, "Third"), LocalDate.FromYearMonthWeekAndDay(year, month, 3, dow));
    yield return DateTuple(string.Format(template, "Fourth"), LocalDate.FromYearMonthWeekAndDay(year, month, 4, dow));
    yield return DateTuple(string.Format(template, "Fifth"), LocalDate.FromYearMonthWeekAndDay(year, month, 5, dow));
  }

  IEnumerable<(string, LocalDate, int)> ParseYearNumber(int year, int number)
  {
    if (number < 1 || number > 366 || (!CalendarSystem.Gregorian.IsLeapYear(year) && number > 365)) yield break;

    yield return DateTuple($"The {Ordinal(number)} day of {year}", new LocalDate(year, 1, 1).PlusDays(number - 1), 10);
    yield return DateTuple($"The {Ordinal(number)}-to-last day of {year}", new LocalDate(year, 12, 31).PlusDays(-(number - 1)));
  }

  IEnumerable<(string, LocalDate, int)> ParseMonthNumber(int month, int number)
  {
    AnnualDate date = DateMath.SafeAnnualDate(month, number);
    if (date.Day < number) yield break;

    LocalDate prev = DateMath.Previous(date);
    LocalDate next = DateMath.Next(date);

    yield return DateTuple($"Last {MonthNames[month]} {Ordinal(number)}", prev);
    yield return DateTuple($"Next {MonthNames[month]} {Ordinal(number)}", next);
    if (next.Year - prev.Year == 2) yield return DateTuple($"Current {MonthNames[month]} {Ordinal(number)}", Today);
  }

  IEnumerable<(string, LocalDate, int)> ParseYearMonthNumber(int year, int month, int number)
  {
    try
    {
      yield return DateTuple(new LocalDate(year, month, number));
    }
    finally { }
  }

  IEnumerable<(string, LocalDate, int)> ParseDowNumber(IsoDayOfWeek dow, int number)
  {
    if (number == 0) yield break;

    yield return DateTuple($"{number} {dow}{(number != 1 ? "s" : "")} ago", Today.Previous(dow).PlusWeeks(-(number - 1)));
    yield return DateTuple($"{number} {dow}{(number != 1 ? "s" : "")} from now", Today.Next(dow).PlusWeeks(number - 1));
  }

  IEnumerable<(string, LocalDate, int)> ParseYearDowNumber(int year, IsoDayOfWeek dow, int number)
  {
    if (number == 0 || number > 53) yield break;

    LocalDate forward = new LocalDate(year - 1, 12, 24).Next(dow).PlusWeeks(number);
    if (forward.Year == year) yield return DateTuple($"The {Ordinal(number)} {dow} of {year}", forward);

    LocalDate backward = new LocalDate(year + 1, 1, 8).Previous(dow).PlusWeeks(-number);
    if (backward.Year == year) yield return DateTuple($"The {Ordinal(number)}-to-last {dow} of {year}", backward);
  }

  IEnumerable<(string, LocalDate, int)> ParseMonthDowNumber(int month, IsoDayOfWeek dow, int number)
  {
    if (number == 0 || number > 5) return Enumerable.Empty<(string, LocalDate, int)>();

    int previous = DateMath.YearOfPrevious(month);
    int next = DateMath.YearOfNext(month);

    if (next - previous == 2)
      return ParseYearMonthDowNumber(previous, month, dow, number, "last")
        .Concat(ParseYearMonthDowNumber(Today.Year, month, dow, number, "current"))
        .Concat(ParseYearMonthDowNumber(next, month, dow, number, "next"));

    else return ParseYearMonthDowNumber(previous, month, dow, number, "last")
      .Concat(ParseYearMonthDowNumber(next, month, dow, number, "next"));
  }

  IEnumerable<(string, LocalDate, int)> ParseYearMonthDowNumber(int year, int month, IsoDayOfWeek dow, int number,
    string yearOverride = null)
  {
    string ordinal = number switch
    {
      1 => "First",
      2 => "Second",
      3 => "Third",
      4 => "Fourth",
      5 => "Last",
      _ => ""
    };

    string template = (yearOverride != null) ? $"{ordinal} {dow} of {yearOverride} {MonthNames[month]}"
      : $"{ordinal} {dow} of {MonthNames[month]} {year}";

    yield return DateTuple(template, LocalDate.FromYearMonthWeekAndDay(year, month, number, dow));
  }
}

[Flags]
internal enum DatePartType
{
  Year = 1,
  Month = 2,
  DayOfWeek = 4,
  Number = 8
}
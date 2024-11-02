using Nixill.Discord.ShadowBot;
using Nixill.Utils;
using NodaTime;

namespace Nixill;

public static class DateMath
{
  static Instant Now => SystemClock.Instance.GetCurrentInstant();
  static LocalDate Today => Now.InZone(Settings.TimeZone).LocalDateTime.Date;

  // Returns the *year* of the previously-occurring instance of a given
  // Gregorian calendar month.
  public static int YearOfPrevious(int month) => YearOfPrevious(month, Today);
  public static int YearOfPrevious(int month, LocalDate baseDate)
  {
    if (baseDate.Month > month) return baseDate.Year;
    return baseDate.Year - 1;
  }

  public static int YearOfNext(int month) => YearOfNext(month, Today);
  public static int YearOfNext(int month, LocalDate baseDate)
  {
    if (baseDate.Month < month) return baseDate.Year;
    return baseDate.Year + 1;
  }

  public static LocalDate Previous(int month, int day, bool strict = true)
    => Previous(new AnnualDate(month, day), Today, strict);
  public static LocalDate Previous(int month, int day, LocalDate baseDate, bool strict = true)
    => Previous(new AnnualDate(month, day), baseDate, strict);
  public static LocalDate Previous(AnnualDate date, bool strict = true) => Previous(date, Today, strict);
  public static LocalDate Previous(AnnualDate date, LocalDate baseDate, bool strict = true)
  {
    AnnualDate baseAD = new(baseDate.Month, baseDate.Day);
    int startYear = baseDate.Year;
    if (baseAD <= date) startYear -= 1;
    if (strict)
      return date.InYear(EnumerableUtils.ForUntil(startYear, year => date.IsValidYear(year), year => year - 1));
    else return date.InYear(startYear);
  }

  public static LocalDate Next(int month, int day, bool strict = true)
    => Next(new AnnualDate(month, day), Today, strict);
  public static LocalDate Next(int month, int day, LocalDate baseDate, bool strict = true)
    => Next(new AnnualDate(month, day), baseDate, strict);
  public static LocalDate Next(AnnualDate date, bool strict = true) => Next(date, Today, strict);
  public static LocalDate Next(AnnualDate date, LocalDate baseDate, bool strict = true)
  {
    AnnualDate baseAD = new(baseDate.Month, baseDate.Day);
    int startYear = baseDate.Year;
    if (baseAD >= date) startYear += 1;
    if (strict)
      return date.InYear(EnumerableUtils.ForUntil(startYear, year => date.IsValidYear(year), year => year + 1));
    else return date.InYear(startYear);
  }

  public static AnnualDate SafeAnnualDate(int month, int day)
    => Enumerable.Range(1, day).Reverse().SelectUnerrored(i => new AnnualDate(month, i)).First();

  public static LocalDate SafeLocalDate(int year, int month, int day)
    => Enumerable.Range(1, day).Reverse().SelectUnerrored(i => new LocalDate(year, month, i)).First();
}
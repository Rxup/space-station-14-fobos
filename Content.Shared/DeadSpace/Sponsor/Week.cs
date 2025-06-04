using System.Globalization;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Sponsor;

[Serializable, NetSerializable]
public sealed class Week : IComparable<Week>, IEquatable<Week>
{
    #region Constants
    private const DayOfWeek FirstDayOfWeek = DayOfWeek.Monday;
    private const DayOfWeek LastDayOfWeek = DayOfWeek.Sunday;
    private static readonly Calendar Calendar = CultureInfo.InvariantCulture.Calendar;
    #endregion

    #region Constructors
    public Week(DateTime date)
    {
        date = date.ToUniversalTime();
        FirstDateOfWeek = GetFirstDateOfWeek(date);
        LastDateOfWeek = GetLastDateOfWeek(date);
        DateTime offsetDate = GetYearOffsetDayOfWeek(FirstDateOfWeek);
        WeekNumber = Calendar.GetWeekOfYear(offsetDate, CalendarWeekRule.FirstFourDayWeek, FirstDayOfWeek);
        WeekYear = offsetDate.Year;
    }

    public Week(DateOnly date)
        : this(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
    {
    }

    public Week(int weekNumber, int year)
    {
        DateTime firstThursday = GetFirstThursdayOfYear(year);
        DateTime targetThursday = firstThursday.AddDays((weekNumber - 1) * 7);
        FirstDateOfWeek = GetFirstDateOfWeek(targetThursday);
        LastDateOfWeek = GetLastDateOfWeek(FirstDateOfWeek);

        DateTime offsetDate = GetYearOffsetDayOfWeek(FirstDateOfWeek);
        WeekNumber = Calendar.GetWeekOfYear(offsetDate, CalendarWeekRule.FirstFourDayWeek, FirstDayOfWeek);
        WeekYear = offsetDate.Year;
    }
    #endregion

    #region Properties
    public DateTime FirstDateOfWeek { get; private set; }
    public DateTime LastDateOfWeek { get; private set; }
    public int WeekNumber { get; private set; }
    public int WeekYear { get; private set; }
    #endregion

    #region Public Methods
    public long GetFirstDateOfWeekUnixTimestamp()
    {
        DateTime utcStart = FirstDateOfWeek.Date;
        return new DateTimeOffset(utcStart).ToUnixTimeSeconds();
    }

    public long GetLastDateOfWeekUnixTimestamp()
    {
        DateTime utcEnd = LastDateOfWeek.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        return new DateTimeOffset(utcEnd).ToUnixTimeSeconds();
    }

    public string GetFirstDateOfWeekIso8601()
    {
        return FirstDateOfWeek.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
    }

    public string GetLastDateOfWeekIso8601()
    {
        return LastDateOfWeek.ToUniversalTime().Date.AddHours(23).AddMinutes(59).AddSeconds(59).ToString("yyyy-MM-ddTHH:mm:ss");
    }
    #endregion

    #region Interface Implementations
    public int CompareTo(Week? week) => week == null ? -1 : string.Compare(ToString(), week.ToString(), StringComparison.Ordinal);
    public bool Equals(Week? week) => week != null && string.Equals(ToString(), week.ToString(), StringComparison.Ordinal);
    public override string ToString() => $"{WeekYear}-W{WeekNumber:00}";
    public override bool Equals(object? obj) => Equals(obj as Week);
    public override int GetHashCode() => ToString().GetHashCode();
    #endregion

    #region Private Methods
    private DateTime GetFirstDateOfWeek(DateTime date)
    {
        while (date.DayOfWeek != FirstDayOfWeek)
            date = date.AddDays(-1);
        return date.Date;
    }

    private DateTime GetLastDateOfWeek(DateTime date)
    {
        while (date.DayOfWeek != LastDayOfWeek)
            date = date.AddDays(1);
        return date.Date;
    }

    private DateTime GetYearOffsetDayOfWeek(DateTime date)
    {
        while (date.DayOfWeek != DayOfWeek.Thursday)
            date = date.AddDays(1);
        return date.Date;
    }

    private DateTime GetFirstThursdayOfYear(int year)
    {
        DateTime date = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        while (date.DayOfWeek != DayOfWeek.Thursday)
            date = date.AddDays(1);

        // Проверяем, принадлежит ли четверг к первой неделе года
        DateTime weekStart = GetFirstDateOfWeek(date);
        DateTime offsetDate = GetYearOffsetDayOfWeek(weekStart);
        int weekNumber = Calendar.GetWeekOfYear(offsetDate, CalendarWeekRule.FirstFourDayWeek, FirstDayOfWeek);
        if (weekNumber == 1)
            return date;

        // Если нет, берем следующий четверг
        date = date.AddDays(7);
        weekStart = GetFirstDateOfWeek(date);
        offsetDate = GetYearOffsetDayOfWeek(weekStart);
        weekNumber = Calendar.GetWeekOfYear(offsetDate, CalendarWeekRule.FirstFourDayWeek, FirstDayOfWeek);
        if (weekNumber != 1)
            throw new InvalidOperationException("Не удалось найти первый четверг года.");
        return date;
    }
    #endregion
}

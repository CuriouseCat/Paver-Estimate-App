using System.Text.Json.Serialization;

namespace AllAroundEstimates.Models;

public class TimeCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }

    public DayAttendance Monday { get; set; } = new();
    public DayAttendance Tuesday { get; set; } = new();
    public DayAttendance Wednesday { get; set; } = new();
    public DayAttendance Thursday { get; set; } = new();
    public DayAttendance Friday { get; set; } = new();
    public DayAttendance Saturday { get; set; } = new();
    public DayAttendance Sunday { get; set; } = new();

    public DayAttendance GetDay(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => Monday,
        DayOfWeek.Tuesday => Tuesday,
        DayOfWeek.Wednesday => Wednesday,
        DayOfWeek.Thursday => Thursday,
        DayOfWeek.Friday => Friday,
        DayOfWeek.Saturday => Saturday,
        DayOfWeek.Sunday => Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek))
    };

    [JsonIgnore]
    public int DaysPresent =>
        new[] { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }
            .Count(d => d.Status == AttendanceStatus.Present);

    [JsonIgnore]
    public string AttendanceSummaryText => $"{DaysPresent} of 7 days present";

    [JsonIgnore]
    public DateTime WeekEndDate => WeekStartDate.AddDays(6);

    [JsonIgnore]
    public bool IsEditable => DateTime.Now.Date <= WeekEndDate;

    [JsonIgnore]
    public string WeekRangeText => $"Week of {WeekStartDate:MMM d} - {WeekEndDate:MMM d}";
}

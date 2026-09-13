using System.Text.Json.Serialization;

namespace AllAroundEstimates.Models;

public class TimeCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }

    public DayPunch Monday { get; set; } = new();
    public DayPunch Tuesday { get; set; } = new();
    public DayPunch Wednesday { get; set; } = new();
    public DayPunch Thursday { get; set; } = new();
    public DayPunch Friday { get; set; } = new();
    public DayPunch Saturday { get; set; } = new();
    public DayPunch Sunday { get; set; } = new();

    public DayPunch GetDay(DayOfWeek dayOfWeek) => dayOfWeek switch
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
    public decimal TotalHours =>
        Monday.Hours + Tuesday.Hours + Wednesday.Hours + Thursday.Hours +
        Friday.Hours + Saturday.Hours + Sunday.Hours;

    [JsonIgnore]
    public DateTime WeekEndDate => WeekStartDate.AddDays(6);

    [JsonIgnore]
    public bool IsEditable => DateTime.Now.Date <= WeekEndDate;

    [JsonIgnore]
    public string WeekRangeText => $"Week of {WeekStartDate:MMM d} - {WeekEndDate:MMM d}";
}

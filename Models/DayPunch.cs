namespace AllAroundEstimates.Models;

public class DayPunch
{
    public decimal Hours { get; set; }
    public DateTime? ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
}

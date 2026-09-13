namespace AllAroundEstimates.Models;

public enum AttendanceStatus
{
    NotMarked,
    Present,
    Absent
}

public class DayAttendance
{
    public AttendanceStatus Status { get; set; } = AttendanceStatus.NotMarked;
}

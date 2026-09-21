namespace School_Management_System.Options;

public class AttendanceOptions
{
    // A teacher can edit attendance until this many hours after the attendance day ends.
    // Managers can make later corrections, but a correction reason is required and audited.
    public int TeacherEditCutoffHoursAfterDayEnd { get; set; } = 24;

    public decimal LowAttendanceThresholdPercent { get; set; } = 75m;
}

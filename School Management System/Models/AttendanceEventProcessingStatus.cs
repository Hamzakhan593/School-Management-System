namespace School_Management_System.Models;

public enum AttendanceEventProcessingStatus
{
    Pending = 0,
    Recorded = 1,
    AttendanceApplied = 2,
    Duplicate = 3,
    Unmatched = 4,
    NeedsReview = 5,
    Rejected = 6
}

namespace School_Management_System.Models;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Principal = "Principal";
    public const string Admin = "Admin";
    public const string Accountant = "Accountant";
    public const string Teacher = "Teacher";
    public const string ExamController = "ExamController";
    public const string HR = "HR";
    public const string Receptionist = "Receptionist";
    public const string Parent = "Parent";
    public const string Student = "Student";

    public const string UserManagers = SuperAdmin + "," + Principal + "," + Admin;

    public static readonly string[] All =
    [
        SuperAdmin,
        Principal,
        Admin,
        Accountant,
        Teacher,
        ExamController,
        HR,
        Receptionist,
        Parent,
        Student
    ];
}

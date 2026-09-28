using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.ViewModels;

public static class AcademicsSamples
{
    public static readonly School School = new() { Id = 1, Name = "The School of Thoughts", Address = "School campus · Academic office", PrincipalName = "School Principal" };
    public static readonly AcademicSession Session = new() { Id = 1, SchoolId = 1, Name = "2026–2027", Status = AcademicSessionStatus.Active };
    public static readonly SchoolClass Class = new() { Id = 1, Name = "Class 6", IsActive = true, Sections = [new() { Id = 1, Name = "A", Capacity = 30, IsActive = true, SchoolClassId = 1 }, new() { Id = 2, Name = "B", Capacity = 30, IsActive = true, SchoolClassId = 1 }] };
    public static readonly Exam Exam = new() { Id = 1, SchoolId = 1, School = School, AcademicSessionId = 1, AcademicSession = Session, Title = "First Term Examination", Status = ExamStatus.MarksEntryOpen, IsActive = true, ExamClasses = [new() { SchoolClassId = 1, SchoolClass = Class }] };
    public static List<ExamSubject> Subjects => new[] { "English", "Mathematics", "Urdu", "Science", "Islamiat", "Social Studies" }.Select((name, i) => new ExamSubject { Id = i + 1, ExamId = 1, Exam = Exam, SchoolClassId = 1, SchoolClass = Class, Subject = new() { Id = i + 1, Title = name, Code = name[..3], IsActive = true }, MaxMarks = 100, PassMarks = 40 }).ToList();
    public static MarksEntryViewModel Marks() => new() { ExamId = 1, ExamSubjectId = 1, ExamMarksSheetId = 1, SchoolClassId = 1, SectionId = 1, ExamTitle = Exam.Title, SessionName = Session.Name, ClassName = Class.Name, SubjectName = "English", SectionName = "A", ExamStatus = ExamStatus.MarksEntryOpen, CanEdit = true, CanSubmit = true, AvailableSections = Class.Sections.ToList(), MaxMarks = 100, PassMarks = 40, Rows = Enumerable.Range(1, 12).Select(i => new MarkEntryRowViewModel { StudentId = i, StudentEnrollmentId = i, StudentName = i == 1 ? "Muhammad Abdullah Khan" : "Sample student " + i, AdmissionNumber = "ST-" + i.ToString("000"), RollNumber = i.ToString(), TheoryMarks = i < 5 ? 80 + i : null }).ToList() };
    public static ResultCardViewModel Card(bool many = false) => new() { School = School, Exam = Exam, Student = new() { Id = 1, FullName = "Muhammad Abdullah Khan", FatherGuardianName = "Muhammad Imran Khan", AdmissionNumber = "ST-2026-001" }, Enrollment = new() { ClassName = "Class 6", SectionName = "A", RollNumber = "12" }, Result = new() { ObtainedMarks = 510, MaximumMarks = 600, Percentage = 85, Grade = "A+", IsPassed = true, ClassPosition = 2, AttendancePercentage = 96.5m, TeacherRemarks = "A strong performance. Keep practising written expression and problem solving.", PublishedAtUtc = new DateTime(2026, 9, 27), VersionNumber = 1 }, ClassCandidateCount = 60, SchoolCandidateCount = 420, SchoolPosition = 12, SchoolRankingComplete = true, Subjects = Enumerable.Range(0, many ? 45 : 6).Select(i => new ResultCardSubjectRowViewModel { Subject = Subjects[i % 6].Subject.Title + (many ? " advanced module " + (i + 1) : ""), MaximumMarks = 100, PassMarks = 40, ObtainedMarks = 85, Passed = true }).ToList() };
    public static List<Exam> ExamList => [new() { Id = 2, SchoolId = 1, AcademicSession = Session, Title = "Second Term / Pre-Board Assessment", ExamType = "Assessment", Status = ExamStatus.Draft, StartDate = new(2026,10,30), EndDate = new(2026,11,6) }, new() { Id = 3, SchoolId = 1, AcademicSession = Session, Title = "First Term Examination 2026", ExamType = "Term Examination", Status = ExamStatus.Published, StartDate = new(2026,9,15), EndDate = new(2026,9,22), ExamClasses = Enumerable.Range(1,10).Select(i=>new ExamClass { SchoolClassId=i, SchoolClass=new() { Id=i, Name="Class " + i, SortOrder=i } }).ToList() }];
    public static object Model(string module) => module switch {
        "marks" => Marks(),
        "workspace" => new MarksWorkspaceViewModel { ExamId = 1, Exams = [Exam], Subjects = Subjects },
        "classes" => new AcademicStructureIndexViewModel { SelectedSessionId = 1, Sessions = [Session], Classes = [Class], Subjects = Subjects.Select(x => x.Subject).ToList(), Teachers = [new() { Id = "teacher", FullName = "English Teacher" }] },
        "results" => new ResultsIndexViewModel { SelectedSessionId = 1, Sessions = [Session], Exams = ExamList.Where(x=>x.Status == ExamStatus.Published).ToList() },
        "card" => Card(),
        "classresult" => new ClassResultViewModel { Exam = ExamList[1], SchoolClass = Class, Sections = Class.Sections.ToList(), HasPublishedResults = true, CanPublish = true, Rows = Enumerable.Range(1,12).Select(i=>new ClassResultRowViewModel { StudentId=i, StudentName=i==1 ? "Muhammad Abdullah Khan" : "Sample student " + i, AdmissionNumber="ST-"+i.ToString("000"), RollNumber=i.ToString(), ObtainedMarks=510, MaximumMarks=600, Percentage=85, Grade="A+", IsPassed=true, Position=2, AttendancePercentage=96.5m, PublishedVersion=1 }).ToList() },
        "examdetails" => new ExamDetailsViewModel { Exam = new Exam { Id=1, Title=Exam.Title, StartDate=new DateTime(2026,9,15), EndDate=new DateTime(2026,9,22), AcademicSession=Session, Status=ExamStatus.Draft, ExamSubjects=Subjects, ExamClasses=Exam.ExamClasses }, AvailableClasses=[Class] },
        _ => new ExamIndexViewModel { SelectedSessionId = 1, Sessions = [Session], Exams = ExamList }
    };
}
public class PreviewSettings : ISystemSettingsService
{
    public Task<SystemSetting> GetAsync(int schoolId, CancellationToken cancellationToken = default) => Task.FromResult(new SystemSetting { ResultCardShowClassPosition = true, ResultCardShowAttendance = true, ClassTeacherSignatureLabel = "Class Teacher", PrincipalSignatureLabel = "Principal", ResultCardFooterText = "Sample layout - demonstration data only." });
    public Task<SystemSetting> GetTrackedAsync(int schoolId, CancellationToken cancellationToken = default) => GetAsync(schoolId, cancellationToken);
}

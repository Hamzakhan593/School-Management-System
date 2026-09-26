using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace School_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class M19_SettingsMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewValues",
                table: "AuditLogs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValues",
                table: "AuditLogs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BackupRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    BackupType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StorageReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestedByEmail = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetentionUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackupRecords_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BiometricDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ConnectionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastStatusMessage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BiometricDevices_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CameraFaceEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemplateReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrolledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CameraFaceEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CameraFaceEnrollments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CameraFaceEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    AcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    TermId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ExamType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exams_AcademicSessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exams_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Exams_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exams_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseCategories_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeeChallanBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    AcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    BatchKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    BillingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    SchoolClassId = table.Column<int>(type: "int", nullable: true),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    SelectedStudentIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExpectedCount = table.Column<int>(type: "int", nullable: false),
                    GeneratedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeChallanBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeChallanBatches_AcademicSessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeChallanBatches_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeeHeads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DefaultFrequency = table.Column<int>(type: "int", nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeHeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeHeads_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    ReversedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceivedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeePayments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeePayments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialNumberCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    NumberType = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialNumberCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false),
                    SchoolClassId = table.Column<int>(type: "int", nullable: true),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    PublishFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttachmentOriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    AttachmentStorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AttachmentSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notices_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notices_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notices_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notices_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OtherIncomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    IncomeDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancelledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherIncomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherIncomes_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    PeriodMonth = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ValidatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    SourceAcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    TargetAcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CopiedClassSubjects = table.Column<bool>(type: "bit", nullable: false),
                    CopiedFeeStructures = table.Column<bool>(type: "bit", nullable: false),
                    CopiedTeacherAssignments = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreparedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RolledBackAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionBatches_AcademicSessions_SourceAcademicSessionId",
                        column: x => x.SourceAcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionBatches_AcademicSessions_TargetAcademicSessionId",
                        column: x => x.TargetAcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionBatches_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PromotionBatches_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RestoreRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    BackupRecordId = table.Column<long>(type: "bigint", nullable: true),
                    BackupFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    BackupSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SafetyBackupFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestedByEmail = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestoreRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestoreRecords_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryStructures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HouseAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MedicalAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransportAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FixedDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AbsenceDeductionPerDay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HalfDayDeductionPerDay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LateDeductionPerOccurrence = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LeaveDeductionPerDay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryStructures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryStructures_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false),
                    SchoolClassId = table.Column<int>(type: "int", nullable: true),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolDocuments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SchoolDocuments_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolDocuments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolDocuments_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffNumberCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffNumberCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    AcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudentEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    SchoolClassId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    AttendanceDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MarkedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MarkedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAttendances_AcademicSessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAttendances_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAttendances_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAttendances_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAttendances_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAttendances_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    AdmissionNumberPrefix = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    AdmissionNumberDigits = table.Column<int>(type: "int", nullable: false),
                    ChallanNumberPrefix = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    ReceiptNumberPrefix = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    FinancialNumberDigits = table.Column<int>(type: "int", nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    DefaultFeeDueDay = table.Column<int>(type: "int", nullable: false),
                    LateFeeFixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LateFeeGraceDays = table.Column<int>(type: "int", nullable: false),
                    ApplyLateFeeOnCollection = table.Column<bool>(type: "bit", nullable: false),
                    AutoGenerateMonthlyChallans = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyChallanGenerationDay = table.Column<int>(type: "int", nullable: false),
                    TeacherAttendanceEditCutoffHours = table.Column<int>(type: "int", nullable: false),
                    LowAttendanceThresholdPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    AttendanceAllowLate = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceAllowLeave = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceAllowHalfDay = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceAllowNoClass = table.Column<bool>(type: "bit", nullable: false),
                    ResultCardShowAttendance = table.Column<bool>(type: "bit", nullable: false),
                    ResultCardShowClassPosition = table.Column<bool>(type: "bit", nullable: false),
                    ClassTeacherSignatureLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PrincipalSignatureLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ResultCardFooterText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PayslipFooterText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GeneralPrintFooterText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrincipalSignaturePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClassTeacherSignaturePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScheduledBackupsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    BackupHourLocal = table.Column<int>(type: "int", nullable: false),
                    BackupRetentionDays = table.Column<int>(type: "int", nullable: false),
                    AllowInAppRestore = table.Column<bool>(type: "bit", nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    PasswordRequiredLength = table.Column<int>(type: "int", nullable: false),
                    PasswordRequireDigit = table.Column<bool>(type: "bit", nullable: false),
                    PasswordRequireUppercase = table.Column<bool>(type: "bit", nullable: false),
                    PasswordRequireLowercase = table.Column<bool>(type: "bit", nullable: false),
                    PasswordRequireSpecialCharacter = table.Column<bool>(type: "bit", nullable: false),
                    EmailNotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SmsNotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WhatsAppNotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailProviderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SmsProviderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    WhatsAppProviderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    EnableBiometricAttendance = table.Column<bool>(type: "bit", nullable: false),
                    EnableCameraAttendance = table.Column<bool>(type: "bit", nullable: false),
                    EnableParentStudentPortal = table.Column<bool>(type: "bit", nullable: false),
                    EnableOnlinePayments = table.Column<bool>(type: "bit", nullable: false),
                    EnablePushNotifications = table.Column<bool>(type: "bit", nullable: false),
                    SchoolTimeZoneId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SystemSettings_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BiometricEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    BiometricDeviceId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    DeviceUserReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Modality = table.Column<int>(type: "int", nullable: false),
                    TemplateReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrolledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BiometricEnrollments_BiometricDevices_BiometricDeviceId",
                        column: x => x.BiometricDeviceId,
                        principalTable: "BiometricDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BiometricEnrollments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BiometricEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SchoolClassId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamClasses_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamClasses_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamClasses_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SchoolClassId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    MaxMarks = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PassMarks = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TheoryMaxMarks = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    PracticalMaxMarks = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WeightagePercent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSubjects_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSubjects_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSubjects_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    AcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudentEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    ObtainedMarks = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MaximumMarks = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    ClassPosition = table.Column<int>(type: "int", nullable: true),
                    AttendancePercentage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    TeacherRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublishedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentResults_AcademicSessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentResults_AspNetUsers_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentResults_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentResults_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentResults_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    ExpenseCategoryId = table.Column<int>(type: "int", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidTo = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalRequired = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancelledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttachmentOriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    AttachmentStorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AttachmentSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeeChallans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    AcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudentEnrollmentId = table.Column<int>(type: "int", nullable: true),
                    FeeChallanBatchId = table.Column<int>(type: "int", nullable: true),
                    ChallanNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BillingPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillingPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClassNameSnapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SectionNameSnapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LateFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentChargesTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousOutstandingAtIssue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsSuperseded = table.Column<bool>(type: "bit", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeChallans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeChallans_AcademicSessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeChallans_FeeChallanBatches_FeeChallanBatchId",
                        column: x => x.FeeChallanBatchId,
                        principalTable: "FeeChallanBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeChallans_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeChallans_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeChallans_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeeStructures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    AcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    FeeHeadId = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    SchoolClassId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    TermId = table.Column<int>(type: "int", nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChargeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeStructures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeStructures_AcademicSessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeStructures_FeeHeads_FeeHeadId",
                        column: x => x.FeeHeadId,
                        principalTable: "FeeHeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeStructures_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeStructures_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeStructures_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeeStructures_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    FeeHeadId = table.Column<int>(type: "int", nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentDiscounts_FeeHeads_FeeHeadId",
                        column: x => x.FeeHeadId,
                        principalTable: "FeeHeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentDiscounts_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDiscounts_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    NoticeId = table.Column<int>(type: "int", nullable: true),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false),
                    SchoolClassId = table.Column<int>(type: "int", nullable: true),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RecipientCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    StatusDetail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationHistory_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommunicationHistory_Notices_NoticeId",
                        column: x => x.NoticeId,
                        principalTable: "Notices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommunicationHistory_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationHistory_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationHistory_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cnic = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Qualification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JoiningDate = table.Column<DateTime>(type: "date", nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PhotoStorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SalaryStructureId = table.Column<int>(type: "int", nullable: true),
                    ExitDate = table.Column<DateTime>(type: "date", nullable: true),
                    ExitReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staff_SalaryStructures_SalaryStructureId",
                        column: x => x.SalaryStructureId,
                        principalTable: "SalaryStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Staff_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    StudentEnrollmentId = table.Column<int>(type: "int", nullable: true),
                    StudentAttendanceId = table.Column<int>(type: "int", nullable: true),
                    BiometricDeviceId = table.Column<int>(type: "int", nullable: true),
                    ExternalEventId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DeviceUserReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocalDate = table.Column<DateTime>(type: "date", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    DetectedFaceCount = table.Column<int>(type: "int", nullable: true),
                    ReviewReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RawReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ProcessedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceEvents_BiometricDevices_BiometricDeviceId",
                        column: x => x.BiometricDeviceId,
                        principalTable: "BiometricDevices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceEvents_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceEvents_StudentAttendances_StudentAttendanceId",
                        column: x => x.StudentAttendanceId,
                        principalTable: "StudentAttendances",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceEvents_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceEvents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamMarksSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ExamSubjectId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReopenedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReopenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReopenReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamMarksSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamMarksSheets_ExamSubjects_ExamSubjectId",
                        column: x => x.ExamSubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamMarksSheets_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMarksSheets_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMarksSheets_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionBatchId = table.Column<int>(type: "int", nullable: false),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SourceEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    TargetEnrollmentId = table.Column<int>(type: "int", nullable: true),
                    SourceSchoolClassId = table.Column<int>(type: "int", nullable: true),
                    SourceSectionId = table.Column<int>(type: "int", nullable: true),
                    SourceAcademicGroupId = table.Column<int>(type: "int", nullable: true),
                    SourceStudentStatus = table.Column<int>(type: "int", nullable: false),
                    SourceEnrollmentStatus = table.Column<int>(type: "int", nullable: false),
                    ProposedDecision = table.Column<int>(type: "int", nullable: false),
                    FinalDecision = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProposedSchoolClassId = table.Column<int>(type: "int", nullable: true),
                    ProposedSectionId = table.Column<int>(type: "int", nullable: true),
                    ProposedAcademicGroupId = table.Column<int>(type: "int", nullable: true),
                    TargetSchoolClassId = table.Column<int>(type: "int", nullable: true),
                    TargetSectionId = table.Column<int>(type: "int", nullable: true),
                    TargetAcademicGroupId = table.Column<int>(type: "int", nullable: true),
                    LatestStudentResultId = table.Column<int>(type: "int", nullable: true),
                    LatestResultPassed = table.Column<bool>(type: "bit", nullable: true),
                    LatestResultPercentage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionItems_AcademicGroups_ProposedAcademicGroupId",
                        column: x => x.ProposedAcademicGroupId,
                        principalTable: "AcademicGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_AcademicGroups_SourceAcademicGroupId",
                        column: x => x.SourceAcademicGroupId,
                        principalTable: "AcademicGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_AcademicGroups_TargetAcademicGroupId",
                        column: x => x.TargetAcademicGroupId,
                        principalTable: "AcademicGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_PromotionBatches_PromotionBatchId",
                        column: x => x.PromotionBatchId,
                        principalTable: "PromotionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionItems_SchoolClasses_ProposedSchoolClassId",
                        column: x => x.ProposedSchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_SchoolClasses_SourceSchoolClassId",
                        column: x => x.SourceSchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_SchoolClasses_TargetSchoolClassId",
                        column: x => x.TargetSchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Sections_ProposedSectionId",
                        column: x => x.ProposedSectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Sections_SourceSectionId",
                        column: x => x.SourceSectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Sections_TargetSectionId",
                        column: x => x.TargetSectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_StudentEnrollments_SourceEnrollmentId",
                        column: x => x.SourceEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_StudentEnrollments_TargetEnrollmentId",
                        column: x => x.TargetEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PromotionItems_StudentResults_LatestStudentResultId",
                        column: x => x.LatestStudentResultId,
                        principalTable: "StudentResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeeChallanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeeChallanId = table.Column<int>(type: "int", nullable: false),
                    FeeHeadId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeChallanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeChallanItems_FeeChallans_FeeChallanId",
                        column: x => x.FeeChallanId,
                        principalTable: "FeeChallans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeeChallanItems_FeeHeads_FeeHeadId",
                        column: x => x.FeeHeadId,
                        principalTable: "FeeHeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PayrollItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    PayrollRunId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    SalaryStructureId = table.Column<int>(type: "int", nullable: true),
                    EmployeeIdSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StaffNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DepartmentSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FixedAllowances = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ManualAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PresentDays = table.Column<int>(type: "int", nullable: false),
                    AbsentDays = table.Column<int>(type: "int", nullable: false),
                    LateDays = table.Column<int>(type: "int", nullable: false),
                    LeaveDays = table.Column<int>(type: "int", nullable: false),
                    HalfDays = table.Column<int>(type: "int", nullable: false),
                    AttendanceDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FixedDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdvanceDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ManualDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ManualAdjustmentNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidationMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollItems_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollItems_SalaryStructures_SalaryStructureId",
                        column: x => x.SalaryStructureId,
                        principalTable: "SalaryStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollItems_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffAdvances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyInstallment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SettledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAdvances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAdvances_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffAdvances_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    CheckInUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MarkedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAttendances_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffAttendances_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffBiometricEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    BiometricDeviceId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    DeviceUserReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Modality = table.Column<int>(type: "int", nullable: false),
                    TemplateReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrolledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffBiometricEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffBiometricEnrollments_BiometricDevices_BiometricDeviceId",
                        column: x => x.BiometricDeviceId,
                        principalTable: "BiometricDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffBiometricEnrollments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffBiometricEnrollments_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffDocuments_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentMarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ExamSubjectId = table.Column<int>(type: "int", nullable: false),
                    ExamMarksSheetId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudentEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    SpecialStatus = table.Column<int>(type: "int", nullable: false),
                    TheoryMarks = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    PracticalMarks = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    ObtainedMarks = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    TeacherRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnteredByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EnteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentMarks_ExamMarksSheets_ExamMarksSheetId",
                        column: x => x.ExamMarksSheetId,
                        principalTable: "ExamMarksSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentMarks_ExamSubjects_ExamSubjectId",
                        column: x => x.ExamSubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMarks_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMarks_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMarks_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMarks_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeePaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeePaymentId = table.Column<int>(type: "int", nullable: false),
                    FeeChallanId = table.Column<int>(type: "int", nullable: false),
                    FeeChallanItemId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeePaymentAllocations_FeeChallanItems_FeeChallanItemId",
                        column: x => x.FeeChallanItemId,
                        principalTable: "FeeChallanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FeePaymentAllocations_FeeChallans_FeeChallanId",
                        column: x => x.FeeChallanId,
                        principalTable: "FeeChallans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeePaymentAllocations_FeePayments_FeePaymentId",
                        column: x => x.FeePaymentId,
                        principalTable: "FeePayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAdvanceDeductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemId = table.Column<int>(type: "int", nullable: false),
                    StaffAdvanceId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdvanceDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceDeductions_PayrollItems_PayrollItemId",
                        column: x => x.PayrollItemId,
                        principalTable: "PayrollItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceDeductions_StaffAdvances_StaffAdvanceId",
                        column: x => x.StaffAdvanceId,
                        principalTable: "StaffAdvances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffAttendanceEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: true),
                    StaffAttendanceId = table.Column<int>(type: "int", nullable: true),
                    BiometricDeviceId = table.Column<int>(type: "int", nullable: true),
                    ExternalEventId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DeviceUserReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocalDate = table.Column<DateTime>(type: "date", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ReviewReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RawReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAttendanceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAttendanceEvents_BiometricDevices_BiometricDeviceId",
                        column: x => x.BiometricDeviceId,
                        principalTable: "BiometricDevices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffAttendanceEvents_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffAttendanceEvents_StaffAttendances_StaffAttendanceId",
                        column: x => x.StaffAttendanceId,
                        principalTable: "StaffAttendances",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffAttendanceEvents_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SchoolId_CreatedAtUtc",
                table: "AuditLogs",
                columns: new[] { "SchoolId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StaffId",
                table: "AspNetUsers",
                column: "StaffId",
                unique: true,
                filter: "[StaffId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_BiometricDeviceId_ExternalEventId",
                table: "AttendanceEvents",
                columns: new[] { "BiometricDeviceId", "ExternalEventId" },
                unique: true,
                filter: "[BiometricDeviceId] IS NOT NULL AND [ExternalEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_SchoolId_LocalDate_ProcessingStatus",
                table: "AttendanceEvents",
                columns: new[] { "SchoolId", "LocalDate", "ProcessingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_SchoolId_StudentId_OccurredAtUtc",
                table: "AttendanceEvents",
                columns: new[] { "SchoolId", "StudentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_StudentAttendanceId",
                table: "AttendanceEvents",
                column: "StudentAttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_StudentEnrollmentId",
                table: "AttendanceEvents",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_StudentId",
                table: "AttendanceEvents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_FileName",
                table: "BackupRecords",
                column: "FileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_SchoolId_StartedAtUtc",
                table: "BackupRecords",
                columns: new[] { "SchoolId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_SchoolId_Status",
                table: "BackupRecords",
                columns: new[] { "SchoolId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_DeviceCode",
                table: "BiometricDevices",
                column: "DeviceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_SchoolId_IsActive",
                table: "BiometricDevices",
                columns: new[] { "SchoolId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricEnrollments_BiometricDeviceId_DeviceUserReference",
                table: "BiometricEnrollments",
                columns: new[] { "BiometricDeviceId", "DeviceUserReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BiometricEnrollments_SchoolId_StudentId_IsActive",
                table: "BiometricEnrollments",
                columns: new[] { "SchoolId", "StudentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricEnrollments_StudentId",
                table: "BiometricEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEnrollments_SchoolId_IsActive",
                table: "CameraFaceEnrollments",
                columns: new[] { "SchoolId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEnrollments_SchoolId_StudentId_ProviderName",
                table: "CameraFaceEnrollments",
                columns: new[] { "SchoolId", "StudentId", "ProviderName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEnrollments_StudentId",
                table: "CameraFaceEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationHistory_CreatedByUserId",
                table: "CommunicationHistory",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationHistory_NoticeId",
                table: "CommunicationHistory",
                column: "NoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationHistory_SchoolClassId",
                table: "CommunicationHistory",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationHistory_SchoolId_Channel_Status",
                table: "CommunicationHistory",
                columns: new[] { "SchoolId", "Channel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationHistory_SchoolId_CreatedAtUtc",
                table: "CommunicationHistory",
                columns: new[] { "SchoolId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationHistory_SectionId",
                table: "CommunicationHistory",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamClasses_ExamId_SchoolClassId",
                table: "ExamClasses",
                columns: new[] { "ExamId", "SchoolClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamClasses_SchoolClassId",
                table: "ExamClasses",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamClasses_SchoolId",
                table: "ExamClasses",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarksSheets_ExamId",
                table: "ExamMarksSheets",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarksSheets_ExamSubjectId_SectionId",
                table: "ExamMarksSheets",
                columns: new[] { "ExamSubjectId", "SectionId" },
                unique: true,
                filter: "[SectionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarksSheets_SchoolId_ExamId_Status",
                table: "ExamMarksSheets",
                columns: new[] { "SchoolId", "ExamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarksSheets_SectionId",
                table: "ExamMarksSheets",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_AcademicSessionId",
                table: "Exams",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CreatedByUserId",
                table: "Exams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_SchoolId_AcademicSessionId_StartDate",
                table: "Exams",
                columns: new[] { "SchoolId", "AcademicSessionId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_SchoolId_AcademicSessionId_Title",
                table: "Exams",
                columns: new[] { "SchoolId", "AcademicSessionId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_TermId",
                table: "Exams",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjects_ExamId_SchoolClassId_SubjectId",
                table: "ExamSubjects",
                columns: new[] { "ExamId", "SchoolClassId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjects_SchoolClassId",
                table: "ExamSubjects",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjects_SchoolId_ExamId_IsActive",
                table: "ExamSubjects",
                columns: new[] { "SchoolId", "ExamId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjects_SubjectId",
                table: "ExamSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_SchoolId_IsActive_SortOrder",
                table: "ExpenseCategories",
                columns: new[] { "SchoolId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_SchoolId_Name",
                table: "ExpenseCategories",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseCategoryId",
                table: "Expenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SchoolId_ApprovalStatus_IsCancelled",
                table: "Expenses",
                columns: new[] { "SchoolId", "ApprovalStatus", "IsCancelled" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SchoolId_ExpenseCategoryId_ExpenseDate",
                table: "Expenses",
                columns: new[] { "SchoolId", "ExpenseCategoryId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SchoolId_ExpenseDate",
                table: "Expenses",
                columns: new[] { "SchoolId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallanBatches_AcademicSessionId",
                table: "FeeChallanBatches",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallanBatches_SchoolId_BatchKey",
                table: "FeeChallanBatches",
                columns: new[] { "SchoolId", "BatchKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallanBatches_SchoolId_BillingPeriod",
                table: "FeeChallanBatches",
                columns: new[] { "SchoolId", "BillingPeriod" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallanItems_FeeChallanId",
                table: "FeeChallanItems",
                column: "FeeChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallanItems_FeeHeadId",
                table: "FeeChallanItems",
                column: "FeeHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallans_AcademicSessionId",
                table: "FeeChallans",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallans_FeeChallanBatchId",
                table: "FeeChallans",
                column: "FeeChallanBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallans_SchoolId_ChallanNumber",
                table: "FeeChallans",
                columns: new[] { "SchoolId", "ChallanNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallans_SchoolId_DueDate_Status",
                table: "FeeChallans",
                columns: new[] { "SchoolId", "DueDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallans_SchoolId_StudentId_BillingPeriod",
                table: "FeeChallans",
                columns: new[] { "SchoolId", "StudentId", "BillingPeriod" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallans_StudentEnrollmentId",
                table: "FeeChallans",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeChallans_StudentId",
                table: "FeeChallans",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeHeads_SchoolId_Code",
                table: "FeeHeads",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeHeads_SchoolId_Name",
                table: "FeeHeads",
                columns: new[] { "SchoolId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_FeePaymentAllocations_FeeChallanId",
                table: "FeePaymentAllocations",
                column: "FeeChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePaymentAllocations_FeeChallanItemId",
                table: "FeePaymentAllocations",
                column: "FeeChallanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePaymentAllocations_FeePaymentId_FeeChallanId_FeeChallanItemId",
                table: "FeePaymentAllocations",
                columns: new[] { "FeePaymentId", "FeeChallanId", "FeeChallanItemId" },
                unique: true,
                filter: "[FeeChallanItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_SchoolId_PaymentDateUtc",
                table: "FeePayments",
                columns: new[] { "SchoolId", "PaymentDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_SchoolId_ReceiptNumber",
                table: "FeePayments",
                columns: new[] { "SchoolId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_SchoolId_StudentId_IsReversed",
                table: "FeePayments",
                columns: new[] { "SchoolId", "StudentId", "IsReversed" });

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_StudentId",
                table: "FeePayments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_AcademicSessionId_SchoolClassId_FeeHeadId",
                table: "FeeStructures",
                columns: new[] { "AcademicSessionId", "SchoolClassId", "FeeHeadId" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_AcademicSessionId_StudentId_FeeHeadId",
                table: "FeeStructures",
                columns: new[] { "AcademicSessionId", "StudentId", "FeeHeadId" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_FeeHeadId",
                table: "FeeStructures",
                column: "FeeHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_SchoolClassId",
                table: "FeeStructures",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_SchoolId_AcademicSessionId_Scope_IsActive",
                table: "FeeStructures",
                columns: new[] { "SchoolId", "AcademicSessionId", "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_StudentId",
                table: "FeeStructures",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_TermId",
                table: "FeeStructures",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialNumberCounters_SchoolId_NumberType_Year",
                table: "FinancialNumberCounters",
                columns: new[] { "SchoolId", "NumberType", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notices_CreatedByUserId",
                table: "Notices",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_SchoolClassId",
                table: "Notices",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_SchoolId_Audience_SchoolClassId_SectionId",
                table: "Notices",
                columns: new[] { "SchoolId", "Audience", "SchoolClassId", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notices_SchoolId_IsPublished_IsArchived_PublishFromUtc",
                table: "Notices",
                columns: new[] { "SchoolId", "IsPublished", "IsArchived", "PublishFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notices_SectionId",
                table: "Notices",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_OtherIncomes_SchoolId_IncomeDate",
                table: "OtherIncomes",
                columns: new[] { "SchoolId", "IncomeDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OtherIncomes_SchoolId_IsCancelled",
                table: "OtherIncomes",
                columns: new[] { "SchoolId", "IsCancelled" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceDeductions_PayrollItemId_StaffAdvanceId",
                table: "PayrollAdvanceDeductions",
                columns: new[] { "PayrollItemId", "StaffAdvanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceDeductions_StaffAdvanceId",
                table: "PayrollAdvanceDeductions",
                column: "StaffAdvanceId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItems_PayrollRunId_StaffId",
                table: "PayrollItems",
                columns: new[] { "PayrollRunId", "StaffId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItems_SalaryStructureId",
                table: "PayrollItems",
                column: "SalaryStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItems_SchoolId_StaffId",
                table: "PayrollItems",
                columns: new[] { "SchoolId", "StaffId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItems_StaffId",
                table: "PayrollItems",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_SchoolId_PeriodYear_PeriodMonth",
                table: "PayrollRuns",
                columns: new[] { "SchoolId", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_SchoolId_Status_PeriodYear_PeriodMonth",
                table: "PayrollRuns",
                columns: new[] { "SchoolId", "Status", "PeriodYear", "PeriodMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionBatches_CreatedByUserId",
                table: "PromotionBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionBatches_SchoolId_SourceAcademicSessionId",
                table: "PromotionBatches",
                columns: new[] { "SchoolId", "SourceAcademicSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionBatches_SchoolId_Status",
                table: "PromotionBatches",
                columns: new[] { "SchoolId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionBatches_SchoolId_TargetAcademicSessionId",
                table: "PromotionBatches",
                columns: new[] { "SchoolId", "TargetAcademicSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionBatches_SourceAcademicSessionId",
                table: "PromotionBatches",
                column: "SourceAcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionBatches_TargetAcademicSessionId",
                table: "PromotionBatches",
                column: "TargetAcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_LatestStudentResultId",
                table: "PromotionItems",
                column: "LatestStudentResultId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_PromotionBatchId_StudentId",
                table: "PromotionItems",
                columns: new[] { "PromotionBatchId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_ProposedAcademicGroupId",
                table: "PromotionItems",
                column: "ProposedAcademicGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_ProposedSchoolClassId",
                table: "PromotionItems",
                column: "ProposedSchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_ProposedSectionId",
                table: "PromotionItems",
                column: "ProposedSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_SchoolId_Status",
                table: "PromotionItems",
                columns: new[] { "SchoolId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_SourceAcademicGroupId",
                table: "PromotionItems",
                column: "SourceAcademicGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_SourceEnrollmentId",
                table: "PromotionItems",
                column: "SourceEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_SourceSchoolClassId",
                table: "PromotionItems",
                column: "SourceSchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_SourceSectionId",
                table: "PromotionItems",
                column: "SourceSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_StudentId",
                table: "PromotionItems",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_TargetAcademicGroupId",
                table: "PromotionItems",
                column: "TargetAcademicGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_TargetEnrollmentId",
                table: "PromotionItems",
                column: "TargetEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_TargetSchoolClassId",
                table: "PromotionItems",
                column: "TargetSchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_TargetSectionId",
                table: "PromotionItems",
                column: "TargetSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RestoreRecords_SchoolId_StartedAtUtc",
                table: "RestoreRecords",
                columns: new[] { "SchoolId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructures_SchoolId_IsActive_EffectiveFrom",
                table: "SalaryStructures",
                columns: new[] { "SchoolId", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolDocuments_SchoolClassId",
                table: "SchoolDocuments",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolDocuments_SchoolId_Audience_SchoolClassId_SectionId",
                table: "SchoolDocuments",
                columns: new[] { "SchoolId", "Audience", "SchoolClassId", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolDocuments_SchoolId_IsActive_Category",
                table: "SchoolDocuments",
                columns: new[] { "SchoolId", "IsActive", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolDocuments_SectionId",
                table: "SchoolDocuments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolDocuments_UploadedByUserId",
                table: "SchoolDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_SalaryStructureId",
                table: "Staff",
                column: "SalaryStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_SchoolId_Cnic",
                table: "Staff",
                columns: new[] { "SchoolId", "Cnic" },
                unique: true,
                filter: "[Cnic] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_SchoolId_EmployeeId",
                table: "Staff",
                columns: new[] { "SchoolId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_SchoolId_Status_Department",
                table: "Staff",
                columns: new[] { "SchoolId", "Status", "Department" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffAdvances_SchoolId_StaffId_Status",
                table: "StaffAdvances",
                columns: new[] { "SchoolId", "StaffId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffAdvances_StaffId",
                table: "StaffAdvances",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendanceEvents_BiometricDeviceId_ExternalEventId",
                table: "StaffAttendanceEvents",
                columns: new[] { "BiometricDeviceId", "ExternalEventId" },
                unique: true,
                filter: "[BiometricDeviceId] IS NOT NULL AND [ExternalEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendanceEvents_SchoolId_StaffId_OccurredAtUtc",
                table: "StaffAttendanceEvents",
                columns: new[] { "SchoolId", "StaffId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendanceEvents_StaffAttendanceId",
                table: "StaffAttendanceEvents",
                column: "StaffAttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendanceEvents_StaffId",
                table: "StaffAttendanceEvents",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_SchoolId_AttendanceDate_Status",
                table: "StaffAttendances",
                columns: new[] { "SchoolId", "AttendanceDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_SchoolId_StaffId_AttendanceDate",
                table: "StaffAttendances",
                columns: new[] { "SchoolId", "StaffId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_StaffId",
                table: "StaffAttendances",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBiometricEnrollments_BiometricDeviceId_DeviceUserReference",
                table: "StaffBiometricEnrollments",
                columns: new[] { "BiometricDeviceId", "DeviceUserReference" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBiometricEnrollments_BiometricDeviceId_StaffId",
                table: "StaffBiometricEnrollments",
                columns: new[] { "BiometricDeviceId", "StaffId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBiometricEnrollments_SchoolId",
                table: "StaffBiometricEnrollments",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBiometricEnrollments_StaffId",
                table: "StaffBiometricEnrollments",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDocuments_StaffId",
                table: "StaffDocuments",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffNumberCounters_SchoolId_Year",
                table: "StaffNumberCounters",
                columns: new[] { "SchoolId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_AcademicSessionId",
                table: "StudentAttendances",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_SchoolClassId",
                table: "StudentAttendances",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_SchoolId_AcademicSessionId_SchoolClassId_SectionId_AttendanceDate",
                table: "StudentAttendances",
                columns: new[] { "SchoolId", "AcademicSessionId", "SchoolClassId", "SectionId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_SchoolId_AcademicSessionId_StudentId_AttendanceDate",
                table: "StudentAttendances",
                columns: new[] { "SchoolId", "AcademicSessionId", "StudentId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_SchoolId_AttendanceDate_Status",
                table: "StudentAttendances",
                columns: new[] { "SchoolId", "AttendanceDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_SectionId",
                table: "StudentAttendances",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_StudentEnrollmentId",
                table: "StudentAttendances",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_StudentId",
                table: "StudentAttendances",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDiscounts_FeeHeadId",
                table: "StudentDiscounts",
                column: "FeeHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDiscounts_SchoolId_StudentId_IsActive",
                table: "StudentDiscounts",
                columns: new[] { "SchoolId", "StudentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentDiscounts_StudentId",
                table: "StudentDiscounts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_ExamId",
                table: "StudentMarks",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_ExamMarksSheetId",
                table: "StudentMarks",
                column: "ExamMarksSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_ExamSubjectId_StudentId",
                table: "StudentMarks",
                columns: new[] { "ExamSubjectId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_SchoolId_ExamId_StudentId",
                table: "StudentMarks",
                columns: new[] { "SchoolId", "ExamId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_StudentEnrollmentId",
                table: "StudentMarks",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_StudentId",
                table: "StudentMarks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_AcademicSessionId",
                table: "StudentResults",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_ExamId_StudentId_VersionNumber",
                table: "StudentResults",
                columns: new[] { "ExamId", "StudentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_PublishedByUserId",
                table: "StudentResults",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_SchoolId_AcademicSessionId_ExamId_IsCurrent",
                table: "StudentResults",
                columns: new[] { "SchoolId", "AcademicSessionId", "ExamId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_StudentEnrollmentId",
                table: "StudentResults",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_StudentId",
                table: "StudentResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UX_StudentResult_Current",
                table: "StudentResults",
                columns: new[] { "ExamId", "StudentId" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_SchoolId",
                table: "SystemSettings",
                column: "SchoolId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedByUserId",
                table: "SystemSettings",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Staff_StaffId",
                table: "AspNetUsers",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Staff_StaffId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "AttendanceEvents");

            migrationBuilder.DropTable(
                name: "BackupRecords");

            migrationBuilder.DropTable(
                name: "BiometricEnrollments");

            migrationBuilder.DropTable(
                name: "CameraFaceEnrollments");

            migrationBuilder.DropTable(
                name: "CommunicationHistory");

            migrationBuilder.DropTable(
                name: "ExamClasses");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "FeePaymentAllocations");

            migrationBuilder.DropTable(
                name: "FeeStructures");

            migrationBuilder.DropTable(
                name: "FinancialNumberCounters");

            migrationBuilder.DropTable(
                name: "OtherIncomes");

            migrationBuilder.DropTable(
                name: "PayrollAdvanceDeductions");

            migrationBuilder.DropTable(
                name: "PromotionItems");

            migrationBuilder.DropTable(
                name: "RestoreRecords");

            migrationBuilder.DropTable(
                name: "SchoolDocuments");

            migrationBuilder.DropTable(
                name: "StaffAttendanceEvents");

            migrationBuilder.DropTable(
                name: "StaffBiometricEnrollments");

            migrationBuilder.DropTable(
                name: "StaffDocuments");

            migrationBuilder.DropTable(
                name: "StaffNumberCounters");

            migrationBuilder.DropTable(
                name: "StudentDiscounts");

            migrationBuilder.DropTable(
                name: "StudentMarks");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "StudentAttendances");

            migrationBuilder.DropTable(
                name: "Notices");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "FeeChallanItems");

            migrationBuilder.DropTable(
                name: "FeePayments");

            migrationBuilder.DropTable(
                name: "PayrollItems");

            migrationBuilder.DropTable(
                name: "StaffAdvances");

            migrationBuilder.DropTable(
                name: "PromotionBatches");

            migrationBuilder.DropTable(
                name: "StudentResults");

            migrationBuilder.DropTable(
                name: "StaffAttendances");

            migrationBuilder.DropTable(
                name: "BiometricDevices");

            migrationBuilder.DropTable(
                name: "ExamMarksSheets");

            migrationBuilder.DropTable(
                name: "FeeChallans");

            migrationBuilder.DropTable(
                name: "FeeHeads");

            migrationBuilder.DropTable(
                name: "PayrollRuns");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "ExamSubjects");

            migrationBuilder.DropTable(
                name: "FeeChallanBatches");

            migrationBuilder.DropTable(
                name: "SalaryStructures");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_SchoolId_CreatedAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_StaffId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NewValues",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OldValues",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AuditLogs");
        }
    }
}

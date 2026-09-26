using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public class PromotionService : IPromotionService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public PromotionService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PromotionIndexViewModel> GetIndexAsync(int schoolId)
    {
        var sessions = await _db.AcademicSessions
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        var batches = await _db.PromotionBatches
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PromotionBatchListItemViewModel
            {
                Id = x.Id,
                SourceSession = x.SourceAcademicSession.Name,
                TargetSession = x.TargetAcademicSession.Name,
                Status = x.Status,
                StudentCount = x.Items.Count,
                HoldCount = x.Items.Count(i => (i.FinalDecision ?? i.ProposedDecision) == PromotionDecision.Hold),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return new PromotionIndexViewModel
        {
            Sessions = sessions,
            Batches = batches
        };
    }

    public async Task<RolloverCreateViewModel> BuildCreateModelAsync(int schoolId, int? sourceSessionId = null, RolloverCreateViewModel? current = null)
    {
        var sessions = await _db.AcademicSessions
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId && (x.Status == AcademicSessionStatus.Active || x.Status == AcademicSessionStatus.Closed))
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        var source = sourceSessionId.HasValue
            ? sessions.FirstOrDefault(x => x.Id == sourceSessionId.Value)
            : sessions.FirstOrDefault(x => x.Status == AcademicSessionStatus.Active) ?? sessions.FirstOrDefault();

        var model = current ?? new RolloverCreateViewModel();
        if (model.SourceAcademicSessionId == 0 && source is not null)
        {
            model.SourceAcademicSessionId = source.Id;
        }

        source = sessions.FirstOrDefault(x => x.Id == model.SourceAcademicSessionId) ?? source;

        if (current is null && source is not null)
        {
            var nextStart = source.EndDate.Date.AddDays(1);
            var lengthDays = Math.Max(1, (source.EndDate.Date - source.StartDate.Date).Days);
            model.TargetStartDate = nextStart;
            model.TargetEndDate = nextStart.AddDays(lengthDays);
            model.WorkingDaysPerWeek = source.WorkingDaysPerWeek;
            model.TargetSessionName = SuggestNextSessionName(source.Name, source.StartDate, source.EndDate);
        }

        model.SourceSessions = sessions.Select(x => new AcademicSessionOptionViewModel
        {
            Id = x.Id,
            Name = x.Name,
            Status = x.Status
        }).ToList();

        if (source is not null)
        {
            model.TeacherAssignments = await _db.TeacherAssignments
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == source.Id && x.IsActive)
                .OrderBy(x => x.SchoolClass.SortOrder)
                .ThenBy(x => x.Section == null ? "" : x.Section.Name)
                .ThenBy(x => x.Subject.Title)
                .Select(x => new TeacherAssignmentOptionViewModel
                {
                    Id = x.Id,
                    TeacherName = x.TeacherUser.FullName,
                    ClassName = x.SchoolClass.Name,
                    SectionName = x.Section != null ? x.Section.Name : null,
                    SubjectName = x.Subject.Title
                })
                .ToListAsync();
        }

        return model;
    }

    public async Task<(bool Success, string Message, int? BatchId)> CreateRolloverAsync(int schoolId, string? userId, RolloverCreateViewModel model)
    {
        var source = await _db.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.SourceAcademicSessionId && x.SchoolId == schoolId);

        if (source is null)
            return (false, "Source academic session was not found.", null);
        if (source.Status != AcademicSessionStatus.Active && source.Status != AcademicSessionStatus.Closed)
            return (false, "Only an Active or Closed academic session can be rolled over.", null);
        if (string.IsNullOrWhiteSpace(model.TargetSessionName))
            return (false, "Next academic session name is required.", null);
        if (model.TargetEndDate.Date <= model.TargetStartDate.Date)
            return (false, "Next session end date must be after its start date.", null);
        if (model.TargetStartDate.Date <= source.EndDate.Date)
            return (false, "The next session must start after the source session ends.", null);

        var duplicateName = await _db.AcademicSessions.AnyAsync(x =>
            x.SchoolId == schoolId && x.Name == model.TargetSessionName.Trim());
        if (duplicateName)
            return (false, "An academic session with this name already exists.", null);

        var existingRollover = await _db.PromotionBatches.AnyAsync(x =>
            x.SchoolId == schoolId && x.SourceAcademicSessionId == source.Id);
        if (existingRollover)
            return (false, "A rollover already exists for this source session. Open the existing rollover instead of creating a duplicate.", null);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var target = new AcademicSession
            {
                SchoolId = schoolId,
                Name = model.TargetSessionName.Trim(),
                StartDate = model.TargetStartDate.Date,
                EndDate = model.TargetEndDate.Date,
                WorkingDaysPerWeek = model.WorkingDaysPerWeek,
                Status = AcademicSessionStatus.Draft,
                Notes = $"Created through annual rollover from {source.Name}.",
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.AcademicSessions.Add(target);
            await _db.SaveChangesAsync();

            await CopySessionFoundationAsync(source.Id, target);

            if (model.CopyClassSubjects)
            {
                var mappings = await _db.ClassSubjects.AsNoTracking()
                    .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == source.Id && x.IsActive)
                    .ToListAsync();
                foreach (var x in mappings)
                {
                    _db.ClassSubjects.Add(new ClassSubject
                    {
                        SchoolId = schoolId,
                        AcademicSessionId = target.Id,
                        SchoolClassId = x.SchoolClassId,
                        SubjectId = x.SubjectId,
                        MaxMarks = x.MaxMarks,
                        PassMarks = x.PassMarks,
                        IsActive = x.IsActive,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            if (model.CopyFeeStructures)
            {
                var targetTerms = await _db.Terms.AsNoTracking()
                    .Where(x => x.AcademicSessionId == target.Id)
                    .ToDictionaryAsync(x => x.Name, x => x.Id);

                var sourceTerms = await _db.Terms.AsNoTracking()
                    .Where(x => x.AcademicSessionId == source.Id)
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

                var feeStructures = await _db.FeeStructures.AsNoTracking()
                    .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == source.Id && x.IsActive)
                    .ToListAsync();

                foreach (var x in feeStructures)
                {
                    int? targetTermId = null;
                    if (x.TermId.HasValue && sourceTerms.TryGetValue(x.TermId.Value, out var termName) && targetTerms.TryGetValue(termName, out var mappedTermId))
                        targetTermId = mappedTermId;

                    _db.FeeStructures.Add(new FeeStructure
                    {
                        SchoolId = schoolId,
                        AcademicSessionId = target.Id,
                        FeeHeadId = x.FeeHeadId,
                        Scope = x.Scope,
                        SchoolClassId = x.SchoolClassId,
                        StudentId = x.StudentId,
                        TermId = targetTermId,
                        Frequency = x.Frequency,
                        Amount = x.Amount,
                        EffectiveFrom = ShiftDateToTarget(x.EffectiveFrom, source.StartDate, target.StartDate),
                        EffectiveTo = ShiftDateToTarget(x.EffectiveTo, source.StartDate, target.StartDate),
                        ChargeDate = ShiftDateToTarget(x.ChargeDate, source.StartDate, target.StartDate),
                        Notes = x.Notes,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            var selectedAssignmentIds = (model.SelectedTeacherAssignmentIds ?? new List<int>()).Distinct().ToList();
            if (selectedAssignmentIds.Count > 0)
            {
                var assignments = await _db.TeacherAssignments.AsNoTracking()
                    .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == source.Id && selectedAssignmentIds.Contains(x.Id) && x.IsActive)
                    .ToListAsync();
                foreach (var x in assignments)
                {
                    _db.TeacherAssignments.Add(new TeacherAssignment
                    {
                        SchoolId = schoolId,
                        AcademicSessionId = target.Id,
                        SchoolClassId = x.SchoolClassId,
                        SectionId = x.SectionId,
                        SubjectId = x.SubjectId,
                        TeacherUserId = x.TeacherUserId,
                        Notes = x.Notes,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            var batch = new PromotionBatch
            {
                SchoolId = schoolId,
                SourceAcademicSessionId = source.Id,
                TargetAcademicSessionId = target.Id,
                Status = PromotionBatchStatus.Draft,
                CopiedClassSubjects = model.CopyClassSubjects,
                CopiedFeeStructures = model.CopyFeeStructures,
                CopiedTeacherAssignments = selectedAssignmentIds.Count > 0,
                CreatedByUserId = userId,
                Notes = model.Notes,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.PromotionBatches.Add(batch);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _audit.WriteAsync(
                "SessionRollover.Created",
                "PromotionBatch",
                batch.Id.ToString(),
                $"{source.Name} → {target.Name}; class-subjects={model.CopyClassSubjects}; fees={model.CopyFeeStructures}; teacher assignments={selectedAssignmentIds.Count}.");

            return (true, "Next session and reusable setup created. Review the proposed student promotion before committing.", batch.Id);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, $"Rollover could not be created: {ex.GetBaseException().Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> PreparePreviewAsync(int schoolId, int batchId)
    {
        var batch = await _db.PromotionBatches
            .Include(x => x.SourceAcademicSession)
            .Include(x => x.TargetAcademicSession)
            .FirstOrDefaultAsync(x => x.Id == batchId && x.SchoolId == schoolId);

        if (batch is null) return (false, "Rollover batch not found.");
        if (batch.Status is PromotionBatchStatus.Completed or PromotionBatchStatus.Finalized)
            return (false, "A committed/finalized rollover cannot be rebuilt. Roll it back first if rollback is still safe.");

        var oldItems = await _db.PromotionItems.Where(x => x.PromotionBatchId == batch.Id).ToListAsync();
        if (oldItems.Count > 0) _db.PromotionItems.RemoveRange(oldItems);

        var sourceEnrollments = await _db.StudentEnrollments
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.SchoolClass)
            .Include(x => x.Section)
            .Include(x => x.AcademicGroup)
            .Where(x => x.SchoolId == schoolId &&
                        x.AcademicSessionId == batch.SourceAcademicSessionId &&
                        x.IsCurrent &&
                        x.Student.Status != StudentStatus.Withdrawn &&
                        x.Student.Status != StudentStatus.Transferred &&
                        x.Student.Status != StudentStatus.Graduated)
            .OrderBy(x => x.SchoolClass != null ? x.SchoolClass.SortOrder : int.MaxValue)
            .ThenBy(x => x.Section != null ? x.Section.Name : x.SectionName)
            .ThenBy(x => x.Student.FullName)
            .ToListAsync();

        if (sourceEnrollments.Count == 0)
            return (false, "No current students were found in the source session.");

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
        var sections = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();
        var groups = await _db.AcademicGroups.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var latestResults = (await _db.StudentResults.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == batch.SourceAcademicSessionId && x.IsCurrent)
                .OrderByDescending(x => x.PublishedAtUtc)
                .ToListAsync())
            .GroupBy(x => x.StudentId)
            .ToDictionary(x => x.Key, x => x.First());

        foreach (var enrollment in sourceEnrollments)
        {
            var sourceClass = enrollment.SchoolClass ?? classes.FirstOrDefault(x =>
                string.Equals(x.Name, enrollment.ClassName, StringComparison.OrdinalIgnoreCase));
            var sourceSection = enrollment.Section;
            var sourceGroup = enrollment.AcademicGroup;
            latestResults.TryGetValue(enrollment.StudentId, out var result);

            SchoolClass? nextClass = null;
            if (sourceClass is not null)
            {
                nextClass = classes.FirstOrDefault(x => x.SortOrder > sourceClass.SortOrder);
            }

            PromotionDecision proposed;
            SchoolClass? targetClass = null;
            if (result is not null && !result.IsPassed)
            {
                proposed = PromotionDecision.Repeat;
                targetClass = sourceClass;
            }
            else if (sourceClass is not null && nextClass is not null)
            {
                proposed = PromotionDecision.Promote;
                targetClass = nextClass;
            }
            else if (sourceClass is not null && result is not null && result.IsPassed)
            {
                proposed = PromotionDecision.Graduate;
            }
            else
            {
                proposed = PromotionDecision.Hold;
                targetClass = sourceClass;
            }

            Section? targetSection = null;
            AcademicGroup? targetGroup = null;
            if (targetClass is not null)
            {
                targetSection = sections.FirstOrDefault(x => x.SchoolClassId == targetClass.Id && sourceSection != null && x.Name == sourceSection.Name)
                    ?? sections.FirstOrDefault(x => x.SchoolClassId == targetClass.Id);

                targetGroup = groups.FirstOrDefault(x => x.SchoolClassId == targetClass.Id && sourceGroup != null && x.Name == sourceGroup.Name);
            }

            _db.PromotionItems.Add(new PromotionItem
            {
                PromotionBatchId = batch.Id,
                SchoolId = schoolId,
                StudentId = enrollment.StudentId,
                SourceEnrollmentId = enrollment.Id,
                SourceSchoolClassId = sourceClass?.Id,
                SourceSectionId = sourceSection?.Id,
                SourceAcademicGroupId = sourceGroup?.Id,
                SourceStudentStatus = enrollment.Student.Status,
                SourceEnrollmentStatus = enrollment.Status,
                ProposedDecision = proposed,
                FinalDecision = proposed,
                Status = PromotionItemStatus.Pending,
                ProposedSchoolClassId = targetClass?.Id,
                ProposedSectionId = targetSection?.Id,
                ProposedAcademicGroupId = targetGroup?.Id,
                TargetSchoolClassId = targetClass?.Id,
                TargetSectionId = targetSection?.Id,
                TargetAcademicGroupId = targetGroup?.Id,
                LatestStudentResultId = result?.Id,
                LatestResultPassed = result?.IsPassed,
                LatestResultPercentage = result?.Percentage,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        batch.Status = PromotionBatchStatus.PreviewReady;
        batch.PreparedAtUtc = DateTime.UtcNow;
        batch.CompletedAtUtc = null;
        batch.RolledBackAtUtc = null;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("SessionRollover.PreviewPrepared", "PromotionBatch", batch.Id.ToString(), $"Prepared {sourceEnrollments.Count} students for review.");
        return (true, $"Promotion preview prepared for {sourceEnrollments.Count} students.");
    }

    public async Task<PromotionPreviewViewModel?> GetPreviewAsync(int schoolId, int batchId)
    {
        var batch = await _db.PromotionBatches.AsNoTracking()
            .Include(x => x.SourceAcademicSession)
            .Include(x => x.TargetAcademicSession)
            .FirstOrDefaultAsync(x => x.Id == batchId && x.SchoolId == schoolId);
        if (batch is null) return null;

        var items = await _db.PromotionItems.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.SourceEnrollment)
            .Include(x => x.SourceSchoolClass)
            .Include(x => x.SourceSection)
            .Where(x => x.PromotionBatchId == batch.Id)
            .OrderBy(x => x.SourceSchoolClass != null ? x.SourceSchoolClass.SortOrder : int.MaxValue)
            .ThenBy(x => x.SourceSection != null ? x.SourceSection.Name : x.SourceEnrollment.SectionName)
            .ThenBy(x => x.Student.FullName)
            .ToListAsync();

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PromotionClassOptionViewModel { Id = x.Id, Name = x.Name, SortOrder = x.SortOrder })
            .ToListAsync();
        var sections = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SchoolClass.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PromotionSectionOptionViewModel
            {
                Id = x.Id,
                SchoolClassId = x.SchoolClassId,
                ClassName = x.SchoolClass.Name,
                Name = x.Name
            })
            .ToListAsync();

        var rows = items.Select(x => new PromotionStudentRowViewModel
        {
            PromotionItemId = x.Id,
            StudentId = x.StudentId,
            AdmissionNumber = x.Student.AdmissionNumber,
            StudentName = x.Student.FullName,
            SourceClass = x.SourceSchoolClass?.Name ?? x.SourceEnrollment.ClassName,
            SourceSection = x.SourceSection?.Name ?? x.SourceEnrollment.SectionName,
            RollNumber = x.SourceEnrollment.RollNumber,
            LatestResultPassed = x.LatestResultPassed,
            LatestResultPercentage = x.LatestResultPercentage,
            ProposedDecision = x.ProposedDecision,
            Decision = x.FinalDecision ?? x.ProposedDecision,
            TargetSchoolClassId = x.TargetSchoolClassId ?? x.ProposedSchoolClassId,
            TargetSectionId = x.TargetSectionId ?? x.ProposedSectionId,
            ReviewNote = x.ReviewNote,
            Status = x.Status
        }).ToList();

        return new PromotionPreviewViewModel
        {
            BatchId = batch.Id,
            SourceSessionName = batch.SourceAcademicSession.Name,
            TargetSessionName = batch.TargetAcademicSession.Name,
            BatchStatus = batch.Status,
            TotalStudents = rows.Count,
            PromoteCount = rows.Count(x => x.Decision == PromotionDecision.Promote),
            RepeatCount = rows.Count(x => x.Decision == PromotionDecision.Repeat),
            HoldCount = rows.Count(x => x.Decision == PromotionDecision.Hold),
            GraduateCount = rows.Count(x => x.Decision == PromotionDecision.Graduate),
            TransferOutCount = rows.Count(x => x.Decision == PromotionDecision.TransferOut),
            Classes = classes,
            Sections = sections,
            Items = rows
        };
    }

    public async Task<(bool Success, string Message)> SavePreviewAsync(int schoolId, PromotionPreviewPostViewModel model)
    {
        var batch = await _db.PromotionBatches.FirstOrDefaultAsync(x => x.Id == model.BatchId && x.SchoolId == schoolId);
        if (batch is null) return (false, "Rollover batch not found.");
        if (batch.Status != PromotionBatchStatus.PreviewReady)
            return (false, "Only a preview-ready rollover can be edited.");

        var itemIds = model.Items.Select(x => x.PromotionItemId).Distinct().ToList();
        var items = await _db.PromotionItems
            .Where(x => x.PromotionBatchId == batch.Id && itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        if (items.Count != model.Items.Count)
            return (false, "One or more student promotion rows were not found. Refresh and try again.");

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .ToDictionaryAsync(x => x.Id);
        var sections = await _db.Sections.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .ToDictionaryAsync(x => x.Id);
        var groups = await _db.AcademicGroups.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .ToListAsync();

        foreach (var posted in model.Items)
        {
            var item = items[posted.PromotionItemId];
            if (posted.Decision == PromotionDecision.Hold)
            {
                if (!item.SourceSchoolClassId.HasValue || !classes.ContainsKey(item.SourceSchoolClassId.Value))
                    return (false, $"Student ID {item.StudentId} has no normalized source class. Map the student's class/section first, then rebuild the rollover preview.");

                item.TargetSchoolClassId = item.SourceSchoolClassId;
                item.TargetSectionId = item.SourceSectionId;
                item.TargetAcademicGroupId = item.SourceAcademicGroupId;
            }
            else if (posted.Decision is PromotionDecision.Promote or PromotionDecision.Repeat)
            {
                if (!posted.TargetSchoolClassId.HasValue || !classes.ContainsKey(posted.TargetSchoolClassId.Value))
                    return (false, $"A valid target class is required for student ID {item.StudentId}.");

                if (posted.TargetSectionId.HasValue)
                {
                    if (!sections.TryGetValue(posted.TargetSectionId.Value, out var section) || section.SchoolClassId != posted.TargetSchoolClassId.Value)
                        return (false, $"The selected section does not belong to the selected target class for student ID {item.StudentId}.");
                }

                item.TargetSchoolClassId = posted.TargetSchoolClassId;
                item.TargetSectionId = posted.TargetSectionId;

                var sourceGroup = item.SourceAcademicGroupId.HasValue
                    ? groups.FirstOrDefault(x => x.Id == item.SourceAcademicGroupId.Value)
                    : null;
                item.TargetAcademicGroupId = sourceGroup is null
                    ? null
                    : groups.FirstOrDefault(x => x.SchoolClassId == posted.TargetSchoolClassId.Value && x.Name == sourceGroup.Name)?.Id;
            }
            else
            {
                item.TargetSchoolClassId = null;
                item.TargetSectionId = null;
                item.TargetAcademicGroupId = null;
            }

            item.FinalDecision = posted.Decision;
            item.ReviewNote = string.IsNullOrWhiteSpace(posted.ReviewNote) ? null : posted.ReviewNote.Trim();
        }

        await _db.SaveChangesAsync();
        await _audit.WriteAsync("SessionRollover.PreviewUpdated", "PromotionBatch", batch.Id.ToString(), $"Reviewed {model.Items.Count} student decisions.");
        return (true, "Promotion decisions saved. Review the counts, then commit when ready.");
    }

    public async Task<(bool Success, string Message)> CommitAsync(int schoolId, int batchId)
    {
        var batch = await _db.PromotionBatches
            .Include(x => x.TargetAcademicSession)
            .FirstOrDefaultAsync(x => x.Id == batchId && x.SchoolId == schoolId);
        if (batch is null) return (false, "Rollover batch not found.");
        if (batch.Status != PromotionBatchStatus.PreviewReady)
            return (false, "Only a preview-ready rollover can be committed.");
        if (batch.TargetAcademicSession.Status != AcademicSessionStatus.Draft)
            return (false, "The target session must still be Draft while student promotion is committed.");

        var items = await _db.PromotionItems
            .Include(x => x.Student)
            .Include(x => x.SourceEnrollment)
            .Where(x => x.PromotionBatchId == batch.Id)
            .OrderBy(x => x.Id)
            .ToListAsync();
        if (items.Count == 0) return (false, "No promotion preview exists for this batch.");
        if (items.Any(x => !x.SourceEnrollment.IsCurrent || x.SourceEnrollment.AcademicSessionId != batch.SourceAcademicSessionId || x.Student.Status != x.SourceStudentStatus))
            return (false, "Student enrollment/status changed after the preview was prepared. Rebuild the preview before committing.");

        var sourceCurrentIds = await _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == batch.SourceAcademicSessionId && x.IsCurrent &&
                        x.Student.Status != StudentStatus.Withdrawn && x.Student.Status != StudentStatus.Transferred && x.Student.Status != StudentStatus.Graduated)
            .Select(x => x.StudentId)
            .ToListAsync();
        var previewStudentIds = items.Select(x => x.StudentId).ToHashSet();
        if (sourceCurrentIds.Any(x => !previewStudentIds.Contains(x)))
            return (false, "New/current students were added after the preview was prepared. Rebuild the preview before committing so nobody is missed.");

        var movingStudentIds = items
            .Where(x => (x.FinalDecision ?? x.ProposedDecision) is PromotionDecision.Promote or PromotionDecision.Repeat or PromotionDecision.Hold)
            .Select(x => x.StudentId)
            .ToList();
        var existingTargets = await _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.AcademicSessionId == batch.TargetAcademicSessionId && movingStudentIds.Contains(x.StudentId))
            .Select(x => x.StudentId)
            .ToListAsync();
        if (existingTargets.Count > 0)
            return (false, "At least one student already has an enrollment in the target session. The commit was stopped to prevent duplicates.");

        var classes = await _db.SchoolClasses.AsNoTracking().Where(x => x.SchoolId == schoolId).ToDictionaryAsync(x => x.Id);
        var sections = await _db.Sections.AsNoTracking().Where(x => x.SchoolId == schoolId).ToDictionaryAsync(x => x.Id);
        var groups = await _db.AcademicGroups.AsNoTracking().Where(x => x.SchoolId == schoolId).ToDictionaryAsync(x => x.Id);

        foreach (var item in items)
        {
            var decision = item.FinalDecision ?? item.ProposedDecision;
            if (decision is PromotionDecision.Promote or PromotionDecision.Repeat or PromotionDecision.Hold)
            {
                if (!item.TargetSchoolClassId.HasValue || !classes.ContainsKey(item.TargetSchoolClassId.Value))
                    return (false, $"Student {item.Student.FullName} does not have a valid target class.");
                if (item.TargetSectionId.HasValue && (!sections.TryGetValue(item.TargetSectionId.Value, out var section) || section.SchoolClassId != item.TargetSchoolClassId.Value))
                    return (false, $"Student {item.Student.FullName} has an invalid target section.");
                if (item.TargetAcademicGroupId.HasValue && (!groups.TryGetValue(item.TargetAcademicGroupId.Value, out var group) || group.SchoolClassId != item.TargetSchoolClassId.Value))
                    return (false, $"Student {item.Student.FullName} has an invalid target group.");
            }
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var endDate = batch.TargetAcademicSession.StartDate.Date.AddDays(-1);
            foreach (var item in items)
            {
                var decision = item.FinalDecision ?? item.ProposedDecision;
                item.SourceEnrollment.IsCurrent = false;
                item.SourceEnrollment.EffectiveTo = endDate;
                item.SourceEnrollment.UpdatedAtUtc = DateTime.UtcNow;
                item.SourceEnrollment.Status = decision switch
                {
                    PromotionDecision.Promote => StudentEnrollmentStatus.Promoted,
                    PromotionDecision.Repeat => StudentEnrollmentStatus.Repeated,
                    PromotionDecision.Hold => StudentEnrollmentStatus.Held,
                    PromotionDecision.Graduate => StudentEnrollmentStatus.Graduated,
                    PromotionDecision.TransferOut => StudentEnrollmentStatus.Transferred,
                    _ => item.SourceEnrollment.Status
                };

                if (decision == PromotionDecision.Graduate)
                    item.Student.Status = StudentStatus.Graduated;
                else if (decision == PromotionDecision.TransferOut)
                    item.Student.Status = StudentStatus.Transferred;

                item.Student.UpdatedAtUtc = DateTime.UtcNow;
            }

            // Persist source closures first so the filtered unique current-enrollment index
            // is cleared before new target enrollments are inserted.
            await _db.SaveChangesAsync();

            var newEnrollments = new List<(PromotionItem Item, StudentEnrollment Enrollment)>();
            foreach (var item in items)
            {
                var decision = item.FinalDecision ?? item.ProposedDecision;
                if (decision is not (PromotionDecision.Promote or PromotionDecision.Repeat or PromotionDecision.Hold))
                    continue;

                var targetClass = classes[item.TargetSchoolClassId!.Value];
                Section? targetSection = item.TargetSectionId.HasValue ? sections[item.TargetSectionId.Value] : null;
                AcademicGroup? targetGroup = item.TargetAcademicGroupId.HasValue ? groups[item.TargetAcademicGroupId.Value] : null;

                var enrollment = new StudentEnrollment
                {
                    SchoolId = schoolId,
                    StudentId = item.StudentId,
                    AcademicSessionId = batch.TargetAcademicSessionId,
                    SchoolClassId = targetClass.Id,
                    SectionId = targetSection?.Id,
                    AcademicGroupId = targetGroup?.Id,
                    ClassName = targetClass.Name,
                    SectionName = targetSection?.Name,
                    GroupStream = targetGroup?.Name,
                    RollNumber = item.SourceEnrollment.RollNumber,
                    EffectiveFrom = batch.TargetAcademicSession.StartDate.Date,
                    IsCurrent = true,
                    Status = decision == PromotionDecision.Hold ? StudentEnrollmentStatus.Held : StudentEnrollmentStatus.Active,
                    Notes = $"Created by annual rollover batch #{batch.Id} ({decision}).",
                    CreatedAtUtc = DateTime.UtcNow
                };
                _db.StudentEnrollments.Add(enrollment);
                newEnrollments.Add((item, enrollment));
            }

            await _db.SaveChangesAsync();

            foreach (var pair in newEnrollments)
            {
                pair.Item.TargetEnrollmentId = pair.Enrollment.Id;
                pair.Item.Status = (pair.Item.FinalDecision ?? pair.Item.ProposedDecision) == PromotionDecision.Hold
                    ? PromotionItemStatus.Held
                    : PromotionItemStatus.Processed;
                pair.Item.ProcessedAtUtc = DateTime.UtcNow;
            }

            foreach (var item in items.Where(x => (x.FinalDecision ?? x.ProposedDecision) is PromotionDecision.Graduate or PromotionDecision.TransferOut))
            {
                item.Status = PromotionItemStatus.Processed;
                item.ProcessedAtUtc = DateTime.UtcNow;
            }

            batch.Status = PromotionBatchStatus.Completed;
            batch.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var holds = items.Count(x => (x.FinalDecision ?? x.ProposedDecision) == PromotionDecision.Hold);
            await _audit.WriteAsync("SessionRollover.Committed", "PromotionBatch", batch.Id.ToString(), $"Committed {items.Count} decisions; held in target session={holds}.");
            return (true, holds == 0
                ? "Student rollover committed successfully. You can now finalize the session rollover."
                : $"Student rollover committed successfully. {holds} student(s) were carried into the target session with Held status and can be resolved later through Student placement.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, $"Nothing was partially promoted. The transaction was rolled back because: {ex.GetBaseException().Message}");
        }
    }

    public async Task<(bool Success, string Message)> RollbackAsync(int schoolId, int batchId)
    {
        var batch = await _db.PromotionBatches
            .Include(x => x.TargetAcademicSession)
            .FirstOrDefaultAsync(x => x.Id == batchId && x.SchoolId == schoolId);
        if (batch is null) return (false, "Rollover batch not found.");
        if (batch.Status != PromotionBatchStatus.Completed)
            return (false, "Only a committed, not-yet-finalized rollover can be rolled back.");
        if (batch.TargetAcademicSession.Status != AcademicSessionStatus.Draft)
            return (false, "Rollback is blocked because the target session is no longer Draft.");

        var items = await _db.PromotionItems
            .Include(x => x.Student)
            .Include(x => x.SourceEnrollment)
            .Where(x => x.PromotionBatchId == batch.Id)
            .ToListAsync();

        var targetEnrollmentIds = items.Where(x => x.TargetEnrollmentId.HasValue).Select(x => x.TargetEnrollmentId!.Value).ToList();
        var affectedStudentIds = items.Select(x => x.StudentId).ToList();

        if (targetEnrollmentIds.Count > 0)
        {
            var hasAttendance = await _db.StudentAttendances.AnyAsync(x => targetEnrollmentIds.Contains(x.StudentEnrollmentId));
            var hasMarks = await _db.StudentMarks.AnyAsync(x => targetEnrollmentIds.Contains(x.StudentEnrollmentId));
            var hasResults = await _db.StudentResults.AnyAsync(x => targetEnrollmentIds.Contains(x.StudentEnrollmentId));
            var hasFees = await _db.FeeChallans.AnyAsync(x => x.AcademicSessionId == batch.TargetAcademicSessionId && affectedStudentIds.Contains(x.StudentId));
            if (hasAttendance || hasMarks || hasResults || hasFees)
                return (false, "Rollback is blocked because the promoted students already have attendance, marks/results, or fee transactions in the target session.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (targetEnrollmentIds.Count > 0)
            {
                var targetEnrollments = await _db.StudentEnrollments.Where(x => targetEnrollmentIds.Contains(x.Id)).ToListAsync();
                _db.StudentEnrollments.RemoveRange(targetEnrollments);
                await _db.SaveChangesAsync();
            }

            foreach (var item in items)
            {
                item.SourceEnrollment.IsCurrent = true;
                item.SourceEnrollment.EffectiveTo = null;
                item.SourceEnrollment.Status = item.SourceEnrollmentStatus;
                item.SourceEnrollment.UpdatedAtUtc = DateTime.UtcNow;
                item.Student.Status = item.SourceStudentStatus;
                item.Student.UpdatedAtUtc = DateTime.UtcNow;
                item.TargetEnrollmentId = null;
                item.Status = PromotionItemStatus.RolledBack;
                item.ProcessedAtUtc = null;
            }

            batch.Status = PromotionBatchStatus.RolledBack;
            batch.RolledBackAtUtc = DateTime.UtcNow;
            batch.CompletedAtUtc = null;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _audit.WriteAsync("SessionRollover.RolledBack", "PromotionBatch", batch.Id.ToString(), "Committed student enrollment changes were safely reversed before target-session activity existed.");
            return (true, "Rollover changes were rolled back. The target session/setup remains Draft; rebuild the preview when ready.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, $"Rollback failed and was not partially applied: {ex.GetBaseException().Message}");
        }
    }

    public async Task<(bool Success, string Message)> FinalizeAsync(int schoolId, int batchId)
    {
        var batch = await _db.PromotionBatches
            .Include(x => x.SourceAcademicSession)
            .Include(x => x.TargetAcademicSession)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == batchId && x.SchoolId == schoolId);
        if (batch is null) return (false, "Rollover batch not found.");
        if (batch.Status != PromotionBatchStatus.Completed)
            return (false, "Commit the student promotion first.");
        if (batch.TargetAcademicSession.Status != AcademicSessionStatus.Draft)
            return (false, "The target academic session must be Draft before finalization.");

        var remainingCurrent = await _db.StudentEnrollments.AnyAsync(x =>
            x.SchoolId == schoolId && x.AcademicSessionId == batch.SourceAcademicSessionId && x.IsCurrent);
        if (remainingCurrent)
            return (false, "The source session still contains current enrollments. Review students before finalizing.");

        var otherActive = await _db.AcademicSessions.AnyAsync(x =>
            x.SchoolId == schoolId && x.Status == AcademicSessionStatus.Active &&
            x.Id != batch.SourceAcademicSessionId && x.Id != batch.TargetAcademicSessionId);
        if (otherActive)
            return (false, "Another academic session is already Active. Resolve it before finalizing this rollover.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (batch.SourceAcademicSession.Status == AcademicSessionStatus.Active)
            {
                batch.SourceAcademicSession.Status = AcademicSessionStatus.Closed;
                batch.SourceAcademicSession.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            batch.TargetAcademicSession.Status = AcademicSessionStatus.Active;
            batch.TargetAcademicSession.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (batch.SourceAcademicSession.Status == AcademicSessionStatus.Closed)
            {
                batch.SourceAcademicSession.Status = AcademicSessionStatus.Archived;
                batch.SourceAcademicSession.UpdatedAtUtc = DateTime.UtcNow;
            }

            batch.Status = PromotionBatchStatus.Finalized;
            batch.FinalizedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _audit.WriteAsync("SessionRollover.Finalized", "PromotionBatch", batch.Id.ToString(), $"Archived {batch.SourceAcademicSession.Name} and activated {batch.TargetAcademicSession.Name}.");
            return (true, $"Rollover finalized. {batch.TargetAcademicSession.Name} is now Active and {batch.SourceAcademicSession.Name} is archived for historical reporting.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, $"Finalization failed and the transaction was rolled back: {ex.GetBaseException().Message}");
        }
    }

    private async Task CopySessionFoundationAsync(int sourceSessionId, AcademicSession target)
    {
        var source = await _db.AcademicSessions.AsNoTracking().FirstAsync(x => x.Id == sourceSessionId);
        var offsetDays = (target.StartDate.Date - source.StartDate.Date).Days;

        var sourceTerms = await _db.Terms.AsNoTracking()
            .Where(x => x.AcademicSessionId == sourceSessionId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
        foreach (var term in sourceTerms)
        {
            _db.Terms.Add(new Term
            {
                AcademicSessionId = target.Id,
                Name = term.Name,
                StartDate = term.StartDate.Date.AddDays(offsetDays),
                EndDate = term.EndDate.Date.AddDays(offsetDays),
                DisplayOrder = term.DisplayOrder
            });
        }

        var schemes = await _db.GradingSchemes.AsNoTracking()
            .Include(x => x.Rules)
            .Where(x => x.AcademicSessionId == sourceSessionId)
            .ToListAsync();
        foreach (var scheme in schemes)
        {
            var copied = new GradingScheme
            {
                AcademicSessionId = target.Id,
                Name = scheme.Name,
                IsDefault = scheme.IsDefault
            };
            foreach (var rule in scheme.Rules)
            {
                copied.Rules.Add(new GradingRule
                {
                    Grade = rule.Grade,
                    MinPercentage = rule.MinPercentage,
                    MaxPercentage = rule.MaxPercentage,
                    Remarks = rule.Remarks
                });
            }
            _db.GradingSchemes.Add(copied);
        }

        await _db.SaveChangesAsync();
    }

    private static DateTime? ShiftDateToTarget(DateTime? value, DateTime sourceStart, DateTime targetStart)
    {
        if (!value.HasValue) return null;
        var offsetDays = (targetStart.Date - sourceStart.Date).Days;
        return value.Value.Date.AddDays(offsetDays);
    }

    private static string SuggestNextSessionName(string sourceName, DateTime sourceStart, DateTime sourceEnd)
    {
        var matchedAnyYear = false;
        var suggested = System.Text.RegularExpressions.Regex.Replace(sourceName, @"\d{4}", match =>
        {
            if (!int.TryParse(match.Value, out var year)) return match.Value;
            matchedAnyYear = true;
            return (year + 1).ToString();
        });
        return matchedAnyYear ? suggested : $"{sourceEnd.Year + 1}-{sourceEnd.Year + 2}";
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class CommunicationIndexViewModel
{
    public List<Notice> Notices { get; set; } = [];
    public List<SchoolDocument> Documents { get; set; } = [];
    public bool CanManage { get; set; }
}

public class CommunicationManageViewModel
{
    public List<Notice> Notices { get; set; } = [];
    public List<SchoolDocument> Documents { get; set; } = [];
    public List<CommunicationHistory> History { get; set; } = [];
    public int PublishedNoticeCount { get; set; }
    public int ActiveDocumentCount { get; set; }
    public int ExternalNotConfiguredCount { get; set; }
}

public class NoticeFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(6000)]
    [DataType(DataType.MultilineText)]
    public string Body { get; set; } = string.Empty;

    [Required]
    public NoticeAudience Audience { get; set; } = NoticeAudience.All;

    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }

    [Display(Name = "Visible from")]
    public DateTime? PublishFromLocal { get; set; }

    [Display(Name = "Visible until")]
    public DateTime? PublishUntilLocal { get; set; }

    [Display(Name = "Attachment")]
    public IFormFile? Attachment { get; set; }

    public string? ExistingAttachmentName { get; set; }
    public bool RemoveExistingAttachment { get; set; }
    public string? RowVersion { get; set; }

    public List<SimpleOptionViewModel> Classes { get; set; } = [];
    public List<SimpleOptionViewModel> Sections { get; set; } = [];
}

public class SchoolDocumentFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public SchoolDocumentCategory Category { get; set; } = SchoolDocumentCategory.General;

    [Required]
    public NoticeAudience Audience { get; set; } = NoticeAudience.All;

    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }

    [Display(Name = "Effective from")]
    public DateTime? EffectiveFromLocal { get; set; }

    [Display(Name = "Effective until")]
    public DateTime? EffectiveUntilLocal { get; set; }

    public bool IsActive { get; set; } = true;

    [Display(Name = "Document")]
    public IFormFile? File { get; set; }

    public string? ExistingFileName { get; set; }
    public string? RowVersion { get; set; }

    public List<SimpleOptionViewModel> Classes { get; set; } = [];
    public List<SimpleOptionViewModel> Sections { get; set; } = [];
}

public class CommunicationDispatchViewModel
{
    [Required]
    public int NoticeId { get; set; }

    public string NoticeTitle { get; set; } = string.Empty;
    public NoticeAudience Audience { get; set; }
    public string AudienceLabel { get; set; } = string.Empty;

    [Required]
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.InApp;

    [StringLength(200)]
    public string? Subject { get; set; }

    [Required, StringLength(4000)]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = string.Empty;
}

public class SimpleOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
}

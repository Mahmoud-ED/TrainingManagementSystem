// ViewModel لتمثيل الصندوق الواحد في المخطط
public class CourseNodeViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public int DurationHours { get; set; }
    public string RequirementTypeCssClass { get; set; }

    // هذه أهم خصائص للتخطيط
    public int Row { get; set; }
    public int Column { get; set; }
}

// ViewModel لتمثيل الخط الواصل (السهم)
public class PrerequisiteLinkViewModel
{
    public string SourceId { get; set; } // ID الصندوق الذي يخرج منه السهم
    public string TargetId { get; set; } // ID الصندوق الذي يدخل إليه السهم
}

// ViewModel الرئيسي للصفحة كلها
public class CoursePlanViewModel
{
    public List<CourseNodeViewModel> Courses { get; set; }
    public List<PrerequisiteLinkViewModel> Prerequisites { get; set; }
    public int MaxRow { get; set; }
    public int MaxColumn { get; set; }
}
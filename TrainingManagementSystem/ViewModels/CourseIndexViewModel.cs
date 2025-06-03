using Microsoft.AspNetCore.Mvc.Rendering;

public class CourseIndexViewModel
{
    public List<CourseItemViewModel> Courses { get; set; } // قائمة الدورات للعرض
    public SelectList CourseClassifications { get; set; } // لملء dropdown الفلترة
    public SelectList Levels { get; set; } // لملء dropdown الفلترة
    public SelectList CourseParents { get; set; } // لملء dropdown الفلترة

    // خصائص للبحث والفلترة
    public string SearchString { get; set; }
    public Guid? SelectedCourseClassificationId { get; set; }
    public Guid? SelectedLevelId { get; set; }
    public Guid? SelectedCourseParentId { get; set; }

    // خصائص لترقيم الصفحات (Pagination)
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

public class CourseItemViewModel // لتمثيل كل دورة في القائمة
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string CourseClassificationName { get; set; }
    public string LevelName { get; set; }
    public string? CourseParentName { get; set; }
    public int CourseDetailsCount { get; set; }
    public int TrainersCount { get; set; }
}
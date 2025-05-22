using Microsoft.AspNetCore.Mvc;
using TrainingManagementSystem.Models.Entities;

namespace TrainingManagementSystem.ViewComponents
{
    public class CreateCourseClassificationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = new CourseClassification();
            return View(model); // يعرض النموذج فقط
        }
    }
}

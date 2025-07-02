namespace TrainingManagementSystem.Models.Entities
{
    public class CoursePrerequisite
    {
        // المفتاح الخارجي للكورس الأساسي
        // (مثال: Id الخاص بـ "قواعد بيانات 2")
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; }

        // المفتاح الخارجي للكورس الذي يعتبر متطلباً مسبقاً
        // (مثال: Id الخاص بـ "قواعد بيانات 1")
        public Guid PrerequisiteCourseId { get; set; }
        public virtual Course PrerequisiteCourse { get; set; }
    }
}

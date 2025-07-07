public class AttendanceIndexModel : PageModel
{
    public CourseDetails CourseDetails { get; set; }
    public List<Trainee> Trainees { get; set; }
    public List<DateTime> Days { get; set; } // ?? ?????? ??? ??????

    // ... OnGet/OnPost logic
}
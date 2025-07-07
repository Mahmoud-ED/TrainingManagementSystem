using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Models;

public class CourseStatusUpdaterService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer = null;

    public CourseStatusUpdaterService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        // ضبط المؤقت ليعمل كل 24 ساعة
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(24));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        // يجب إنشاء نطاق جديد (Scope) لأن DbContext مسجل كـ Scoped والخدمة Singleton
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // استبدل YourDbContext باسم الـ DbContext الخاص بك

            // نفس منطق التحديث من الطريقة الأولى بالضبط
            var inRegistrationStatusId = new Guid("07057281-82e6-4789-a740-e30d1029d4f6");
            var inExecutionStatusId = new Guid("07057281-82e6-4789-a740-e30d1129d4f6");
            var notExecutedStatusId = new Guid("07057281-82e6-4789-a740-e31d1129d4f6");

            var coursesToReview = await context.CourseDetails
                .Include(cd => cd.CourseTrainees)
                .Where(cd => cd.StatusId == inRegistrationStatusId && cd.StartDate <= DateTime.Today)
                .ToListAsync();

            if (!coursesToReview.Any()) return; // لا يوجد شيء لفعله

            foreach (var course in coursesToReview)
            {
                if (course.CourseTrainees.Any())
                {
                    course.StatusId = inExecutionStatusId;
                }
                else
                {
                    course.StatusId = notExecutedStatusId;
                }
            }

            await context.SaveChangesAsync();
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
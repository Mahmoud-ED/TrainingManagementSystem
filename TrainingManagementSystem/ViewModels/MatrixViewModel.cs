using System.Collections.Generic;
using TrainingManagementSystem.Models.Entities;

namespace TrainingManagementSystem.ViewModels
{
    // هذا سيمثل كل سطر في الجدول
    public class MatrixCourseItem
    {
        public PlanCours Plan { get; set; }
        public List<double> WeeklyHours { get; set; } // قائمة بعدد الساعات لكل أسبوع في الشهر

        public MatrixCourseItem()
        {
            WeeklyHours = new List<double>();
        }
    }

    // هذا هو الموديل الرئيسي الذي سنمرره للـ View
    public class MatrixViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int NumberOfWeeks { get; set; }
        public List<MatrixCourseItem> Courses { get; set; }
        public List<double> WeeklyTotals { get; set; } // الإجمالي العمودي لكل أسبوع
        public double GrandTotal { get; set; } // الإجمالي الكلي للشهر

        public MatrixViewModel()
        {
            Courses = new List<MatrixCourseItem>();
            WeeklyTotals = new List<double>();
        }
    }
}
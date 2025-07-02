using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace TrainingManagementSystem.ViewModels
{
    // هذا يمثل كل برنامج سيظهر في القائمة للاختيار
    public class PlanToCopyItem
    {
        public Guid PlanId { get; set; } // معرف الخطة الأصلي
        public string CourseName { get; set; }
        public DateTime OriginalStartDate { get; set; }
        public string LocationName { get; set; }
        public bool IsSelected { get; set; } // لتتبع حالة الـ checkbox
    }

    // هذا هو الموديل الرئيسي للشاشة
    public class AdvancedCopyViewModel
    {
        // --- حقول الإدخال ---
        public int? SourceYear { get; set; }
        public int TargetYear { get; set; }

        // --- حقول العرض ---
        public IEnumerable<SelectListItem> YearList { get; set; }
        public List<PlanToCopyItem> PlansToCopy { get; set; }

        // --- حقل لاستقبال البيانات عند الإرسال ---
        public List<Guid> SelectedPlanIds { get; set; }

        public AdvancedCopyViewModel()
        {
            PlansToCopy = new List<PlanToCopyItem>();
            SelectedPlanIds = new List<Guid>();
            // السنة الهدف الافتراضية هي السنة القادمة
            TargetYear = DateTime.Today.Year + 1;
        }
    }
}
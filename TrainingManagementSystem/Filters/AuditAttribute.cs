using Microsoft.AspNetCore.Mvc;

namespace TrainingManagementSystem.Filters
{
    public class AuditAttribute : TypeFilterAttribute
    {
        public AuditAttribute(string actionType, string entityName)
            : base(typeof(AuditActionFilter))
        {
            Arguments = new object[] { actionType, entityName };
        }
    }
}
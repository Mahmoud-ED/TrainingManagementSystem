using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;

public static class BreadcrumbHelper
{
    public static string GetControllerDisplayName(ViewContext viewContext)
    {
        var controllerDescriptor = viewContext.ActionDescriptor as ControllerActionDescriptor;
        var controllerType = controllerDescriptor?.ControllerTypeInfo;
        var displayAttribute = controllerType?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? controllerDescriptor?.ControllerName;
    }

    public static string GetActionDisplayName(ViewContext viewContext)
    {
        var controllerDescriptor = viewContext.ActionDescriptor as ControllerActionDescriptor;
        var methodInfo = controllerDescriptor?.MethodInfo;
        var displayAttribute = methodInfo?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? controllerDescriptor?.ActionName;
    }
}

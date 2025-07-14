using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TrainingManagementSystem.Classes
{
    public interface IViewHelper
    {
        string RenderViewToString(string viewName, object model);

    }
    public class ViewHelper : IViewHelper
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ViewHelper(IRazorViewEngine viewEngine, ITempDataProvider dataProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = dataProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public string RenderViewToString(string viewName, object model)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("HttpContext is not available.");
            }

            var routeData = httpContext.GetRouteData() ?? new RouteData();
            var controllerContext = new ControllerContext(new ActionContext(httpContext, routeData, new ControllerActionDescriptor()));
            var actionContext = new ActionContext(httpContext, routeData, controllerContext.ActionDescriptor);
            var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);

            // Create an instance of ViewDataDictionary
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                // Set the model in ViewData
                Model = model
            };

            using (var sw = new StringWriter())
            {
                var viewResult = _viewEngine.GetView(null, viewPath: viewName, isMainPage: false);

                if (viewResult.Success)
                {
                    var viewContext = new ViewContext(
                        actionContext,
                        viewResult.View,
                        viewData,
                        tempData,
                        sw,
                        new HtmlHelperOptions()
                    );

                    viewResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                    return sw.ToString();
                }

                throw new InvalidOperationException($"Unable to find view '{viewName}'");
            }
        }


    }
}

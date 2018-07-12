namespace Dragonfly.Framework.Core
{
    using System.Configuration;
    using System.Web.Configuration;
    using System.Web.Mvc;
    using DevTrends.MvcDonutCaching;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using Umbraco.Web.Models;
    using Umbraco.Web.Mvc;

    public abstract class DragonflySurfaceController : SurfaceController, IRenderMvcController
    {
        #region Render MVC

        /// <summary>
        /// Checks to make sure the physical view file exists on disk.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        protected bool EnsurePhysicalViewExists(string template)
        {
            var result = ViewEngines.Engines.FindView(this.ControllerContext, template, null);

            if (result.View == null)
            {
                LogHelper.Warn<DragonflySurfaceController>("No physical template file was found for template " + template);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns an ActionResult based on the template name found in the route values and the given model.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// If the template found in the route values doesn't physically exist, then an empty ContentResult will be returned.
        /// </remarks>
        protected ActionResult CurrentTemplate<T>(T model)
        {
            var template = this.ControllerContext.RouteData.Values["action"].ToString();

            if (!this.EnsurePhysicalViewExists(template))
            {
                return this.HttpNotFound();
            }

            return this.View(template, model);
        }

        /// <summary>
        /// The default action to render the front-end view.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual ActionResult Index(RenderModel model)
        {
            return this.CurrentTemplate(model);
        }

        #endregion

        #region Exception Handling

        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
            {
                return;
            }

            //Log the exception.
            var msg =
                $"DragonflySurfaceController Error on page '{this.CurrentPage.Name}' [{this.CurrentPage.Id}] {this.CurrentPage.UrlAbsolute()}";
            LogHelper.Error<DragonflySurfaceController>(msg, filterContext.Exception);

            //Clear the MvcDonutCache if an error occurs.
            var cacheManager = new OutputCacheManager();
            cacheManager.RemoveItems();

            //Show the custom view error, if debug=false (otherwise, just throw the error)
            CompilationSection compilationSection = (CompilationSection)ConfigurationManager.GetSection(@"system.web/compilation");

            if (compilationSection != null)
            {
                bool isDebug = compilationSection.Debug;
                if (!isDebug)
                {
                    var handledContext = this.ReturnErrorView(filterContext, this.CurrentPage);
                    //var errorModel = new ErrorPageModel();

                    filterContext.Result = handledContext.Result;
                    filterContext.ExceptionHandled = true;
                }
            }
        }

        public abstract ExceptionContext ReturnErrorView(ExceptionContext ExContext, IPublishedContent CurrentPublishedContent);

        #endregion
    }
}
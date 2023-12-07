﻿using MiniWebServer.Mvc.Abstraction;
using MiniWebServer.Mvc.MiniRazorEngine.Parser;

namespace MiniWebServer.Mvc.MiniRazorEngine
{
    public class DefaultViewFinder(string viewDirectory) : IViewFinder
    {
        private readonly string viewDirectory = viewDirectory ?? throw new ArgumentNullException(nameof(viewDirectory));

        public string? Find(ActionResultContext context, string viewName)
        {
            var controllerName = context.Controller.GetType().Name;
            if (controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                controllerName = controllerName[..^10];
            }

            if (FindView(Path.Combine(viewDirectory, controllerName, viewName + ".cshtml"), out string? viewContent))
            {
                return viewContent;
            }

            if (FindView(Path.Combine(viewDirectory, "Shared", viewName + ".cshtml"), out viewContent))
            {
                return viewContent;
            }

            return null;
        }

        private static bool FindView(string viewPath, out string? viewContent)
        {
            if (File.Exists(viewPath))
            {
                viewContent = File.ReadAllText(viewPath);

                return true;
            }

            viewContent = null;
            return false;
        }
    }
}

﻿using MiniWebServer.Abstractions;
using MiniWebServer.MiniApp;
using MiniWebServer.Mvc.Abstraction.ActionResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Mvc.Abstraction
{
    public abstract class Controller: IController
    {
        private ControllerContext? controllerContext = null;
        public ControllerContext ControllerContext { 
            get
            {
                if (controllerContext == null)
                {
                    throw new InvalidOperationException("ControllerContext must be inited before executing actions");
                }

                return controllerContext;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(nameof(value));

                controllerContext = value;
            } 
        }

        public IMiniAppContext Context => ControllerContext.Context;
        public IHttpRequest Request => ControllerContext.Context.Request;
        public IHttpResponse Response => ControllerContext.Context.Response;
        public ISession Session => ControllerContext.Context.Session;
        public ClaimsPrincipal? User => ControllerContext.Context.User;

        protected IActionResult Ok(object? content)
        {
            return new OkActionResult(content);
        }
        public IActionResult View(string viewName, object? model, IDictionary<string, object>? viewData, string? contentType = default)
        {
            return new ViewActionResult(controllerContext!, viewName, model, contentType ?? "text/html", viewData ?? new Dictionary<string, object>(), controllerContext!.ViewEngine);
        }
        public IActionResult View([CallerMemberName] string callerName = "")
        {
            return View(callerName, null, null, null);
        }
        public IActionResult View(object? model, [CallerMemberName] string callerName = "")
        {
            return View(callerName, model, null, null);
        }
        public IActionResult View(string viewName, object model)
        {
            return View(viewName, model, null, null);
        }
    }
}

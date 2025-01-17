﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MaxithermalWebApplication
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { controller = "ReceiveData", action = "Post", id = RouteParameter.Optional }
            );
        }
    }
}

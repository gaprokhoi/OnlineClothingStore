using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ClothingStoreWebApp
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Admin Dashboard route - phải đặt trước các route khác
            routes.MapRoute(
                name: "AdminDashboard",
                url: "Admin",
                defaults: new { controller = "Admin", action = "Index" },
                namespaces: new[] { "ClothingStoreWebApp.Controllers" }
            );

            // Admin actions routes - các action khác của AdminController
            routes.MapRoute(
                name: "AdminActions",
                url: "Admin/{action}/{id}",
                defaults: new { controller = "Admin", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "ClothingStoreWebApp.Controllers" }
            );

            // API routes (nếu có) - đặt trước Default route
            //routes.MapRoute(
            //    name: "API",
            //    url: "api/{controller}/{action}/{id}",
            //    defaults: new { controller = "Api", action = "Index", id = UrlParameter.Optional },
            //    namespaces: new[] { "ClothingStoreWebApp.Controllers" }
            //);

            // User authentication routes - để tránh conflict
            routes.MapRoute(
                name: "UserAuth",
                url: "User/{action}/{id}",
                defaults: new { controller = "User", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "ClothingStoreWebApp.Controllers" }
            );


            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

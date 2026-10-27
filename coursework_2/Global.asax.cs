using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace coursework_2
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null) return;

            FormsAuthenticationTicket ticket;
            try { ticket = FormsAuthentication.Decrypt(authCookie.Value); }
            catch { return; }

            if (ticket == null || ticket.Expired || string.IsNullOrEmpty(ticket.UserData)) return;

            var identity = new FormsIdentity(ticket);
            var principal = new System.Security.Principal.GenericPrincipal(identity, new[] { ticket.UserData });

            Context.User = principal;
            System.Threading.Thread.CurrentPrincipal = principal;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using myGlassApps.models;
using System.Text;
using System.IO;

namespace myGlassApps
{
    /// <summary>
    /// Summary description for index
    /// </summary>
    public class index : IHttpHandler
    {
        // create a view map
        Dictionary<string, string> htmlViewMap = new Dictionary<string, string>();
        public void ProcessRequest(HttpContext context)
        {
            //LoadHTMLLocations();
            ViewManager vm = new ViewManager();
            vm.AddView("index", "~/views/index.html");

            context.Response.ContentType = "text/html";

            String oAuthURL = Authorization.GetAuthorizationUrl("", "");

            vm.Arguments = new Dictionary<String, String>() {                
                { "AUTH_CODE" , oAuthURL } 
            };

            context.Response.Write(vm.RenderView("index"));
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}
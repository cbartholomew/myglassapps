using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.IO;

namespace myGlassApps.models
{
    public class ViewManager
    {
        private Dictionary<String,String> htmlViewMap { get; set; }
        public  Dictionary<String,String> Arguments { get; set; }

        public ViewManager() {
            this.htmlViewMap = new Dictionary<String, String>();               
        }

        public void AddView(String shortNameKey, String viewLocation)
        {
            this.htmlViewMap.Add(shortNameKey, viewLocation);
            this.Arguments = null;
        }

        public void RemoveView(String shortNameKey)
        {
            this.htmlViewMap.Remove(shortNameKey);        
        }

        public String RenderView(String shortNameKey)
        {
            StringBuilder html = new StringBuilder();
            // default to the error location
            string viewLocation = "";
            // apply the location from the viewmap
            this.htmlViewMap.TryGetValue(shortNameKey, out viewLocation);
            // map the server path
            var path = System.Web.HttpContext.Current.Server.MapPath(viewLocation);
            // read in html server side
            IEnumerable<string> htmlQuery = File.ReadLines(path);
            // write out
            foreach (string htmlLine in htmlQuery)
            {
                if (this.Arguments != null)
                {
                    string modifiedLine = htmlLine;
                    string item = "";
                    int startPos = 0;
                    int endPos = 0;
                    startPos = htmlLine.IndexOf('{');
                    endPos = htmlLine.LastIndexOf('}');

                    if (startPos == -1 || endPos == -1)
                    {
                        // just keep writing despite if key not present
                        html.Append(modifiedLine);
                        continue;
                    }
                    else
                    {
                        // don't get curly braces
                        startPos += 1;
                        // extract the real key
                        item = htmlLine.Substring(startPos, endPos - startPos);

                        // now replace if possible
                        if (this.Arguments.ContainsKey(item))
                        {
                            // replace and update code
                            modifiedLine = htmlLine.Replace("{" + item + "}", this.Arguments[item]);
                        }

                        html.Append(modifiedLine);
                        continue;
                    }
                }
                else
                {
                    // handles no additional arguments
                    html.Append(htmlLine);
                }
            }
            // write out server side
            return html.ToString();        
        }
    }
}
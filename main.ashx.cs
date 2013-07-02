using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Google.Apis.Authentication;
using DotNetOpenAuth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;
using Google.Apis.Mirror.v1;
using Google.Apis.Mirror.v1.Data;
using myGlassApps.models;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using System.Text;
using System.IO;

using System.Web.SessionState;
using System.Diagnostics;

namespace myGlassApps
{
    /// <summary>
    /// Summary description for main
    /// </summary>
    public class main : IHttpHandler, IRequiresSessionState
    {
        public ViewManager vm { get; set; }
        private String authorizationCode { get; set; }
        private String userId { get; set; }
        private String state  { get; set; }
        private String operation { get; set; }        
        private BlogLink blogLink { get; set; }   

        public void ProcessRequest(HttpContext ctx)
        {
            // load views
            this.vm = new ViewManager();
            this.vm.AddView("main", "~/views/main.html");
     
            // html page
            ctx.Response.ContentType = "text/html";

            IAuthenticator credentials = null;

            try
            {
                // check if code is already set
                if (ctx.Session["CODE"] == null)
                {
                    setAuthorizationCode(ctx);
                    credentials = GetAuthInteface();               
                }
                else
                {                
                    setUserId(ctx);
                    credentials = GetAuthInterfaceToken(this.userId);
                }

                if (credentials != null)
                {
                    switch (ctx.Request.HttpMethod)
                    {
                        case "POST":
                            handleOperation(ctx, credentials);
                            break;
                        default:
                            break;
                    }
                }

                handleBlogs(ctx, credentials);
            }
            catch (Exception)
            {
                ctx.Response.Redirect("https://myglassapps.com/");
            }
        }

        private void handleOperation(HttpContext ctx, IAuthenticator authInterface)
        {
            setOperation(ctx);

            switch (this.operation)
            {
                case "syncBlog":
                    insertSharedContact(ctx, authInterface);
                    DAL.InsertIntoBlogLink(this.blogLink);
                    break;
                case "deactivateBlog":
                    removeSharedContact(ctx, authInterface);
                    break;
                case "enableToolboxCard":
                    enableToolboxCard(ctx, authInterface);
                    break;
                case "disableToolboxCard":
                    disableToolboxCard(ctx, authInterface);
                    break;
                default:
                    break;
            }

        }
        

        private void enableToolboxCard(HttpContext ctx, IAuthenticator authInterface)
        {
            try
            {
                // set authorization from session;
                this.authorizationCode = ctx.Session["CODE"].ToString();
                // get the user id
                String userId = DAL.RetrieveUserIdByAuthCode(this.authorizationCode);
                // get the view code for the new timeline card
                ViewManager vm = new ViewManager();
                // add view
                vm.AddView("timelineCardToolbox", "~/views/toolboxTimelineCardCover.html");           
                // insert timeline card while rendering new view
                TimelineItem toolboxCard = Mirror.insertToolboxTimelineCard(Mirror.BuildService(authInterface), vm.RenderView("timelineCardToolbox"));
                // make timeline toolbox card with new item
                PostToolbox pt = new PostToolbox() 
                { 
                    userId = userId,
                    itemId = toolboxCard.Id,
                    isActive = true
                };

                // insert it into the database
                DAL.InsertPostToolbox(pt.userId, pt.itemId, pt.isActive);
            }
            catch (Exception ex)
            {
                String requestId = DAL.RetrieveRequestIdByAuthCode(this.authorizationCode);
                DAL.InsertAccessLog(requestId,userId,ex.ToString());                
            }
        }

        private void disableToolboxCard(HttpContext ctx, IAuthenticator authInterface)
        {
            try
            {
                // set authorization from session;
                this.authorizationCode = ctx.Session["CODE"].ToString();
                // get the user id
                String userId = DAL.RetrieveUserIdByAuthCode(this.authorizationCode);
                // get card from post toolbox
                PostToolbox pt = DAL.GetPostToolbox(userId);
                // remove the timeline card on disable
                Mirror.DeleteTimelineItem(Mirror.BuildService(authInterface), pt.itemId);
                // disable credentials in the database (can use insert, it will update)
                DAL.InsertPostToolbox(userId, pt.itemId, false);
            }
            catch (Exception ex)
            {
                String requestId = DAL.RetrieveRequestIdByAuthCode(this.authorizationCode);
                DAL.InsertAccessLog(requestId, userId, ex.ToString());        
            }
        }

        private void insertSharedContact(HttpContext ctx, IAuthenticator authInterface)
        {
            // set authorization from session;
            this.authorizationCode = ctx.Session["CODE"].ToString();
            // remove exsisting subscription
            Mirror.UnsubscribeFromNotifications(Mirror.BuildService(authInterface), "timeline");
            // remove exsisting contact
            Mirror.findSharedContactAndRemove(Mirror.BuildService(authInterface), DAL.GetContactType().Id);
            // insert a new contact
            Contact insertedContact = Mirror.insertContact(Mirror.BuildService(authInterface), DAL.GetContactType());
            // insert a new subscription request
            Subscription insertedSubscription = insertSubscription(ctx, authInterface);
            // create blog link data row
            makeBlogLink(ctx, insertedContact.Source,insertedSubscription.Id);           
        }

        private void makeBlogLink(HttpContext ctx, String sharedContactSource, String subscriptionId)
        {
            String[] blogRequest = ctx.Request["blog_selection"].Split('|');
            String _blogId = blogRequest[0];
            String _blogName = blogRequest[1];
            
            this.blogLink = new BlogLink()
            {
                userId = DAL.RetrieveUserIdByAuthCode(this.authorizationCode),
                blogId = _blogId,
                contactId = DAL.GetContactType().Id,
                isActive = true,
                blogName = _blogName,
                source = sharedContactSource,
                subscriptionId = subscriptionId
            };
        }

        private void removeSharedContact(HttpContext ctx, IAuthenticator authInterface)
        {
            Mirror.deleteContact(Mirror.BuildService(authInterface), DAL.GetContactType().Id);

            Mirror.UnsubscribeFromNotifications(Mirror.BuildService(authInterface), "timeline");

            this.authorizationCode = ctx.Session["CODE"].ToString();

            String userId = DAL.RetrieveUserIdByAuthCode(this.authorizationCode);
            String blogId = (ctx.Request["blog_id"] != null) ? ctx.Request["blog_id"].ToString() : null;

            DAL.DisableBlog(userId);

        }

        private void handleBlogs(HttpContext ctx, IAuthenticator authInterface)
        {        
            StringBuilder sb = new StringBuilder();
           
            StringBuilder postToolboxResponse = new StringBuilder();       
           
            this.authorizationCode = ctx.Session["CODE"].ToString();
           
            String userId = DAL.RetrieveUserIdByAuthCode(this.authorizationCode);
           
            BlogList blogList = Blogger.getBlogs(Blogger.BuildService(authInterface), userId);
           
            BlogLink blogLink = DAL.GetActiveBlogLinkByUserId(userId);
           
            //<--- this code is used to handle the add-on's tab -->
            PostToolbox postToolbox = DAL.GetPostToolbox(userId);
           
            postToolboxResponse.Append("<table class='table'>");
            postToolboxResponse.Append("<thead>");
            postToolboxResponse.Append("<th>Feature</th>");
            postToolboxResponse.Append("<th>Preview</th>");
            postToolboxResponse.Append("<th>Description</th>");
            postToolboxResponse.Append("<th>Action</th>");
            postToolboxResponse.Append("</thead>");
            postToolboxResponse.Append("<tbody>");
            postToolboxResponse.Append("<tr>");
            postToolboxResponse.Append("<td>Toolcard</td>");
            postToolboxResponse.Append("<td><a href='img/toolcard.png' target='_blank'><img src='img/toolcard.png' class='img-polaroid' style='height:150px;width:300px;'/></a></td>");
            postToolboxResponse.Append("<td style='width:50%'><p>The toolcard is a pinnable item that allows the glass user to extend the functionalilty of the glassware service. Additional options include blog-by-voice and comment retrieval.</p></td>");
        
            if (postToolbox.isActive)
            {
                postToolboxResponse.Append("<td>");
                postToolboxResponse.Append("<input type='hidden' name='operation' value='disableToolboxCard' />");
                postToolboxResponse.Append("<button type='submit' class='btn btn-danger'><i class='icon-remove-sign'></i> Disable</button>");
                postToolboxResponse.Append("</td>");
            }
            else
            {
                postToolboxResponse.Append("<td>");
                postToolboxResponse.Append("<input type='hidden' name='operation' value='enableToolboxCard' />");
                postToolboxResponse.Append("<button type='submit' class='btn btn-success'><i class='icon-ok-sign'></i> Enable</button>");
                postToolboxResponse.Append("</td>");
            }
            
            postToolboxResponse.Append("</tr>");
            postToolboxResponse.Append("</tbody>");
            postToolboxResponse.Append("</table>");
            //<--- end add-on tab -->


            if (blogList.Items.Count > 0)
            {
                sb.Append("<table class='table table-striped'>");
                sb.Append("<thead>");
                sb.Append("<th>Blog</th>");
                sb.Append("<th>Default</th>");
                sb.Append("</thead>");
                sb.Append("<tbody>");
                foreach (Blog blog in blogList.Items)
                {
                        String isHighlight = (blogLink.blogId == blog.Id) ? "success" : "";

                        sb.Append("<tr class='" + isHighlight + "'>");
                        sb.Append("<td>" + blog.Name + "</td>");    
                        if (blogLink.blogId == blog.Id)
                        {
                            sb.Append("<td><input value='" + blog.Id + "|" + blog.Name + "' type='radio' name='blog_selection' checked='checked' /></td>");
                        }
                        else
                        {
                            sb.Append("<td><input value='" + blog.Id + "|" + blog.Name + "' type='radio' name='blog_selection' /></td>");
                        }        
                        sb.Append("</tr>");
                }
                sb.Append("<tr>");
                sb.Append("<td></td>");
                sb.Append("<td><button type='submit' class='btn btn-block'><i class='icon-retweet'></i> Synchronize Glass</button></td>");
                sb.Append("</tr>");
                sb.Append("</tbody>");
                sb.Append("</table>");
            }
            else
            {
                sb.Append("<h3>No Blogger Blogs found for this account!</h3>");
            }

            vm.Arguments = new Dictionary<String, String>()
            {
                {"BLOGS",sb.ToString()},
                {"BUTTON_OPERATION",postToolboxResponse.ToString()}
            };

            // wright file
            ctx.Response.Write(vm.RenderView("main"));
        }

        private Subscription insertSubscription(HttpContext ctx, IAuthenticator authInterface) 
        {
            // set authorization from session;
            this.authorizationCode = ctx.Session["CODE"].ToString();

            // get the user id
            String userId = DAL.RetrieveUserIdByAuthCode(this.authorizationCode);

            String[] blogRequest = ctx.Request["blog_selection"].Split('|');
            String _blogId = blogRequest[0];

            // build blogger 
            Subscription subscription = Mirror.buildBloggerSubscription(userId, DAL.RetrieveRequestIdByAuthCode(this.authorizationCode), _blogId);

            // subscribe to notfications
           return Mirror.SubscribeToNotifications(Mirror.BuildService(authInterface), subscription);
        }

        private void setAuthorizationCode(HttpContext ctx)
        {
            this.authorizationCode = ctx.Request["code"];
            ctx.Session.Add("CODE", this.authorizationCode);
        }

        private void setOperation(HttpContext ctx)
        {
            
            this.operation = ctx.Request["operation"];

        }

        private void setState(HttpContext ctx)
        {
            this.state = ctx.Request.Url.AbsoluteUri;
        }

        private void setUserId(HttpContext ctx)
        {
            var code = ctx.Session["CODE"];
            this.userId = DAL.RetrieveUserIdByAuthCode(code.ToString());               
        }

        private IAuthenticator GetAuthInteface()
        {            
            return Authorization.GetCredentials(this.authorizationCode, this.state);
        }

        private IAuthenticator GetAuthInterfaceToken(String userId)
        {
            IAuthorizationState credentials = DAL.RetrieveCredentials(userId);
            return Authorization.GetAuthenticatorFromState(credentials);
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
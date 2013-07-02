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
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace myGlassApps
{

    /// <summary>
    /// Summary description for notification
    /// </summary>
    public class notification : IHttpHandler
    {
        // for comments, how many posts should we pull to check?
        public const int RECENT_POST_COUNT = 5;

        private String operation { get; set; }

        static String PROD_URI = "https://myglassapps.com";
        static String TEST_URI = "http://localhost:8080";

        static String BASE_URI = PROD_URI;

        public void ProcessRequest(HttpContext ctx)
        {
            Notification mirror = null;
            String requestPayload = "";
            try
            {

                using (var reader = new StreamReader(ctx.Request.InputStream))
                {
                    requestPayload = reader.ReadToEnd();
                }
                // deserialize the object to a workable class
                JToken request = JObject.Parse(requestPayload);

                mirror = new Notification();
                mirror.ItemId = (String)request.SelectToken("itemId");
                mirror.UserToken = (String)request.SelectToken("userToken");
                mirror.VerifyToken =(String)request.SelectToken("verifyToken");
                mirror.Operation = (String)request.SelectToken("operation");
                mirror.UserActions = new List<UserAction>();
                
                JEnumerable<JToken> jtokens = request.SelectToken("userActions").Children<JToken>();

                foreach (JToken innerReqest in jtokens)
                {
                    UserAction ua = new UserAction();
                    
                    ua.Type = (String)innerReqest.SelectToken("type");
                    
                    ua.Payload = (String)innerReqest.SelectToken("payload");
                    
                    mirror.UserActions.Add(ua);

                    break;
                }

                string additionalData = "";
                additionalData += "|User Action: " + mirror.UserActions[0].Type;
                additionalData += "|Payload: " + requestPayload;

                if (mirror.Operation == "INSERT")
                {
                    switch (mirror.UserActions[0].Type)
                    {
                        case "SHARE":
                            DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, additionalData);
                            handleShare(ctx, mirror, requestPayload);
                            break;
                        case "REPLY":
                            DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, additionalData);
                            handleReply(ctx, mirror, requestPayload);
                            break;
                        case "CUSTOM":
                            DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, additionalData);
                            handlePayload(ctx, mirror, requestPayload);
                            break;
                        case "DELETE":
                        default:
                            break;
                    }
                }
                else if (mirror.Operation == "UPDATE")
                {
                    switch (mirror.UserActions[0].Type)
                    {
                        case "CUSTOM":
                            DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, additionalData + "mirror.operation");
                            handlePayload(ctx,mirror,requestPayload);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                string additionalData = "";
                additionalData +=     "|User Action: " + mirror.UserActions[0].Type;
                additionalData +=     "|Payload: " + requestPayload;
                additionalData +=     "|EXCEPTION: " + ex.ToString();
                DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, additionalData);
            }
            ctx.Response.AddHeader("Accept-Encoding", "gzip");
            ctx.Response.AddHeader("User-Agent", "myglassapps (gzip)");
            ctx.Response.StatusCode = 200;
            ctx.Response.StatusDescription = "OK";
            ctx.Response.End();
        }

        private void handleShare(HttpContext ctx, Notification mirror, String requestPayload)
        {
            try
            {
                // access the credential state
                IAuthorizationState credentialState = DAL.RetrieveCredentialsByRequestIdAndUserToken(mirror.UserToken, mirror.VerifyToken);
                
                // get auth code from the verify token
                String authCode = DAL.RetrieveAuthCodeByRequestId(mirror.VerifyToken);     
                
                // we can reuse the access token
                IAuthenticator credentials = Authorization.GetAuthenticatorFromState(credentialState);
                
                // is there an attachment?
                Attachment attachment = Mirror.getTimelineItem(Mirror.BuildService(credentials), mirror.ItemId).Attachments[0];

                // only do this if post doesn't exsist 
                Stream fileStream = Mirror.DownloadAttachment(Mirror.BuildService(credentials), attachment);

                // create a new image and save it to the server
                PostManager pm = makeImage(fileStream, mirror);

                // create an image payload for re-creation
                String imagePayload = "data:image/jpeg;base64," + makeImageBase64(pm.postImageLocation);

                // create the response that will go to blogger
                ViewManager bloggerViewHandler = new ViewManager();
                bloggerViewHandler.AddView("bloggerImage", "~/views/bloggerImageView.html");
                bloggerViewHandler.Arguments = new Dictionary<String, String>()
                {
                    {"IMAGE_WEB_URI",pm.postImageWebURI}
                };
                String bloggerResponse = bloggerViewHandler.RenderView("bloggerImage");

                // get active blogger link
                BlogLink bl = DAL.GetActiveBlogLinkByUserId(mirror.UserToken);
                
                // create blogger post instance
                Post postInsertRequest = new Post();

                // create timeline post instance
                TimelineItem timelineItem = new TimelineItem();
                
                try
                {
                    // use post insert request to handle items
                    postInsertRequest = Blogger.insertPost(Blogger.BuildService(credentials), bl, bloggerResponse); 
                    ViewManager timlineViewHandler = new ViewManager();
                    timlineViewHandler.AddView("timelineImage", "~/views/insertTimelinePhotoOverlay.html");
                    timlineViewHandler.Arguments = new Dictionary<String, String>()
                    {
                        {"IMAGE_WEB_URI", pm.postImageWebURI},
                        {"BLOG_NAME", bl.blogName}   
                    };
                    timelineItem = Mirror.insertTimelineItem(Mirror.BuildService(credentials), timlineViewHandler.RenderView("timelineImage"));         
                }
                catch (Exception ex)
                {

                    DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, ex.ToString());
                    postInsertRequest.Title = "FAILED";
                    postInsertRequest.Id    = "FAILED";
                    postInsertRequest.Url   = "FAILED";

                    String errorTitle = "Unable to Post";
                    String errorDesc  = "This blog has exceeded its posting limit for the day";
                    String errorAdditional = "please share again tomorrow";

                    ViewManager errorInsertTimeline = new ViewManager();
                    errorInsertTimeline.AddView("timelineError", "~/views/timelineErrorView.html");
                    errorInsertTimeline.Arguments = new Dictionary<String, String>() {
                           {"ERROR_TITLE", errorTitle},
                           {"ERROR_DESC",errorDesc},
                           {"ERROR_ADDITIONAL", errorAdditional},
                    };

                    timelineItem = Mirror.insertTimelineItem(Mirror.BuildService(credentials), errorInsertTimeline.RenderView("timelineError"), true);  
                }

                // submit to post_manager
                pm.postImageContent = imagePayload;
                pm.blogLinkId = bl.blogId;
                pm.postTitle = postInsertRequest.Title;
                pm.postId = postInsertRequest.Id;
                pm.postContent = bloggerResponse;
                pm.userId = mirror.UserToken;
                pm.eTagId = timelineItem.ETag;
                pm.itemId = timelineItem.Id;

                // insert post manager
                DAL.InsertPostManager(pm);

                return;
            }
            catch (Exception ex)
            {
                String additionalData = "";
                additionalData += "|Function: handleShare()";
                additionalData += "|Exception: " + ex.ToString();
                additionalData += "|Request Payload: " + requestPayload;
                DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, additionalData);
                return;
            }           
        }

        private void handlePayload(HttpContext ctx, Notification mirror, String requestPayload)
        {
            switch (mirror.UserActions[0].Payload)
	        {
                case "REMOVE_BLOG_CONTENT":
                    DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, mirror.UserActions[0].Payload + " mirror.operation");
                    handleRemoveBlogContent(ctx, mirror, requestPayload);
                    break;
                case "GET_COMMENTS":
                    DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, mirror.UserActions[0].Payload  + " mirror.operation");
                    handleGetComments(ctx, mirror, requestPayload);
                    break;
	        }              
        }
        
        private void handleGetComments(HttpContext ctx, Notification mirror, String requestPayload)
        {
            // make new timeline item
            TimelineItem timelineItem = new TimelineItem();

            // access the credential state
            IAuthorizationState credentialState = DAL.RetrieveCredentialsByRequestIdAndUserToken(mirror.UserToken, mirror.VerifyToken);

            // get auth code from the verify token
            String authCode = DAL.RetrieveAuthCodeByRequestId(mirror.VerifyToken);

            // we can reuse the access token
            IAuthenticator credentials = Authorization.GetAuthenticatorFromState(credentialState);

            // get active blogger link
            BlogLink bl = DAL.GetActiveBlogLinkByUserId(mirror.UserToken);
            
            try
            {
                // set up the view manager
                ViewManager bloggerViewManager = new ViewManager();
                bloggerViewManager.AddView("bloggerPostCover", "~/views/bloggerPostingBundleCover.html");
                bloggerViewManager.AddView("bloggerComment", "~/views/bloggerComment.html");
            
                // take the top 3 recent posts
                PostList postList = Blogger.getPosts(Blogger.BuildService(credentials), bl);

                // list of html pages
                StringBuilder bundleCover = new StringBuilder();

                // html comment pages
                List<HtmlComment> htmlPages = new List<HtmlComment>();

                int totalCommentCount = 0;
                // build bundle cover - limit to constant recent_post_count init above
                for (int postIndex = 0; postIndex < RECENT_POST_COUNT; postIndex++)
                {
                    // if the index is more than the amount of posts, just break out.
                    if (postIndex >= postList.Items.Count)
                        break;

                    // get post
                    Post post = postList.Items[postIndex];
                    // get comments for post
                    CommentList commentList = Blogger.getComments(Blogger.BuildService(credentials), bl, post.Id);   
             
                    // get the count
                    int commentCount = (commentList.Items == null) ? 0 : commentList.Items.Count;

                    // build individual comment view
                    if (commentCount > 0)
                    {
                        // go through each comment and make some pages
                        foreach (Comment comment in commentList.Items)
	                    {
                            // new argument list
		                    bloggerViewManager.Arguments = new Dictionary<String, String>() 
                            { 
                                { "PROFILE_URL",comment.Author.Image.Url},
                                { "POST_TITLE",post.Title},
                                { "COMMENT_CONTENT",comment.Content},
                                { "COMMENT_AUTHOR",comment.Author.DisplayName},
                                { "COMMENT_TIME",comment.Published},
                                { "BLOG_NAME",bl.blogName}
                            };

                            // render view and add it to the html page list
                            htmlPages.Add(new HtmlComment() { html = bloggerViewManager.RenderView("bloggerComment"), text =  comment.Content, time = comment.Published });
	                    }
                        totalCommentCount += commentCount;
                    }
                }

                // make new argument dictionary for view before processing.
                bloggerViewManager.Arguments = new Dictionary<String, String>()
                {
                    {"POST_COUNT",totalCommentCount.ToString()}
                };


                timelineItem = Mirror.insertCommentBundleTimelineCard(Mirror.BuildService(credentials), bloggerViewManager.RenderView("bloggerPostCover"), htmlPages, mirror);
            }
            catch (Exception ex)
            {
                // notify user of exception
                StringBuilder response = new StringBuilder();
                DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, ex.ToString());
                response.Append("<article>\n  <section>\n    <div class=\"text-auto-size\" style=\"\">\n      <p class=\"red\">Unable to Retrieve Comments</p>\n      <p>Problem with Retrieving Comments</p>\n    </div>\n  </section>\n  <footer>\n    <div>please share again later</div>\n  </footer>\n</article>\n");
                timelineItem = Mirror.insertTimelineItem(Mirror.BuildService(credentials), response.ToString(), true);
                return;
            }
            return;
        }

        private void handleRemoveBlogContent(HttpContext ctx, Notification mirror, String requestPayload)
        {
            // create new string builder to hold the response
            StringBuilder response = new StringBuilder();

            // access the credential state
            IAuthorizationState credentialState = DAL.RetrieveCredentialsByRequestIdAndUserToken(mirror.UserToken, mirror.VerifyToken);

            // get auth code from the verify token
            String authCode = DAL.RetrieveAuthCodeByRequestId(mirror.VerifyToken);

            // we can reuse the access token
            IAuthenticator credentials = Authorization.GetAuthenticatorFromState(credentialState);

            // get active blogger link
            BlogLink bl = DAL.GetActiveBlogLinkByUserId(mirror.UserToken);

            // get the speakable text
            TimelineItem timelineItem = Mirror.getTimelineItem(Mirror.BuildService(credentials), mirror.ItemId);

            try
            {
                // the inreply to is the specific post we are dealing with
                PostManager pm = DAL.GetPostManager(mirror, mirror.ItemId);

                if (pm.itemId != "-1")
                {
                    // remove the post from blogger
                    Blogger.deletePost(Blogger.BuildService(credentials), bl, pm);
                    // remove the post from the service
                    DAL.RemovePostManagerEntry(pm);
                    // remove the timeline item
                    Mirror.DeleteTimelineItem(Mirror.BuildService(credentials), pm.itemId);
                }
            }
            catch (Exception ex)
            {
                // notify user of exception
                DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, ex.ToString());
                response.Append("<article>\n  <section>\n    <div class=\"text-auto-size\" style=\"\">\n      <p class=\"red\">Unable to Remove Post</p>\n      <p>Problem with Removing Post</p>\n    </div>\n  </section>\n  <footer>\n    <div>please share again later</div>\n  </footer>\n</article>\n");
                timelineItem = Mirror.insertTimelineItem(Mirror.BuildService(credentials), response.ToString(), true);
                return;
            }
            return;
        }

        private void handleReply(HttpContext ctx, Notification mirror, String requestPayload)
        {
            // create new string builder to hold the response
            StringBuilder response = new StringBuilder();

            // access the credential state
            IAuthorizationState credentialState = DAL.RetrieveCredentialsByRequestIdAndUserToken(mirror.UserToken, mirror.VerifyToken);

            // get auth code from the verify token
            String authCode = DAL.RetrieveAuthCodeByRequestId(mirror.VerifyToken);

            // we can reuse the access token
            IAuthenticator credentials = Authorization.GetAuthenticatorFromState(credentialState);

            // get the speakable text
            TimelineItem timelineItem = Mirror.getTimelineItem(Mirror.BuildService(credentials), mirror.ItemId);

            // get the new title and in reply to
            String newTitle = timelineItem.Text;
            String inReplyTo = timelineItem.InReplyTo;

            // remove the extra timline card
            Mirror.DeleteTimelineItem(Mirror.BuildService(credentials), timelineItem.Id);

            // get active blogger link
            BlogLink bl = DAL.GetActiveBlogLinkByUserId(mirror.UserToken);

            // we need to determine if this came from our post toolbox or not
            PostToolbox postToolbox = DAL.GetPostToolbox(bl.userId);

            if (postToolbox.itemId == inReplyTo)
            // it's a new post request from the toolbox
            {
                // new posting
                String newPostText = newTitle;

                // post manager
                PostManager pm = new PostManager();

                // create blogger post instance
                Post postInsertRequest = new Post();

                try
                {
                    // append brand
                    newPostText += "<code><br />Posted From Glass</code>";

                    // use post insert request to handle items
                    postInsertRequest = Blogger.insertPost(Blogger.BuildService(credentials), bl, newPostText);

                    // create the response that will go to glass
                    ViewManager bloggerViewHandler = new ViewManager();
                    bloggerViewHandler.AddView("bloggerText", "~/views/bloggerTextPost.html");
                    bloggerViewHandler.Arguments = new Dictionary<String, String>()
                    {
                         {"BLOG_NAME",bl.blogName},
                         {"BLOG_TEXT",newPostText} 
                    };                  
                    timelineItem = Mirror.insertTimelineItem(Mirror.BuildService(credentials),   bloggerViewHandler.RenderView("bloggerText"));   
                }
                catch (Exception ex)
                {
                    // notify user of exception
                    DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, ex.ToString());
                    response.Append("<article>\n  <section>\n    <div class=\"text-auto-size\" style=\"\">\n      <p class=\"red\">Unable to Insert Text Post</p>\n      <p>Problem with Inserting Text Post</p>\n    </div>\n  </section>\n  <footer>\n    <div>please share again later</div>\n  </footer>\n</article>\n");
                    timelineItem = Mirror.insertTimelineItem(Mirror.BuildService(credentials), response.ToString(), true);               
                }

                // submit to post_manager
                pm.postImageLocation    = "TEXT_ONLY";
                pm.postImageWebURI      = "TEXT_ONLY";
                pm.postImageContent     = "TEXT_ONLY";
                pm.blogLinkId = bl.blogId;
                pm.postTitle = DAL.scrubApos(postInsertRequest.Title);
                pm.postId = postInsertRequest.Id;
                pm.postContent = DAL.scrubApos(newPostText);
                pm.userId = mirror.UserToken;
                pm.eTagId = timelineItem.ETag;
                pm.itemId = timelineItem.Id;

                // insert post manager
                DAL.InsertPostManager(pm);

            }
            else
            // it's a title request
            {
         
                // create blogger post instance
                Post postPatchRequest = new Post();

                try
                {
                    // the inreply to is the specific post we are dealing with
                    PostManager pm = DAL.GetPostManager(mirror, inReplyTo);

                    if (pm.itemId != "-1")
                    {
                        // use post insert request to handle items
                        postPatchRequest = Blogger.updatePostTitle(Blogger.BuildService(credentials), bl, pm, newTitle);
                        // set post manager title to new title
                        pm.postTitle = DAL.scrubApos(newTitle);
                        pm.itemId = inReplyTo;
                        // update the post manager database
                        DAL.UpdatePostManagerTitle(pm);
                    }
                }
                catch (Exception ex)
                {
                    // notify user of exception
                    DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, ex.ToString());
                    response.Append("<article>\n  <section>\n    <div class=\"text-auto-size\" style=\"\">\n      <p class=\"red\">Unable to Update</p>\n      <p>Problem with Updating Title Text</p>\n    </div>\n  </section>\n  <footer>\n    <div>please share again later</div>\n  </footer>\n</article>\n");
                    timelineItem = Mirror.insertTimelineItem(Mirror.BuildService(credentials), response.ToString(), true);
                }
            }

            return;
        }

        private String makeImageBase64(String path)
        {
            String base64 = "";
            Image timelineImage = Image.FromFile(path);

            using (MemoryStream ms = new MemoryStream())
            {
                timelineImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                byte[] imageBytes = ms.ToArray();

                base64 = Convert.ToBase64String(imageBytes);

                return base64;
            }
        }

        private PostManager makeImage(Stream fs, Notification mirror) {
            String userImagePath = "";
            try
            {
                // map the server path
                var path = System.Web.HttpContext.Current.Server.MapPath("~/processed");
                userImagePath = path + "\\" + mirror.UserToken;

                if (!Directory.Exists(userImagePath))
                {
                    Directory.CreateDirectory(userImagePath);
                }

                Image timelineImage = Image.FromStream(fs,true);
                
                String fileName = mirror.ItemId; 
                // append file name
                userImagePath += "\\" + fileName + ".jpeg";

                timelineImage.Save(userImagePath);

            }
            catch (Exception ex)
            {
                String additionalData = "";
                additionalData += "|Function: makeImage()";
                additionalData += "|Exception: " + ex.ToString();
                additionalData += "|userImagePath: " + userImagePath;
                DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, additionalData);
            }

            return new PostManager()
            {
                postImageLocation = userImagePath,
                postImageWebURI = BASE_URI + "/processed/" + mirror.UserToken + "/" + mirror.ItemId + ".jpeg"
            };
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
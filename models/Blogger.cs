using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;
using Google.Apis.Services;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Google;
using Google.Apis.Authentication;

namespace myGlassApps.models
{
    public class Blogger
    {
        /// <summary>
        /// Build a Blogger service object.
        /// </summary>
        /// <param name="credentials">OAuth 2.0 credentials.</param>
        /// <returns>Mirror service object.</returns>
        public static BloggerService BuildService(IAuthenticator credentials)
        {
            // create base client service initializer
            BaseClientService.Initializer baseClientService = new BaseClientService.Initializer() { Authenticator = credentials };

            return new BloggerService(baseClientService);
        }

        public static BlogList getBlogs(BloggerService service, String userId)
        {
            BlogsResource.ListByUserRequest blogListByUserAction = service.Blogs.ListByUser("self");

            BlogList list = blogListByUserAction.Fetch();
           
            return list;
        }

        public static CommentList getComments(BloggerService service, BlogLink blogLink, String postId)
        {
            CommentsResource.ListRequest commentsListRequest = null;
            try
            {
               commentsListRequest = new CommentsResource.ListRequest(service, blogLink.blogId, postId);
            }
            catch (Exception ex)
            {
                DAL.InsertAccessLog(blogLink.blogName, blogLink.userId, ex.ToString());
            }
           
            return commentsListRequest.Fetch();
        }

        public static PostList getPosts(BloggerService service, BlogLink blogLink)
        {
            PostsResource.ListRequest postListRequest = service.Posts.List(blogLink.blogId);
            return postListRequest.Fetch();
        }

        public static Post insertPost(BloggerService service, BlogLink blogLink, String content)
        {
            Post postContent = new Post();

            postContent.Title = "#throughglass";
            postContent.Content = content;
            postContent.Labels = new List<String>() { "throughglass" };

            PostsResource prInsertAction = service.Posts;

            return prInsertAction.Insert(postContent, blogLink.blogId).Fetch();
        }

        public static void deletePost(BloggerService service, BlogLink blogLink, PostManager postManager)
        {
            PostsResource prDeleteAction = service.Posts;

            prDeleteAction.Delete(blogLink.blogId, postManager.postId).Fetch();
        }

        public static Post updatePostTitle(BloggerService service, BlogLink blogLink, PostManager postManager, String content)
        {
            Post patchContent = new Post();

            patchContent.Title   = content;

            patchContent.Content = postManager.postContent;

            patchContent.Id      = postManager.postId;

            patchContent.Labels = new List<String>() { "throughglass" };

            PostsResource.PatchRequest prPatchRequest = service.Posts.Patch(patchContent, blogLink.blogId, postManager.postId);           

            return prPatchRequest.Fetch();
        }


    }
}
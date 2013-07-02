using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace myGlassApps.models
{
    public class PostManager
    {
        public string eTagId { get; set; }
        public string blogLinkId { get; set; }
        public string userId { get; set; }
        public string postId { get; set; }
        public string postTitle { get; set; }
        public string postContent { get; set; }
        public string postImageContent { get; set; }
        public string postImageLocation { get; set; }
        public string postImageWebURI { get; set; }
        public string itemId { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace myGlassApps.models
{
    public class BlogLink
    {
        public String userId { get; set; }
        public String contactId { get; set; }
        public String subscriptionId { get; set; }
        public String blogId { get; set; }
        public String blogName { get; set; }
        public String source { get; set; }
        public bool isActive { get; set; }
    }
}
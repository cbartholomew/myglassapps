using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Web;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Google;
using Google.Apis.Authentication;
using Google.Apis.Mirror.v1;
using Google.Apis.Mirror.v1.Data;
using Google.Apis.Services;
namespace myGlassApps.models
{
    public class Mirror
    {
        /// <summary>
        /// Build a Mirror service object.
        /// </summary>
        /// <param name="credentials">OAuth 2.0 credentials.</param>
        /// <returns>Mirror service object.</returns>
        public static MirrorService BuildService(IAuthenticator credentials)
        {            
            // create base client service initializer
            BaseClientService.Initializer baseClientService = new BaseClientService.Initializer() { Authenticator = credentials };

            return new MirrorService(baseClientService);
        }
        
        /// <summary>
        /// Print a timeline item's metadata.
        /// </summary>
        /// <param name="service">Mirror service instance.</param>
        /// <param name="itemId">ID of the timeline item to print metadata for.</param>
        public static TimelineItem getTimelineItem(MirrorService service, String itemId)
        {
            TimelineItem item = null;
            try
            {
                item = service.Timeline.Get(itemId).Fetch();
            }
            catch (GoogleApiRequestException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.Unauthorized)
                {
                    // Credentials have been revoked.
                    // TODO: Redirect the user to the authorization URL and/or remove the
                    //       credentials from the database.
                    //throw new NotImplementedException();
                }
            }
            return item;
        }

        public static TimelineItem insertCommentBundleTimelineCard(MirrorService service, String bundleCover, List<HtmlComment> htmlPages, Notification mirror)
        {
            String bundleId = new Random().Next().ToString();

            TimelineItem bundleItem = new TimelineItem();
            try
            {
                // build bundle
                bundleItem.Html = bundleCover;
                bundleItem.IsBundleCover = true;
                bundleItem.BundleId = bundleId;

                // add notification
                bundleItem.Notification = new NotificationConfig() { Level = "DEFAULT" };

                // insert
                bundleItem = service.Timeline.Insert(bundleItem).Fetch();

                // sort the other way
                htmlPages.Reverse();

                // make child threads
                foreach (HtmlComment htmlPage in htmlPages)
                {
                    String childTimelineHtml = htmlPage.html;
                    String childTimelineText = htmlPage.text;

                    TimelineItem childTimelineItem = new TimelineItem();

                    childTimelineItem.Html = childTimelineHtml;
                    childTimelineItem.SpeakableText = childTimelineText;
                    childTimelineItem.BundleId = bundleId;
                    childTimelineItem.IsBundleCover = false; 
                    // add menu items
                    IList<MenuItem> childMenuItems = new List<MenuItem>();

                    // add menu options
                    MenuItem readAloudMi = new MenuItem() { Action = "READ_ALOUD" };
                    childMenuItems.Add(readAloudMi);
                    MenuItem deleteMi = new MenuItem() { Action = "DELETE" };
                    childMenuItems.Add(deleteMi);

                    // add to child bundle item menu
                    childTimelineItem.MenuItems = childMenuItems;

                    // insert the new timeline item
                    childTimelineItem = service.Timeline.Insert(childTimelineItem).Fetch();
                }

            }
            catch (GoogleApiRequestException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.Unauthorized)
                {
                    // Credentials have been revoked.
                    // TODO: Redirect the user to the authorization URL and/or remove the
                    //       credentials from the database.
                    //throw new NotImplementedException();
                }
            }

            return bundleItem;        
        }

        public static TimelineItem insertToolboxTimelineCard(MirrorService service, String body)
        {
            TimelineItem item = new TimelineItem();
            try
            {
                item.Html = body;
                // add menu items
                IList<MenuItem> menuItems = new List<MenuItem>();

                MenuItem replyMi = new MenuItem() { Action = "REPLY" };
                menuItems.Add(replyMi);

                MenuItem customMi = new MenuItem
                {
                    Action = "CUSTOM",
                    Id = "GET_COMMENTS",
                    Values = new List<MenuValue>() { 
                            new MenuValue(){ 
                                DisplayName = "Comments", IconUrl = "https://myglassapps.com/img/icons/send_to_glass_32x32_hover.png"
                            }
                        }
                };
                menuItems.Add(customMi);

                MenuItem pinMi = new MenuItem() { Action = "TOGGLE_PINNED" };
                menuItems.Add(pinMi);

                MenuItem deleteMi = new MenuItem() { Action = "DELETE" };
                menuItems.Add(deleteMi);

                item.MenuItems = menuItems;

                // add notification
                item.Notification = new NotificationConfig() { Level = "DEFAULT" };

                item = service.Timeline.Insert(item).Fetch();

            }
            catch (GoogleApiRequestException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.Unauthorized)
                {
                    // Credentials have been revoked.
                    // TODO: Redirect the user to the authorization URL and/or remove the
                    //       credentials from the database.
                    //throw new NotImplementedException();
                }
            }
            return item;        
        }

        /// <summary>
        /// Print a timeline item's metadata.
        /// </summary>
        /// <param name="service">Mirror service instance.</param>
        public static TimelineItem insertTimelineItem(MirrorService service, String body, Boolean isException = false)
        {
            TimelineItem item = new TimelineItem();   
            try
            {                
                     item.Html = body;                    
                     // add menu items
                     IList<MenuItem> menuItems = new List<MenuItem>();

                     if (!isException)
                     {
                         MenuItem replyMi = new MenuItem() { Action = "REPLY" };
                         menuItems.Add(replyMi);

                         MenuItem customMi = new MenuItem
                         {
                             Action = "CUSTOM",
                             Id = "REMOVE_BLOG_CONTENT",
                             Values = new List<MenuValue>() { 
                             new MenuValue(){ 
                                 DisplayName = "Remove Post", IconUrl = "https://myglassapps.com/img/blogger_64.png"
                             }
                         }
                         };
                         menuItems.Add(customMi);
                     }

                     MenuItem deleteMi = new MenuItem() { Action = "DELETE" };
                     menuItems.Add(deleteMi);

                     item.MenuItems = menuItems;   
                         
                     // add notification
                     item.Notification = new NotificationConfig() { Level = "DEFAULT" };

                     item = service.Timeline.Insert(item).Fetch();

            }
            catch (GoogleApiRequestException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.Unauthorized)
                {
                    // Credentials have been revoked.
                    // TODO: Redirect the user to the authorization URL and/or remove the
                    //       credentials from the database.
                    //throw new NotImplementedException();
                }
            }
            return item;
        }

        /// <summary>
        /// Delete a timeline item.
        /// </summary>
        /// <param name='service'>Authorized Mirror service.</param>
        /// <param name='itemId'>ID of the timeline item to delete.</param>
        public static void DeleteTimelineItem(MirrorService service,
            String itemId)
        {
            try
            {
                service.Timeline.Delete(itemId).Fetch();
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occurred: " + e.Message);
            }
        }

        /// <summary>
        /// Download a timeline items's attachment.
        /// </summary>
        /// <param name="service">Authorized Mirror service.</param>
        /// <param name="attachment">Attachment to download content for.</param>
        /// <returns>Attachment's content if successful, null otherwise.</returns>
        public static System.IO.Stream DownloadAttachment(
            MirrorService service, Attachment attachment)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                  new Uri(attachment.ContentUrl));
                service.Authenticator.ApplyAuthenticationToRequest(request);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response.GetResponseStream();
                }
                else
                {
                    Console.WriteLine(
                      "An error occurred: " + response.StatusDescription);
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
                return null;
            }
        }

          /// <summary>
          /// Print some timeline item metadata information.
          /// </summary>
          /// <param name='service'>Authorized Mirror service.</param>
          /// <param name='itemId'>
          /// ID of the timeline item to print metadata information for.
          /// </param>
       public static void PrintTimelineItemMetadata(MirrorService service,
      String itemId) {
    try {
      TimelineItem timelineItem = service.Timeline.Get(itemId).Fetch();

      Console.WriteLine("Timeline item ID: " + timelineItem.Id);
      if (timelineItem.IsDeleted.HasValue && timelineItem.IsDeleted.Value) {
        Console.WriteLine("Timeline item has been deleted");
      } else {
        Contact creator = timelineItem.Creator;
        if (creator != null) {
          Console.WriteLine("Timeline item created by " + creator.DisplayName);
        }
        Console.WriteLine("Timeline item created on " + timelineItem.Created);
        Console.WriteLine(
            "Timeline item displayed on " + timelineItem.DisplayTime);
        String inReplyTo = timelineItem.InReplyTo;
        if (!String.IsNullOrEmpty(inReplyTo)) {
          Console.WriteLine("Timeline item is a reply to " + inReplyTo);
        }
        String text = timelineItem.Text;
        if (!String.IsNullOrEmpty(text)) {
          Console.WriteLine("Timeline item has text: " + text);
        }
        foreach (Contact contact in timelineItem.Recipients) {
          Console.WriteLine("Timeline item is shared with: " + contact.Id);
        }
        NotificationConfig notification = timelineItem.Notification;
        if (notification != null) {
          Console.WriteLine(
              "Notification delivery time: " + notification.DeliveryTime);
          Console.WriteLine("Notification level: " + notification.Level);
        }
        // See mirror.timeline.attachments.get to learn how to download the
        // attachment's content.
        foreach (Attachment attachment in timelineItem.Attachments) {
          Console.WriteLine("Attachment ID: " + attachment.Id);
          Console.WriteLine("  > Content-Type: " + attachment.ContentType);
        }
      }
    } catch (Exception e) {
      Console.WriteLine("An error occurred: " + e.Message);
    }
  }


        /// <summary>
        /// Insert a new contact for the current user.
        /// </summary>
        /// <param name='service'>Authorized Mirror service.</param>
        /// <param name='contactId'>ID of the contact to insert.</param>
        /// <param name='displayName'>
        /// Display name for the contact to insert.
        /// </param>
        /// <param name='iconUrl'>URL of the contact's icon.</param>
        /// <returns>
        /// The inserted contact on success, null otherwise.
        /// </returns>
        public static Contact insertContact(MirrorService service,
            Contact contact)
        {
            try
            {
                return service.Contacts.Insert(contact).Fetch();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Print information for a contact.
        /// </summary>
        /// <param name='service'>Authorized Mirror service</param>
        /// <param name='contactId'>
        /// ID of the Contact to print information for.
        /// </param>
        public static void printContact(MirrorService service,
            String contactId)
        {
            try
            {
                Contact contact = service.Contacts.Get(contactId).Fetch();

                Console.WriteLine(
                    "Contact displayName: " + contact.DisplayName);
                if (contact.ImageUrls != null)
                {
                    foreach (String imageUrl in contact.ImageUrls)
                    {
                        Console.WriteLine("Contact imageUrl: " + imageUrl);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }

        /// <summary>
        /// Print all contacts for the current user.
        /// </summary>
        /// <param name='service'>Authorized Mirror service.</param>
        public static void findSharedContactAndRemove(MirrorService service, String contactId)
        {
            try
            {
                ContactsListResponse contacts =
                    service.Contacts.List().Fetch();

                if (contacts.Items.Count > 0)
                {
                    foreach (Contact contact in contacts.Items)
                    {
                        if (contact.Id == contactId)
                            deleteContact(service, contactId);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }

        /// <summary>
        /// Delete a contact for the current user.
        /// </summary>
        /// <param name='service'>Authorized Mirror service.</param>
        /// <param name='contactId'>ID of the Contact to delete.</param>
        public static void deleteContact(MirrorService service,
            String contactId)
        {
            try
            {
                service.Contacts.Delete(contactId).Fetch();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }

        public static Subscription buildBloggerSubscription(String userToken, String verfiyToken, String blogId )
        {
            String baseUrl = "https://myglassapps.com/";

            Notification notification = new Notification(){
                UserActions = new List<UserAction>() { 
                    new UserAction() {
                        Payload = blogId
                    }
                }
            };

            return new Subscription()
            {
                CallbackUrl = baseUrl + "notification.ashx",
                Collection = "timeline",
                UserToken = userToken,
                VerifyToken = verfiyToken,
                Notification = notification               
            };
        }

        /// <summary>
        /// Subscribe to notifications for the current user.
        /// </summary>
        /// <param name='service'>Authorized Mirror service.</param>
        /// <param name='collection'>
        /// Collection to subscribe to (supported values are "timeline" and
        /// "locations").
        /// </param>
        /// <param name='userToken'>
        /// Opaque token used by the Glassware to identify the user the
        /// notification pings are sent for (recommended).
        /// </param>
        /// <param name='verifyToken'>
        /// Opaque token used by the Glassware to verify that the notification
        /// pings are sent by the API (optional).
        /// </param>
        /// <param name='callbackUrl'>
        /// URL receiving notification pings (must be HTTPS).
        /// </param>
        /// <param name='operation'>
        /// List of operations to subscribe to. Valid values are "UPDATE", "INSERT"
        /// and "DELETE" or {@code null} to subscribe to all.
        /// </param>
        public static Subscription SubscribeToNotifications(MirrorService service,
           Subscription subscription)
        {           
            try
            {
                subscription = service.Subscriptions.Insert(subscription).Fetch();
            }
            catch (Exception)
            {
                subscription.Id = "-1";                
            }
            return subscription;
        }


        /// <summary>
        /// Delete a subscription to a collection.
        /// </summary>
        /// <param name='service'>Authorized Mirror service.</param>
        /// <param name='collection'>
        /// Collection to unsubscribe from (supported values are "timeline" and
        /// "locations").
        /// </param>
        public static void UnsubscribeFromNotifications(MirrorService service,
            String collection)
        {
            try
            {
                service.Subscriptions.Delete(collection).Fetch();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }
    }
}
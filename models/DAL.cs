using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using DotNetOpenAuth.OAuth2;
using Google.Apis.Mirror.v1;
using Google.Apis.Mirror.v1.Data;
using System.Net;

namespace myGlassApps.models
{
    public class DAL
    {

        //public const String DATA_SOURCE = "MIRRORAPIPROD.db.4173543.hostedresource.com";
        //public const String CATALOG = "MIRRORAPIPROD";
        //public const String USER_ID = "MIRRORAPIPROD";
        //public const String PWD = "La1nd0nna@ma";

        public const String DATA_SOURCE = "MIRRORAPI.db.4173543.hostedresource.com";
        public const String CATALOG = "MIRRORAPI";
        public const String USER_ID = "MIRRORAPI";
        public const String PWD = "La1nd0nna@ma";

        public static void InsertToken(String userId,IAuthorizationState authState, String authCode)
        {
            String hostName = Dns.GetHostName();

            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog="+ CATALOG +"; User ID="+ USER_ID +"; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("INSERT_ACCESS_TOKEN", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@USER_ID", userId);
                myCommand.Parameters.AddWithValue("@ACCESS_TOKEN", authState.AccessToken);
                myCommand.Parameters.AddWithValue("@ACCESS_TOKEN_EXPIRATION_UTC", authState.AccessTokenExpirationUtc);
                myCommand.Parameters.AddWithValue("@ACCESS_TOKEN_ISSUE_UTC", authState.AccessTokenIssueDateUtc);
                myCommand.Parameters.AddWithValue("@REFRESH_TOKEN", authState.RefreshToken);
                myCommand.Parameters.AddWithValue("@CALLBACK", authState.Callback.AbsoluteUri);
                myCommand.Parameters.AddWithValue("@HOST_NAME", hostName);
                myCommand.Parameters.AddWithValue("@AUTH_CODE", authCode);

                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myConnection.Close();
            }
        }

        public static AuthorizationState RetrieveCredentials(String userId)
        {
            AuthorizationState authState = new AuthorizationState();

            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("GET_STORED_CREDENTIALS", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@USER_ID", userId);

                myConnection.Open();
                
                SqlDataReader reader = myCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        authState.AccessToken = reader["ACCESS_TOKEN"].ToString();
                        authState.AccessTokenExpirationUtc = Convert.ToDateTime(reader["ACCESS_TOKEN_EXPIRATION_UTC"]);
                        authState.AccessTokenIssueDateUtc = Convert.ToDateTime(reader["ACCESS_TOKEN_ISSUE_UTC"]);
                        authState.RefreshToken = reader["REFRESH_TOKEN"].ToString();
                        authState.Callback = new Uri(reader["CALLBACK"].ToString());
                    }
                }
                myConnection.Close();
            }
            return authState;
        }

        public static AuthorizationState RetrieveCredentialsByAuthCode(String authCode)
        {
            AuthorizationState authState = new AuthorizationState();

            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("GET_STORED_CREDENTIALS_BY_AUTHCODE", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@AUTH_CODE", authCode);
                
                myConnection.Open();

                SqlDataReader reader = myCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        authState.AccessToken = reader["ACCESS_TOKEN"].ToString();
                        authState.AccessTokenExpirationUtc = Convert.ToDateTime(reader["ACCESS_TOKEN_EXPIRATION_UTC"]);
                        authState.AccessTokenIssueDateUtc = Convert.ToDateTime(reader["ACCESS_TOKEN_ISSUE_UTC"]);
                        authState.RefreshToken = reader["REFRESH_TOKEN"].ToString();
                        authState.Callback = new Uri(reader["CALLBACK"].ToString());
                    }
                }
                myConnection.Close();
            }
            return authState;
        }

        public static AuthorizationState RetrieveCredentialsByRequestIdAndUserToken(String userId, String requestId)
        {
            AuthorizationState authState = new AuthorizationState();

            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("GET_STORED_CREDENTIALS_BY_REQUEST_ID", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@USER_ID", userId);
                myCommand.Parameters.AddWithValue("@REQUEST_ID", requestId);

                myConnection.Open();

                SqlDataReader reader = myCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        authState.AccessToken = reader["ACCESS_TOKEN"].ToString();
                        authState.AccessTokenExpirationUtc = Convert.ToDateTime(reader["ACCESS_TOKEN_EXPIRATION_UTC"]);
                        authState.AccessTokenIssueDateUtc = Convert.ToDateTime(reader["ACCESS_TOKEN_ISSUE_UTC"]);
                        authState.RefreshToken = reader["REFRESH_TOKEN"].ToString();
                        authState.Callback = new Uri(reader["CALLBACK"].ToString());
                    }
                }
                myConnection.Close();
            }
            return authState;
        }

        public static String RetrieveUserIdByAuthCode(String authCode)
        {
            String userId = "";
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("GET_USER_ID_BY_AUTHCODE", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@AUTH_CODE", authCode);

                myConnection.Open();

                SqlDataReader reader = myCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        userId = reader["USER_ID"].ToString();
                        break;
                    }
                }
                myConnection.Close();
            }
            return userId;
        }

        public static String RetrieveRequestIdByAuthCode(String authCode)
        {
            String requestId = "";
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("GET_REQUEST_ID_BY_AUTHCODE", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@AUTH_CODE", authCode);

                myConnection.Open();

                SqlDataReader reader = myCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        requestId = reader["REQUEST_ID"].ToString();
                        break;
                    }
                }
                myConnection.Close();
            }
            return requestId;
        }

        public static String RetrieveAuthCodeByRequestId(String requestId)
        {
            String authCode = "";
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("GET_AUTHCODE_BY_REQUEST_ID", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@REQUEST_ID", authCode);

                myConnection.Open();

                SqlDataReader reader = myCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        authCode = reader["AUTH_CODE"].ToString();
                        break;
                    }
                }
                myConnection.Close();
            }
            return authCode;
        }

        public static Contact GetContactType()
        {
            Contact contact = new Contact();
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("GET_CONTACT_TYPE", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@IS_ACTIVE", true);

                myConnection.Open();

                SqlDataReader reader = myCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        contact.Id = reader["CONTACT_ID"].ToString();
                        contact.DisplayName = reader["CONTACT_NAME"].ToString();
                        contact.ImageUrls = new List<string>() { reader["IMAGE_NAME"].ToString() };
                        contact.Type = "INDIVIDUAL";
                        contact.AcceptTypes = new List<string>() { "image/jpeg" };
                        contact.Priority = 0;
                        break;
                    }
                }
                myConnection.Close();
            }
            return contact;
        }

        public static void InsertIntoBlogLink(BlogLink blogLink)
        {
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("INSERT_BLOG_LINK", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@USER_ID", blogLink.userId);
                myCommand.Parameters.AddWithValue("@CONTACT_ID", blogLink.contactId);
                myCommand.Parameters.AddWithValue("@SUBSCRIPTION_ID", blogLink.subscriptionId);
                myCommand.Parameters.AddWithValue("@BLOG_ID", blogLink.blogId);
                myCommand.Parameters.AddWithValue("@BLOG_NAME", blogLink.blogName);
                myCommand.Parameters.AddWithValue("@SOURCE", blogLink.source);
                myCommand.Parameters.AddWithValue("@IS_ACTIVE", blogLink.isActive);

                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myConnection.Close();
            }
        
        }

        public static BlogLink GetActiveBlogLinkByUserId(String userId)
        {
            BlogLink blogLink = new BlogLink() { blogId = "-1" };

            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("GET_BLOG_ID_BY_USERID", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@USER_ID", userId);

                myConnection.Open();

                SqlDataReader reader = myCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        blogLink.userId = reader["USER_ID"].ToString();
                        blogLink.contactId = reader["CONTACT_ID"].ToString();
                        blogLink.subscriptionId = reader["SUBSCRIPTION_ID"].ToString();
                        blogLink.blogId = reader["BLOG_ID"].ToString();
                        blogLink.blogName = reader["BLOG_NAME"].ToString();
                        blogLink.source = reader["SOURCE"].ToString();
                        blogLink.isActive = Convert.ToBoolean(reader["IS_ACTIVE"]);
                    }
                }
                myConnection.Close();
            }
            return blogLink;
        }

        public static void DisableBlog(String userId, String blogId = null)
        {
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("DISABLE_BLOG_BY_USER_ID", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@USER_ID", userId);
                myCommand.Parameters.AddWithValue("@BLOG_ID", blogId);
                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myConnection.Close();
            }
        }

        public static void InsertAccessLog(String requestId, String userId, String requestPayload)
        {
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("INSERT_ACCESS_LOG", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@REQUEST_ID", requestId);
                myCommand.Parameters.AddWithValue("@USER_ID", userId);
                myCommand.Parameters.AddWithValue("@PAYLOAD", requestPayload);               
                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myConnection.Close();
            }
        }

        public static void InsertPostManager(PostManager postManager)
        {
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("INSERT_POST_MANAGER", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@E_TAG_ID", postManager.eTagId);
                myCommand.Parameters.AddWithValue("@BLOG_LINK_ID", postManager.blogLinkId);
                myCommand.Parameters.AddWithValue("@USER_ID", postManager.userId);
                myCommand.Parameters.AddWithValue("@POST_ID", postManager.postId);
                myCommand.Parameters.AddWithValue("@POST_TITLE", postManager.postTitle);
                myCommand.Parameters.AddWithValue("@POST_CONTENT", postManager.postContent);
                myCommand.Parameters.AddWithValue("@POST_IMAGE_CONTENT", postManager.postImageContent);
                myCommand.Parameters.AddWithValue("@POST_IMAGE_LOCATION", postManager.postImageLocation);
                myCommand.Parameters.AddWithValue("@POST_IMAGE_WEB_URI", postManager.postImageWebURI);
                myCommand.Parameters.AddWithValue("@ITEM_ID", postManager.itemId);

                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myConnection.Close();
            }
        }

        public static void UpdatePostManagerTitle(PostManager postManager)
        {
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("UPDATE POST_MANAGER SET POST_TITLE ='"+ postManager.postTitle +"' WHERE ITEM_ID = '" + postManager.itemId + "'", myConnection);
                myCommand.CommandType = CommandType.Text;
                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myConnection.Close();
            }
        }

        public static void RemovePostManagerEntry(PostManager postManager)
        {
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("DELETE FROM POST_MANAGER WHERE ITEM_ID = '" + postManager.itemId + "' AND USER_ID ='" + postManager.userId + "'", myConnection);
                myCommand.CommandType = CommandType.Text;
                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myConnection.Close();
            }
        }

        public static PostManager GetPostManager(Notification mirror, String itemId)
        {
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                PostManager pm = null;
                try
                {
                    SqlCommand myCommand  = new SqlCommand("SELECT * FROM POST_MANAGER WHERE ITEM_ID = '" + itemId + "'", myConnection);
                    SqlDataReader reader;
                    myCommand.CommandType = CommandType.Text;
                    myConnection.Open();
                    reader = myCommand.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            pm = new PostManager()
                            {
                                eTagId = reader["E_TAG_ID"].ToString(),
                                blogLinkId = reader["BLOG_LINK_ID"].ToString(),
                                userId = reader["USER_ID"].ToString(),
                                postId = reader["POST_ID"].ToString(),
                                postTitle = reader["POST_TITLE"].ToString(),
                                postContent = reader["POST_CONTENT"].ToString(),
                                postImageContent = reader["POST_IMAGE_CONTENT"].ToString(),
                                postImageLocation = reader["POST_IMAGE_LOCATION"].ToString(),
                                postImageWebURI = reader["POST_IMAGE_WEB_URI"].ToString()
                            };
                        }
                    }
                    myConnection.Close();
                }
                catch (Exception ex)
                {
                    DAL.InsertAccessLog(mirror.VerifyToken, mirror.UserToken, "Item Id:" + itemId + " Exception: " + ex.ToString());
                    pm = new PostManager()
                    {
                        itemId = "-1"
                    };
                }
                return pm;
            }
        }

        public static void InsertPostToolbox(String userId, String itemId, Boolean isActive) 
        {
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("INSERT_POST_TOOLBOX", myConnection);
                myCommand.CommandType = CommandType.StoredProcedure;

                // db will generate auto new id
                myCommand.Parameters.AddWithValue("@USER_ID", userId);
                myCommand.Parameters.AddWithValue("@ITEM_ID", itemId);
                myCommand.Parameters.AddWithValue("@IS_ACTIVE", isActive);

                myConnection.Open();
                myCommand.ExecuteNonQuery();
                myConnection.Close();
            }
        
        }

        public static PostToolbox GetPostToolbox(String userId)
        {
            PostToolbox pt = new PostToolbox();
            using (SqlConnection myConnection = new SqlConnection("Data Source=" + DATA_SOURCE + "; Initial Catalog=" + CATALOG + "; User ID=" + USER_ID + "; Password='" + PWD + "';"))
            {
                SqlCommand myCommand = new SqlCommand("SELECT * FROM dbo.POST_TOOLBOX (NOLOCK) WHERE USER_ID = '" +  userId + "'", myConnection);
                myCommand.CommandType = CommandType.Text;
                try
                {
                    myConnection.Open();
                    SqlDataReader reader = myCommand.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            pt.itemId = reader["ITEM_ID"].ToString();
                            pt.isActive = Convert.ToBoolean(reader["IS_ACTIVE"]);
                            pt.userId = reader["USER_ID"].ToString();

                            break;
                        }
                    }
                    else
                    {
                        pt.itemId = "-1";
                        pt.userId = "-1";
                        pt.isActive = false;
                    }
                }
                catch (Exception)
                {
                        pt.itemId = "-1";
                        pt.userId = "-1";
                        pt.isActive = false;
                        myConnection.Close();
                }
                myConnection.Close();
            }
            return pt;        
        }

        public static String scrubApos(String text) { 
            return text.Replace("'","&apos;");        
        }
    }
}
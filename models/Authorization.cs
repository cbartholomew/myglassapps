using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Google;
using Google.Apis;
using Google.Apis.Authentication;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Services;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using System.Collections.Specialized;
using System.Diagnostics;

namespace myGlassApps.models
{
    public class Authorization
    {
        static String PROD_URI = "https://myglassapps.com/main.ashx";
        static String TEST_URI = "http://localhost:8080/main.ashx";

        static String CLIENT_ID = "73341334822-n8emkr56dbl3k1g471dcf0h6cdql6cdt.apps.googleusercontent.com";
        static String CLIENT_SECRET = "";
        static String REDIRECT_URI = PROD_URI;
        static String[] SCOPES = new String[] {
        "https://www.googleapis.com/auth/glass.timeline",
        "https://www.googleapis.com/auth/userinfo.profile",
        "https://www.googleapis.com/auth/blogger",
        "https://www.blogger.com/feeds"
        };

        /// <summary>
        /// Exception thrown when an error occurred while retrieving credentials.
        /// </summary>
        public class GetCredentialsException : Exception
        {
            public String AuthorizationUrl { get; set; }

            /// <summary>
            /// Construct a GetCredentialsException.
            /// </summary>
            /// @param authorizationUrl The authorization URL to redirect the user to.
            public GetCredentialsException(String authorizationUrl)
            {
                this.AuthorizationUrl = authorizationUrl;
            }

        }

        /// <summary>
        /// Exception thrown when no refresh token has been found.
        /// </summary>
        public class NoRefreshTokenException : GetCredentialsException
        {

            /// <summary>
            /// Construct a NoRefreshTokenException.
            /// </summary>
            /// @param authorizationUrl The authorization URL to redirect the user to.
            public NoRefreshTokenException(String authorizationUrl)
                : base(authorizationUrl)
            {
            }

        }

        /// <summary>
        /// Exception thrown when a code exchange has failed.
        /// </summary>
        private class CodeExchangeException : GetCredentialsException
        {

            /// <summary>
            /// Construct a CodeExchangeException.
            /// </summary>
            /// @param authorizationUrl The authorization URL to redirect the user to.
            public CodeExchangeException(String authorizationUrl)
                : base(authorizationUrl)
            {
            }

        }

        /// <summary>
        /// Exception thrown when no user ID could be retrieved.
        /// </summary>
        private class NoUserIdException : Exception
        {
        }

        /// <summary>
        /// Extends the NativeApplicationClient class to allow setting of a custom IAuthorizationState.
        /// </summary>
        public class StoredStateClient : NativeApplicationClient
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StoredStateClient"/> class.
            /// </summary>
            /// <param name="authorizationServer">The token issuer.</param>
            /// <param name="clientIdentifier">The client identifier.</param>
            /// <param name="clientSecret">The client secret.</param>
            public StoredStateClient(AuthorizationServerDescription authorizationServer,
                String clientIdentifier,
                String clientSecret,
                IAuthorizationState state)
                : base(authorizationServer, clientIdentifier, clientSecret)
            {
                this.State = state;
            }

            public IAuthorizationState State { get; private set; }

            /// <summary>
            /// Returns the IAuthorizationState stored in the StoredStateClient instance.
            /// </summary>
            /// <param name="provider">OAuth2 client.</param>
            /// <returns>The stored authorization state.</returns>
            static public IAuthorizationState GetState(StoredStateClient provider)
            {
                return provider.State;
            }
        }

        /// <summary>
        /// Retrieve an IAuthenticator instance using the provided state.
        /// </summary>
        /// <param name="credentials">OAuth 2.0 credentials to use.</param>
        /// <returns>Authenticator using the provided OAuth 2.0 credentials</returns>
        public static IAuthenticator GetAuthenticatorFromState(IAuthorizationState credentials)
        {
            var provider = new StoredStateClient(GoogleAuthenticationServer.Description, CLIENT_ID, CLIENT_SECRET, credentials);
            var auth = new OAuth2Authenticator<StoredStateClient>(provider, StoredStateClient.GetState);
            auth.LoadAccessToken();
            return auth;
        }

        /// <summary>
        /// Retrieved stored credentials for the provided user ID.
        /// </summary>
        /// <param name="userId">User's ID.</param>
        /// <returns>Stored GoogleAccessProtectedResource if found, null otherwise.</returns>
        static IAuthorizationState GetStoredCredentials(String userId)
        {
            return DAL.RetrieveCredentials(userId);
        }

        /// <summary>
        /// Store OAuth 2.0 credentials in the application's database.
        /// </summary>
        /// <param name="userId">User's ID.</param>
        /// <param name="credentials">The OAuth 2.0 credentials to store.</param>
        static void StoreCredentials(String userId, IAuthorizationState credentials, String authCode)
        {
            DAL.InsertToken(userId, credentials, authCode);
        }

        /// <summary>
        /// Exchange an authorization code for OAuth 2.0 credentials.
        /// </summary>
        /// <param name="authorizationCode">Authorization code to exchange for OAuth 2.0 credentials.</param>
        /// <returns>OAuth 2.0 credentials.</returns>
        /// <exception cref="CodeExchangeException">An error occurred.</exception>
        static IAuthorizationState ExchangeCode(String authorizationCode)
        {
            var provider = new NativeApplicationClient(GoogleAuthenticationServer.Description, CLIENT_ID, CLIENT_SECRET);
            IAuthorizationState state = new AuthorizationState();
            state.Callback = new Uri(REDIRECT_URI);

            try
            {
                state = provider.ProcessUserAuthorization(authorizationCode, state);
                return state;
            }
            catch (ProtocolException)
            {
                throw new CodeExchangeException(null);
            }
        }

        /// <summary>
        /// Send a request to the UserInfo API to retrieve the user's information.
        /// </summary>
        /// <param name="credentials">OAuth 2.0 credentials to authorize the request.</param>
        /// <returns>User's information.</returns>
        /// <exception cref="NoUserIdException">An error occurred.</exception>
        static Userinfo GetUserInfo(IAuthorizationState credentials)
        {

            // create base client service initializer
            BaseClientService.Initializer baseClientService = new BaseClientService.Initializer() { Authenticator = GetAuthenticatorFromState(credentials) };
           
            Oauth2Service userInfoService = new Oauth2Service(baseClientService);
            Userinfo userInfo = null;
            try
            {
                userInfo = userInfoService.Userinfo.Get().Fetch();
            }
            catch (GoogleApiRequestException e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
            if (userInfo != null && !String.IsNullOrEmpty(userInfo.Id))
            {
                return userInfo;
            }
            else
            {
                throw new NoUserIdException();
            }
        }

        /// <summary>
        /// Retrieve the authorization URL.
        /// </summary>
        /// <param name="userId">User's Google ID.</param>
        /// <param name="state">State for the authorization URL.</param>
        /// <returns>Authorization URL to redirect the user to.</returns>
        public static String GetAuthorizationUrl(String userId, String state)
        {
            var provider = new NativeApplicationClient(GoogleAuthenticationServer.Description);
            provider.ClientIdentifier = CLIENT_ID;

            IAuthorizationState authorizationState = new AuthorizationState(SCOPES);
            authorizationState.Callback = new Uri(REDIRECT_URI);

            UriBuilder builder = new UriBuilder(provider.RequestUserAuthorization(authorizationState));
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(builder.Query);

            queryParameters.Set("access_type", "offline");
            queryParameters.Set("approval_prompt", "force");
            queryParameters.Set("user_id", userId);
            queryParameters.Set("state", state);

            builder.Query = queryParameters.ToString();
            return builder.Uri.ToString();
        }

        /// <summary>
        /// Retrieve credentials using the provided authorization code.
        ///
        /// This function exchanges the authorization code for an access token and
        /// queries the UserInfo API to retrieve the user's Google ID. If a
        /// refresh token has been retrieved along with an access token, it is stored
        /// in the application database using the user's Google ID as key. If no
        /// refresh token has been retrieved, the function checks in the application
        /// database for one and returns it if found or throws a NoRefreshTokenException
        /// with the authorization URL to redirect the user to.
        /// </summary>
        /// <param name="authorizationCode">Authorization code to use to retrieve an access token.</param>
        /// <param name="state">State to set to the authorization URL in case of error.</param>
        /// <returns>OAuth 2.0 credentials instance containing an access and refresh token.</returns>
        /// <exception cref="CodeExchangeException">
        /// An error occurred while exchanging the authorization code.
        /// </exception>
        /// <exception cref="NoRefreshTokenException">
        /// No refresh token could be retrieved from the available sources.
        /// </exception>
        public static IAuthenticator GetCredentials(String authorizationCode, String state)
        {
            String userId = "";

            try
            {
                IAuthorizationState credentials = ExchangeCode(authorizationCode);
                Userinfo userInfo = GetUserInfo(credentials);
                userId = userInfo.Id;
                state = userId;

                if (!String.IsNullOrEmpty(credentials.RefreshToken))
                {
                    StoreCredentials(userId, credentials, authorizationCode);
                    return GetAuthenticatorFromState(credentials);
                }
                else
                {
                    credentials = GetStoredCredentials(userId);
                    if (credentials != null && !String.IsNullOrEmpty(credentials.RefreshToken))
                    {
                        return GetAuthenticatorFromState(credentials);
                    }
                }
            }
            catch (CodeExchangeException e)
            {
                Console.WriteLine("An error occurred during code exchange.");
                // Glass services should try to retrieve the user and credentials for the current
                // session.
                // If none is available, redirect the user to the authorization URL.
                e.AuthorizationUrl = GetAuthorizationUrl(userId, state);
                //throw e;
            }
            catch (NoUserIdException)
            {
                Console.WriteLine("No user ID could be retrieved.");
            }
            // No refresh token has been retrieved.
            String authorizationUrl = GetAuthorizationUrl(userId, state);

            throw new NoRefreshTokenException(authorizationUrl);
        }

    }
}

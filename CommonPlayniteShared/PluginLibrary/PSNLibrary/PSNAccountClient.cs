﻿//using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using CommonPlayniteShared.PluginLibrary.PSNLibrary.Models;//using PSNLibrary.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Web;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Http.Headers;
using System.Security;

/*
using CommonPlayniteShared.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
//using PSNLibrary.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
*/

namespace CommonPlayniteShared.PluginLibrary.PSNLibrary
{
    public class ApiRedirectResponse
    {
        public string redirectUrl { get; set; }
        public string sid { get; set; }
    }
    public class PSNAccountClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI api;
        public MobileTokens mobileToken;//private MobileTokens mobileToken;
        //private readonly PSNLibrary library;
        private readonly string tokenPath;
        private const int pageRequestLimit = 100;
        private const string loginUrl = @"https://web.np.playstation.com/api/session/v1/signin?redirect_uri=https://io.playstation.com/central/auth/login%3FpostSignInURL=https://www.playstation.com/home%26cancelURL=https://www.playstation.com/home&smcid=web:pdc";
        private const string gameListUrl = "https://web.np.playstation.com/api/graphql/v1/op?operationName=getPurchasedGameList&variables={{\"isActive\":true,\"platform\":[\"ps3\",\"ps4\",\"ps5\"],\"start\":{0},\"size\":{1},\"subscriptionService\":\"NONE\"}}&extensions={{\"persistedQuery\":{{\"version\":1,\"sha256Hash\":\"2c045408b0a4d0264bb5a3edfed4efd49fb4749cf8d216be9043768adff905e2\"}}}}";
        private const string playedListUrl = "https://web.np.playstation.com/api/graphql/v1/op?operationName=getUserGameList&variables=%7B%22limit%22%3A100%2C%22categories%22%3A%22ps4_game%2Cps5_native_game%22%7D&extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%22e780a6d8b921ef0c59ec01ea5c5255671272ca0d819edb61320914cf7a78b3ae%22%7D%7D";
        private const string mobileCodeUrl = @"https://ca.account.sony.com/api/authz/v3/oauth/authorize?access_type=offline&client_id=ac8d161a-d966-4728-b0ea-ffec22f69edc&redirect_uri=com.playstation.PlayStationApp%3A%2F%2Fredirect&response_type=code&scope=psn%3Amobile.v1%20psn%3Aclientapp";
        private const string mobileTokenUrl = "https://ca.account.sony.com/api/authz/v3/oauth/token";
        private const string mobileTokenAuth = "YWM4ZDE2MWEtZDk2Ni00NzI4LWIwZWEtZmZlYzIyZjY5ZWRjOkRFaXhFcVhYQ2RYZHdqMHY=";
        private const string playedMobileListUrl = "https://m.np.playstation.net/api/gamelist/v2/users/me/titles?categories=ps4_game,ps5_native_game&limit=250&offset={0}";
        private const string trophiesMobileUrl = @"https://m.np.playstation.net/api/trophy/v1/users/me/trophyTitles?limit=250&offset={0}";
        public string trophiesWithIdsMobileUrl = @"https://m.np.playstation.net/api/trophy/v1/users/me/titles/trophyTitles?npTitleIds={0}";

        public PSNAccountClient(IPlayniteAPI api, string PsnPluginUserDataPath)//public PSNAccountClient(PSNLibrary library, IPlayniteAPI api)
        {
            //this.library = library;
            this.api = api;
            tokenPath = Path.Combine(PsnPluginUserDataPath, "token.json");//Path.Combine(library.GetPluginUserDataPath(), "token.json");
        }

        public void Login()
        {
            var loggedIn = false;


            using (var view = api.WebViews.CreateView(580, 700))
            {
                view.LoadingChanged += (s, e) =>
                {
                    var address = view.GetCurrentAddress();
                    if (address.StartsWith(@"https://www.playstation.com/"))
                    {
                        loggedIn = true;
                        view.Close();
                    }
                };

                view.DeleteDomainCookies(".sony.com");
                view.DeleteDomainCookies("ca.account.sony.com");
                view.DeleteDomainCookies(".playstation.com");
                view.DeleteDomainCookies("io.playstation.com");
                view.Navigate(loginUrl);
                view.OpenDialog();
            }

            if (!loggedIn)
            {
                return;
            }

            dumpCookies();

            return;
        }

        private IEnumerable<Playnite.SDK.HttpCookie> dumpCookies()
        {
            var view = api.WebViews.CreateOffscreenView();

            var cookies = view.GetCookies();


            var cookieContainer = new CookieContainer();
            foreach (var cookie in cookies)
            {
                if (cookie.Domain == ".playstation.com")
                {
                    cookieContainer.Add(new Uri("https://web.np.playstation.com"), new Cookie(cookie.Name, cookie.Value));
                }
                if (cookie.Domain == "ca.account.sony.com")
                {

                    cookieContainer.Add(new Uri("https://ca.account.sony.com"), new Cookie(cookie.Name, cookie.Value));
                }
                if (cookie.Domain == ".sony.com")
                {
                    cookieContainer.Add(new Uri("https://ca.account.sony.com"), new Cookie(cookie.Name, cookie.Value));
                }
            }

            WriteCookiesToDisk(cookieContainer);

            view.Dispose();
            return cookies;
        }

        private void WriteCookiesToDisk(CookieContainer cookieJar)
        {
            File.Delete(tokenPath);
            using (Stream stream = File.Create(tokenPath))
            {
                try
                {
                    Console.Out.Write("Writing cookies to disk... ");
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookieJar);
                    Console.Out.WriteLine("Done.");
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Problem writing cookies to disk: " + e.GetType());
                }
            }
        }

        private CookieContainer ReadCookiesFromDisk()
        {
            try
            {
                using (Stream stream = File.Open(tokenPath, FileMode.Open))
                {
                    Console.Out.Write("Reading cookies from disk... ");
                    BinaryFormatter formatter = new BinaryFormatter();
                    Console.Out.WriteLine("Done.");
                    return (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Problem reading cookies from disk: " + e.GetType());
                return new CookieContainer();
            }
        }

        private async Task<bool> getMobileToken()
        {
            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                string mobileCode;
                try
                {
                    var mobileCodeResponse = await httpClient.GetAsync(mobileCodeUrl);
                    mobileCode = HttpUtility.ParseQueryString(mobileCodeResponse.Headers.Location.Query)["code"];
                }
                catch
                {
                    TryRefreshCookies();
                    try
                    {
                        var mobileCodeResponse = await httpClient.GetAsync(mobileCodeUrl);
                        mobileCode = HttpUtility.ParseQueryString(mobileCodeResponse.Headers.Location.Query)["code"];
                    }
                    catch
                    {
                        return false;
                    }
                }

                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("post"), mobileTokenUrl);
                var requestMessageForm = new List<KeyValuePair<string, string>>();
                requestMessageForm.Add(new KeyValuePair<string, string>("code", mobileCode));
                requestMessageForm.Add(new KeyValuePair<string, string>("redirect_uri", "com.playstation.PlayStationApp://redirect"));
                requestMessageForm.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
                requestMessageForm.Add(new KeyValuePair<string, string>("token_format", "jwt"));
                requestMessage.Content = new FormUrlEncodedContent(requestMessageForm);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", mobileTokenAuth);

                var mobileTokenResponse = await httpClient.SendAsync(requestMessage);
                var strResponse = await mobileTokenResponse.Content.ReadAsStringAsync();
                mobileToken = Serialization.FromJson<MobileTokens>(strResponse);
                return true;
            }
        }

        public async Task CheckAuthentication()
        {
            if (!File.Exists(tokenPath))
            {
                throw new Exception("User is not authenticated.");
            }
            else
            {
                if (!await GetIsUserLoggedIn())
                {
                    TryRefreshCookies();
                    if (!await GetIsUserLoggedIn())
                    {
                        throw new Exception("User is not authenticated.");
                    }
                }
                else
                {
                    if (mobileToken == null)
                    {
                        if (!await getMobileToken())
                        {
                            throw new Exception("User is not authenticated.");
                        }
                    }
                }
            }
        }

        public async Task<List<PlayedTitlesResponseData.PlayedTitlesRetrieve.Title>> GetPlayedTitles()
        {
            await CheckAuthentication();

            var titles = new List<PlayedTitlesResponseData.PlayedTitlesRetrieve.Title>();

            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                var resp = httpClient.GetAsync(playedListUrl).GetAwaiter().GetResult();
                var strResponse = await resp.Content.ReadAsStringAsync();
                var titles_part = Serialization.FromJson<PlayedTitles>(strResponse);
                titles.AddRange(titles_part.data.gameLibraryTitlesRetrieve.games);
            }

            return titles;
        }

        public async Task<List<AccountTitlesResponseData.AccountTitlesRetrieve.Title>> GetAccountTitles()
        {
            await CheckAuthentication();

            var titles = new List<AccountTitlesResponseData.AccountTitlesRetrieve.Title>();

            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {

                var itemCount = 0;
                var offset = -pageRequestLimit;

                do
                {
                    object[] args = { offset, pageRequestLimit };
                    var resp = httpClient.GetAsync(gameListUrl.Format(offset + pageRequestLimit, pageRequestLimit)).GetAwaiter().GetResult();
                    var strResponse = await resp.Content.ReadAsStringAsync();
                    var titles_part = Serialization.FromJson<AccountTitles>(strResponse);
                    titles.AddRange(titles_part.data.purchasedTitlesRetrieve.games);
                    offset = titles_part.data.purchasedTitlesRetrieve.pageInfo.offset;
                    itemCount = titles_part.data.purchasedTitlesRetrieve.pageInfo.totalCount;
                } while (offset + pageRequestLimit < itemCount);


            }

            return titles;
        }

        public async Task<List<PlayedTitlesMobile.PlayedTitleMobile>> GetPlayedTitlesMobile()
        {
            await CheckAuthentication();

            var titles = new List<PlayedTitlesMobile.PlayedTitleMobile>();

            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                int? offset = 0;

                do
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("get"), playedMobileListUrl.Format(offset));
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mobileToken.access_token);
                    var resp = await httpClient.SendAsync(requestMessage);
                    var strResponse = await resp.Content.ReadAsStringAsync();
                    var titles_part = Serialization.FromJson<PlayedTitlesMobile>(strResponse);
                    titles.AddRange(titles_part.titles);
                    offset = titles_part.nextOffset;
                } while (offset != null);


            }

            return titles;
        }

        public async Task<List<TrophyTitleMobile>> GetTrohpiesMobile()
        {
            await CheckAuthentication();

            var titles = new List<TrophyTitleMobile>();

            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                int? offset = 0;

                do
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("get"), trophiesMobileUrl.Format(offset));
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mobileToken.access_token);
                    var resp = await httpClient.SendAsync(requestMessage);
                    var strResponse = await resp.Content.ReadAsStringAsync();
                    var titles_part = Serialization.FromJson<TrophyTitlesMobile>(strResponse);
                    titles.AddRange(titles_part.trophyTitles);
                    offset = titles_part.nextOffset;
                } while (offset != null);
            }

            return titles;
        }

        //public async Task<List<TrophyTitlesWithIdsMobile.TrophyTitleWithIdsMobile>> GetTrohpiesWithIdsMobile(string[] titleIdsArray)
        //{
        //    await CheckAuthentication();
        //
        //    var titles = new List<TrophyTitlesWithIdsMobile.TrophyTitleWithIdsMobile>();
        //
        //    var cookieContainer = ReadCookiesFromDisk();
        //    using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
        //    using (var httpClient = new HttpClient(handler))
        //    {
        //        int querySize = 5;
        //        int offset = 0;
        //
        //        do
        //        {
        //            HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("get"), trophiesWithIdsMobileUrl.Format(string.Join(",", titleIdsArray.Skip(offset).Take(querySize))));
        //            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mobileToken.access_token);
        //            var resp = await httpClient.SendAsync(requestMessage);
        //            var strResponse = await resp.Content.ReadAsStringAsync();
        //            var titles_part = Serialization.FromJson<TrophyTitlesWithIdsMobile>(strResponse);
        //            titles.AddRange(titles_part.titles);
        //            offset = offset + querySize;
        //        } while (offset < titleIdsArray.Length);
        //    }
        //
        //    return titles;
        //}

        private void TryRefreshCookies()
        {
            string address;
            using (var webView = api.WebViews.CreateOffscreenView())
            {
                webView.LoadingChanged += (s, e) =>
                {
                    address = webView.GetCurrentAddress();
                    webView.Close();
                };

                webView.NavigateAndWait(loginUrl);
            }

            dumpCookies();
        }

        public async Task<bool> GetIsUserLoggedIn()
        {
            if (!File.Exists(tokenPath))
            {
                return false;
            }

            try
            {
                var cookieContainer = ReadCookiesFromDisk();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                using (var httpClient = new HttpClient(handler))
                {

                    var resp = httpClient.GetAsync(gameListUrl.Format(0, 24)).GetAwaiter().GetResult();
                    var strResponse = await resp.Content.ReadAsStringAsync();
                    if (Serialization.TryFromJson<AccountTitlesErrorResponse>(strResponse, out var error) && error.data.purchasedTitlesRetrieve == null)
                    {
                        return false;
                    }

                    if (Serialization.TryFromJson<AccountTitles>(strResponse, out var accountTitles) && accountTitles.data.purchasedTitlesRetrieve != null)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                logger.Error(e, "Failed to check if user is authenticated into PSN.");
                return false;
            }
        }
    }

    /*
    public class PSNAccountClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI PlayniteApi;//private readonly PSNLibrary library;
        private readonly string tokenPath;
        private const int pageRequestLimit = 100;
        private const string loginUrl = @"https://my.account.sony.com/central/signin/?response_type=token&scope=capone%3Areport_submission%2Ckamaji%3Agame_list%2Ckamaji%3Aget_account_hash%2Cuser%3Aaccount.get%2Cuser%3Aaccount.profile.get%2Ckamaji%3Asocial_get_graph%2Ckamaji%3Augc%3Adistributor%2Cuser%3Aaccount.identityMapper%2Ckamaji%3Amusic_views%2Ckamaji%3Aactivity_feed_get_feed_privacy%2Ckamaji%3Aactivity_feed_get_news_feed%2Ckamaji%3Aactivity_feed_submit_feed_story%2Ckamaji%3Aactivity_feed_internal_feed_submit_story%2Ckamaji%3Aaccount_link_token_web%2Ckamaji%3Augc%3Adistributor_web%2Ckamaji%3Aurl_preview&client_id=656ace0b-d627-47e6-915c-13b259cd06b2&redirect_uri=https%3A%2F%2Fmy.playstation.com%2Fauth%2Fresponse.html%3FrequestID%3Dexternal_request_a90959ab-afa3-4594-824b-ad00b6617f57%26baseUrl%3D%2F%26returnRoute%3D%2F%26targetOrigin%3Dhttps%3A%2F%2Fmy.playstation.com%26excludeQueryParams%3Dtrue&tp_console=true&ui=pr&cid=b7895274-2de2-45da-add9-3afd775eb65f&error=login_required&error_code=4165&no_captcha=true#/signin/ca?entry=ca";
        private const string loginTokenUrl = @"https://ca.account.sony.com/api/v1/oauth/authorize?response_type=token&scope=capone:report_submission,kamaji:game_list,kamaji:get_account_hash,user:account.get,user:account.profile.get,kamaji:social_get_graph,kamaji:ugc:distributor,user:account.identityMapper,kamaji:music_views,kamaji:activity_feed_get_feed_privacy,kamaji:activity_feed_get_news_feed,kamaji:activity_feed_submit_feed_story,kamaji:activity_feed_internal_feed_submit_story,kamaji:account_link_token_web,kamaji:ugc:distributor_web,kamaji:url_preview&client_id=656ace0b-d627-47e6-915c-13b259cd06b2&redirect_uri=https://my.playstation.com/auth/response.html?requestID=iframe_request_ecd7cd01-27ad-4851-9c0d-0798c1a65e53&baseUrl=/&targetOrigin=https://my.playstation.com&prompt=none";
        private const string tokenUrl = @"https://ca.account.sony.com/api/v1/oauth/authorize?response_type=token&scope=capone:report_submission,kamaji:game_list,kamaji:get_account_hash,user:account.get,user:account.profile.get,kamaji:social_get_graph,kamaji:ugc:distributor,user:account.identityMapper,kamaji:music_views,kamaji:activity_feed_get_feed_privacy,kamaji:activity_feed_get_news_feed,kamaji:activity_feed_submit_feed_story,kamaji:activity_feed_internal_feed_submit_story,kamaji:account_link_token_web,kamaji:ugc:distributor_web,kamaji:url_preview&client_id=656ace0b-d627-47e6-915c-13b259cd06b2&redirect_uri=https://my.playstation.com/auth/response.html?requestID=iframe_request_b0f09e04-8206-49be-8be6-b2cfe05249e2&baseUrl=/&targetOrigin=https://my.playstation.com&prompt=none";
        private const string gameListUrl = @"https://gamelist.api.playstation.com/v1/users/me/titles?type=owned,played&app=richProfile&sort=-lastPlayedDate&iw=240&ih=240&fields=@default&limit={0}&offset={1}&npLanguage=en";
        private const string trophiesUrl = @"https://us-tpy.np.community.playstation.net/trophy/v1/trophyTitles?fields=@default,trophyTitleSmallIconUrl&platform=PS3,PS4,PSVITA&limit={0}&offset={1}&npLanguage=en";
        private const string profileUrl = @"https://us-prof.np.community.playstation.net/userProfile/v1/users/me/profile2";
        private const string downloadListUrl = @"https://store.playstation.com/en/download/list";
        private const string profileLandingUrl = @"https://my.playstation.com/whatsnew";

        public PSNAccountClient(IPlayniteAPI PlayniteApi, string PluginUserDataPath)//public PSNAccountClient(PSNLibrary library)
        {
            this.PlayniteApi = PlayniteApi;//this.library = library;
            tokenPath = Path.Combine(PluginUserDataPath, "token.json");//tokenPath = Path.Combine(library.GetPluginUserDataPath(), "token.json");
        }

        //public void Login()
        //{
        //    using (var webView = library.PlayniteApi.WebViews.CreateView(540, 670))using (var webView = library.PlayniteApi.WebViews.CreateView(540, 670))
        //    {
        //        var callbackUrl = string.Empty;
        //        webView.LoadingChanged += (_, __) =>
        //        {
        //            var address = webView.GetCurrentAddress();
        //            if (address == profileLandingUrl)
        //            {
        //                webView.Navigate(tokenUrl);
        //            }
        //            else if (address.Contains("access_token="))
        //            {
        //                callbackUrl = address;
        //                webView.Close();
        //            }
        //            else if (address.EndsWith("/friends"))
        //            {
        //                webView.Navigate(loginTokenUrl);
        //            }
        //        };
        //
        //        if (File.Exists(tokenPath))
        //        {
        //            File.Delete(tokenPath);
        //        }
        //
        //        webView.DeleteDomainCookies(".playstation.com");
        //        webView.DeleteDomainCookies(".sonyentertainmentnetwork.com");
        //        webView.DeleteDomainCookies(".sony.com");
        //        webView.Navigate(loginUrl);
        //        webView.OpenDialog();
        //
        //        if (!callbackUrl.IsNullOrEmpty())
        //        {
        //            var rediUri = new Uri(callbackUrl);
        //            var fragments = HttpUtility.ParseQueryString(rediUri.Fragment);
        //            var token = fragments["#access_token"];
        //            FileSystem.WriteStringToFile(tokenPath, token);
        //        }
        //    }
        //}

        private async Task CheckAuthentication()
        {
            if (!File.Exists(tokenPath))
            {
                throw new Exception("User is not authenticated.");
            }
            else
            {
                if (!await GetIsUserLoggedIn())
                {
                    throw new Exception("User is not authenticated.");
                }
            }
        }

        //public async Task<List<DownloadListEntitlement>> GetDownloadList()
        //{
        //    await CheckAuthentication();
        //
        //    using (var webView = library.PlayniteApi.WebViews.CreateOffscreenView())
        //    {
        //        var loadComplete = new AutoResetEvent(false);
        //        var items = new List<DownloadListEntitlement>();
        //        var processingDownload = false;
        //
        //        webView.LoadingChanged += async (_, e) =>
        //        {
        //            var address = webView.GetCurrentAddress();
        //            if (address?.EndsWith("download/list") == true && !e.IsLoading)
        //            {
        //                if (processingDownload)
        //                {
        //                    return;
        //                }
        //
        //                processingDownload = true;
        //                var numberOfTries = 0;
        //                while (numberOfTries < 6)
        //                {
        //                    // Don't know how to reliable tell if the data are ready because they are laoded post page load
        //                    await Task.Delay(10000);
        //                    if (!webView.CanExecuteJavascriptInMainFrame)
        //                    {
        //                        logger.Warn("PSN JS execution not ready yet.");
        //                        continue;
        //                    }
        //
        //                    // Need to use this hack since the data we need are stored in browser's local storage
        //                    // Based on https://github.com/RePod/psdle/blob/master/psdle.js
        //                    var res = await webView.EvaluateScriptAsync(@"JSON.stringify(Ember.Application.NAMESPACES_BY_ID['valkyrie-storefront'].__container__.lookup('service:macross-brain').macrossBrainInstance.getEntitlementStore().getAllEntitlements()._result)");
        //                    var strRes = (string)res.Result;
        //                    if (strRes.IsNullOrEmpty())
        //                    {
        //                        numberOfTries++;
        //                        continue;
        //                    }
        //
        //                    try
        //                    {
        //                        items = Serialization.FromJson<List<DownloadListEntitlement>>(strRes);
        //                    }
        //                    catch (Exception exc)
        //                    {
        //                        logger.Error(exc, "Failed to deserialize PSN's download list.");
        //                        logger.Debug(strRes);
        //                    }
        //
        //                    loadComplete.Set();
        //                    break;
        //                }
        //            }
        //        };
        //
        //        webView.Navigate(downloadListUrl);
        //        loadComplete.WaitOne(60000);
        //        return items;
        //    }
        //}

        private async Task<T> SendPageRequest<T>(HttpClient client, string url, int offset) where T : class, new()
        {
            var strResponse = await client.GetStringAsync(url.Format(pageRequestLimit, offset));
            return Serialization.FromJson<T>(strResponse);
        }

        //public async Task<List<TrophyTitles.TrophyTitle>> GetThropyTitles()
        //{
        //    await CheckAuthentication();
        //    var token = GetStoredToken();
        //    var titles = new List<TrophyTitles.TrophyTitle>();
        //    using (var client = new HttpClient())
        //    {
        //        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        //        var itemCount = 0;
        //        var offset = -pageRequestLimit;
        //
        //        do
        //        {
        //            var response = await SendPageRequest<TrophyTitles>(client, trophiesUrl, offset + pageRequestLimit);
        //            itemCount = response.totalResults;
        //            offset = response.offset;
        //            titles.AddRange(response.trophyTitles);
        //        }
        //        while (offset + pageRequestLimit < itemCount);
        //    }
        //
        //    return titles;
        //}

        //public async Task<List<AccountTitles.Title>> GetAccountTitles()
        //{
        //    await CheckAuthentication();
        //    var token = GetStoredToken();
        //    var titles = new List<AccountTitles.Title>();
        //    using (var client = new HttpClient())
        //    {
        //        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        //        var itemCount = 0;
        //        var offset = -pageRequestLimit;
        //
        //        do
        //        {
        //            var response = await SendPageRequest<AccountTitles>(client, gameListUrl, offset + pageRequestLimit);
        //            itemCount = response.totalResults;
        //            offset = response.start;
        //            titles.AddRange(response.titles);
        //        }
        //        while (offset + pageRequestLimit < itemCount);
        //    }
        //
        //    return titles;
        //}

        public string GetStoredToken()
        {
            var token = string.Empty;
            if (File.Exists(tokenPath))
            {
                token = File.ReadAllText(tokenPath);
            }

            return token;
        }

        private string RefreshToken()
        {
            //logger.Debug("Trying to refresh PSN token.");
            if (File.Exists(tokenPath))
            {
                File.Delete(tokenPath);
            }

            var callbackUrl = string.Empty;
            using (var webView = PlayniteApi.WebViews.CreateOffscreenView())//using (var webView = library.PlayniteApi.WebViews.CreateOffscreenView())
            {
                webView.LoadingChanged += (_, __) =>
                {
                    var address = webView.GetCurrentAddress();
                    if (address.Contains("access_token="))
                    {
                        callbackUrl = address;
                    }
                };

                webView.NavigateAndWait(profileLandingUrl);
                webView.NavigateAndWait(tokenUrl);

                if (!callbackUrl.IsNullOrEmpty())
                {
                    var rediUri = new Uri(callbackUrl);
                    var fragments = HttpUtility.ParseQueryString(rediUri.Fragment);
                    var token = fragments["#access_token"];
                    FileSystem.WriteStringToFile(tokenPath, token);
                    return token;
                }
            }

            return string.Empty;
        }

        public async Task<bool> GetIsUserLoggedIn()
        {
            if (!File.Exists(tokenPath))
            {
                return false;
            }

            try
            {
                var token = GetStoredToken();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    var response = await client.GetAsync(profileUrl);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        token = RefreshToken();
                        if (token.IsNullOrEmpty())
                        {
                            return false;
                        }

                        client.DefaultRequestHeaders.Remove("Authorization");
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                        response = await client.GetAsync(profileUrl);
                        return response.StatusCode == System.Net.HttpStatusCode.OK;
                    }
                }
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                logger.Error(e, "Failed to check if user is authenticated into PSN.");
                return false;
            }
        }
    }
    */
}

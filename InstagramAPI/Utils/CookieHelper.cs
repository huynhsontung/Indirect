using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace InstagramAPI.Utils
{
    internal static class CookieHelper
    {
        public static void ClearCookies()
        {
            var myFilter = new HttpBaseProtocolFilter();
            var cookieManager = myFilter.CookieManager;
            var instagramApiCookies = cookieManager.GetCookies(UriCreator.BaseInstagramUri);
            foreach (var cookie in instagramApiCookies)
            {
                cookieManager.DeleteCookie(cookie);
            }

            var instagramCookies = cookieManager.GetCookies(new Uri("https://www.instagram.com/"));
            foreach (var cookie in instagramCookies)
            {
                cookieManager.DeleteCookie(cookie);
            }

            var fbCookies = cookieManager.GetCookies(new Uri("https://www.facebook.com/"));
            foreach (var cookie in fbCookies)
            {
                cookieManager.DeleteCookie(cookie);
            }
        }

        public static List<HttpCookie> GetCookies()
        {
            var myFilter = new HttpBaseProtocolFilter();
            var cookieManager = myFilter.CookieManager;

            var cookiesList = new List<HttpCookie>();
            cookiesList.AddRange(cookieManager.GetCookies(UriCreator.BaseInstagramUri));
            cookiesList.AddRange(cookieManager.GetCookies(new Uri("https://www.instagram.com/")));
            cookiesList.AddRange(cookieManager.GetCookies(new Uri("https://www.facebook.com/")));

            return cookiesList;
        }

        public static void SetCookies(IEnumerable<HttpCookie> cookies)
        {
            var myFilter = new HttpBaseProtocolFilter();
            var cookieManager = myFilter.CookieManager;

            foreach (var cookie in cookies)
            {
                cookieManager.SetCookie(cookie);
            }
        }
    }
}

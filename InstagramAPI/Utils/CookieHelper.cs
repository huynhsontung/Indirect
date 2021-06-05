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
        public static HttpBaseProtocolFilter ClearCookies()
        {
            var filter = new HttpBaseProtocolFilter();
            var cookieManager = filter.CookieManager;
            var instagramApiCookies = cookieManager.GetCookies(UriCreator.BaseInstagramUri);
            foreach (var cookie in instagramApiCookies)
            {
                cookieManager.DeleteCookie(cookie);
            }

            var fbCookies = cookieManager.GetCookies(new Uri("https://www.facebook.com/"));
            foreach (var cookie in fbCookies)
            {
                cookieManager.DeleteCookie(cookie);
            }

            return filter;
        }

        public static List<HttpCookie> GetCookies()
        {
            var myFilter = new HttpBaseProtocolFilter();
            var cookieManager = myFilter.CookieManager;

            var cookiesList = new List<HttpCookie>();
            cookiesList.AddRange(cookieManager.GetCookies(UriCreator.BaseInstagramUri));
            cookiesList.AddRange(cookieManager.GetCookies(new Uri("https://www.facebook.com/")));

            return cookiesList;
        }

        public static HttpBaseProtocolFilter SetCookies(IEnumerable<HttpCookie> cookies)
        {
            if (cookies == null)
            {
                return new HttpBaseProtocolFilter();
            }

            var filter = new HttpBaseProtocolFilter();
            var cookieManager = filter.CookieManager;

            foreach (var cookie in cookies)
            {
                cookieManager.SetCookie(cookie);
            }

            return filter;
        }
    }
}

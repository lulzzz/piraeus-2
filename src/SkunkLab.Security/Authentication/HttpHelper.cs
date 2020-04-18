using Microsoft.AspNetCore.Http;

namespace SkunkLab.Security.Authentication
{
    public static class HttpHelper
    {
        private static IHttpContextAccessor accessor;

        public static HttpContext HttpContext => accessor.HttpContext;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            accessor = httpContextAccessor;
        }
    }
}
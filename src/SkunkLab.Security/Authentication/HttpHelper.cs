﻿using Microsoft.AspNetCore.Http;

namespace SkunkLab.Security.Authentication
{
    public static class HttpHelper
    {
        private static IHttpContextAccessor _accessor;

        public static HttpContext HttpContext => _accessor.HttpContext;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _accessor = httpContextAccessor;
        }
    }
}
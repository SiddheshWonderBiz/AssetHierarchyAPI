using System.Collections.Concurrent;

namespace AssetHierarchyAPI.Middleware
{
    public class RateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, DateTime> _lastcall = new ConcurrentDictionary<string, DateTime>();
        public RateLimiterMiddleware(RequestDelegate next)
        {
            _next = next;

        }
        public async Task InvokeAsync(HttpContext context)
        {

            if ((context.Request.Method == "POST" && context.Request.Path.StartsWithSegments("/api/Hierarchy/add")))
            {
                string UserKey = context.Connection.RemoteIpAddress.ToString(); 
                if (_lastcall.TryGetValue(UserKey, out var lastCall))
                {
                    if (DateTime.UtcNow - lastCall < TimeSpan.FromMinutes(1))
                    {
                        await context.Response.WriteAsync("Please wait one minute before making a request");
                        return;


                    }
                }
                _lastcall[UserKey] = DateTime.UtcNow;


            }
            await _next(context);
        }
    }
}
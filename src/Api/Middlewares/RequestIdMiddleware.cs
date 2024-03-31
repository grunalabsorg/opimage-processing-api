using Api.Common;
using System;

namespace Api.Middlewares
{
    public class RequestIdMiddleware
    {
        private readonly RequestDelegate _next;        

        public RequestIdMiddleware(RequestDelegate next)
        {
            _next = next;            
        }

        public async Task InvokeAsync(HttpContext context, SessionData sessionData)
        {            
            var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(requestId))
            {
                requestId = sessionData.RequestId;
                context.Request.Headers["X-Request-ID"] = requestId;                
            }

            await _next(context);
        }
    }
}

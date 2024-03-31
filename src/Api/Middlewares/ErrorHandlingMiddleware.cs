using System.Net;
using System.Text.Json;

namespace Api.Middlewares
{
    /// <summary>
    /// Exception handler middleware.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode code = HttpStatusCode.InternalServerError;

            var result = JsonSerializer.Serialize(new
            {
                title = "An error occurred.",
                type = "Internal server error.",
                detail = string.Empty,
                status = (int)code,
                traceId = context.TraceIdentifier
            });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            return context.Response.WriteAsync(result);

        }
    }
}

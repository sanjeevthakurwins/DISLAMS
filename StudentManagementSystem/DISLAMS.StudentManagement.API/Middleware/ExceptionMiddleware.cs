using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DISLAMS.StudentManagement.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception ex)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            var problem = new CustomProblemDetails();

            switch (ex)
            {
                case KeyNotFoundException keyNotFound:
                    statusCode = HttpStatusCode.NotFound;
                    problem = new CustomProblemDetails
                    {
                        Title = keyNotFound.Message,
                        Status = (int)statusCode,
                        Detail = keyNotFound.InnerException?.Message,
                        Type = nameof(KeyNotFoundException)
                    };
                    break;
                case InvalidOperationException invalidOp:
                    statusCode = HttpStatusCode.BadRequest;
                    problem = new CustomProblemDetails
                    {
                        Title = invalidOp.Message,
                        Status = (int)statusCode,
                        Detail = invalidOp.InnerException?.Message,
                        Type = nameof(InvalidOperationException)
                    };
                    break;
                case UnauthorizedAccessException unauth:
                    statusCode = HttpStatusCode.Unauthorized;
                    problem = new CustomProblemDetails
                    {
                        Title = unauth.Message,
                        Status = (int)statusCode,
                        Detail = unauth.InnerException?.Message,
                        Type = nameof(UnauthorizedAccessException)
                    };
                    break;
                case ArgumentException argEx:
                    statusCode = HttpStatusCode.BadRequest;
                    problem = new CustomProblemDetails
                    {
                        Title = argEx.Message,
                        Status = (int)statusCode,
                        Detail = argEx.InnerException?.Message,
                        Type = nameof(ArgumentException)
                    };
                    break;
                default:
                    problem = new CustomProblemDetails
                    {
                        Title = ex.Message,
                        Status = (int)statusCode,
                        Type = nameof(HttpStatusCode.InternalServerError),
                        Detail = ex.StackTrace,
                    };
                    break;
            }

            // If the exception exposes validation details (ValidationErrors or Errors), try to copy them into ProblemDetails.Errors
            var errorsProp = ex.GetType().GetProperty("ValidationErrors") ?? ex.GetType().GetProperty("Errors");
            if (errorsProp is not null)
            {
                var val = errorsProp.GetValue(ex);
                try
                {
                    if (val is IDictionary<string, string[]> dict)
                    {
                        problem.Errors = new Dictionary<string, string[]>(dict);
                    }
                    else if (val is IDictionary<string, IEnumerable<string>> dict2)
                    {
                        problem.Errors = dict2.ToDictionary(k => k.Key, v => v.Value.ToArray());
                    }
                    else if (val is IEnumerable<KeyValuePair<string, string[]>> kvps)
                    {
                        problem.Errors = kvps.ToDictionary(k => k.Key, v => v.Value);
                    }
                }
                catch
                {
                    // ignore if we cannot map the structure
                }
            }

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)statusCode;
            var logMessage = JsonSerializer.Serialize(problem);
            _logger.LogError(logMessage);
            await httpContext.Response.WriteAsJsonAsync(problem);

        }
    }
}
﻿using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Contact.Infrastructure.Middleware;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILoggerFactory _loggerFactory)
    {
        _next = next;
        _logger = _loggerFactory.CreateLogger<ErrorHandlerMiddleware>();
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception error)
        {
            // Todo: We could enrich the logs with additional attributes (like Request.Path), route params, headers, other?
            _logger.LogError(error, "An error occured for {url}", context.Request.Path);
            
            HttpResponse response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode = error switch
            {
                BadHttpRequestException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized, //unauth error
                KeyNotFoundException => (int)HttpStatusCode.NotFound,// not found error
                _ => (int)HttpStatusCode.InternalServerError,// unhandled error
            };

            // Todo: We should consider not serializing the exception message to the client?
            var result = JsonSerializer.Serialize(new { message = error?.Message });
            await response.WriteAsync(result);
        }
    }
}

using System.Net;
using System.Text.Json;
using MongoDB.Driver;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Errors;

namespace WcxTimeOff.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, (int)ex.StatusCode, ex.Error, ex.Message);
        }
        catch (MongoConnectionException ex)
        {
            logger.LogError(ex, "MongoDB connection failed.");
            await WriteAsync(context, (int)HttpStatusCode.ServiceUnavailable, "database_unavailable", "The database is currently unavailable.");
        }
        catch (MongoException ex)
        {
            logger.LogError(ex, "MongoDB operation failed.");
            await WriteAsync(context, (int)HttpStatusCode.ServiceUnavailable, "database_error", "A database error occurred.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception.");
            await WriteAsync(context, (int)HttpStatusCode.InternalServerError, "server_error", "An unexpected error occurred.");
        }
    }

    private static Task WriteAsync(HttpContext context, int statusCode, string error, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse
        {
            Error = error,
            Message = message
        }, JsonOptions));
    }
}

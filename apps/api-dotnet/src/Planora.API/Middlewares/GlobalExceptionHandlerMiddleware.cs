using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planora.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware : IMiddleware
{
	private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

	public GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger)
	{
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled exception while processing request {Method} {Path}", context.Request.Method, context.Request.Path);
			await HandleExceptionAsync(context, ex);
		}
	}

	private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		int statusCode = StatusCodes.Status500InternalServerError;
		string message = "An unexpected error occurred.";
		var errors = new List<object>();

		switch (exception)
		{
			case ValidationException vex:
				statusCode = StatusCodes.Status400BadRequest;
				message = "Request validation failed";
				errors.AddRange(vex.Errors.Select(e => new
				{
					field = string.IsNullOrWhiteSpace(e.PropertyName) ? null : e.PropertyName.ToLower(),
					code = (string.IsNullOrWhiteSpace(e.PropertyName) ? "VALIDATION" : (e.PropertyName + ".Invalid")).ToUpper(),
					message = e.ErrorMessage
				}));
				break;
			case UnauthorizedAccessException _:
				statusCode = StatusCodes.Status401Unauthorized;
				message = "Unauthorized";
				errors.Add(new { field = (string?)null, code = "UNAUTHORIZED", message = exception.Message });
				break;
			case KeyNotFoundException _:
				statusCode = StatusCodes.Status404NotFound;
				message = "Not found";
				errors.Add(new { field = (string?)null, code = "NOT_FOUND", message = exception.Message });
				break;
			default:
				errors.Add(new { field = (string?)null, code = "INTERNAL_ERROR", message = "An unexpected error occurred." });
				break;
		}

		var result = new
		{
			statusCode = statusCode,
			message = message,
			errors = errors,
			data = (object?)null
		};

		context.Response.ContentType = "application/json";
		context.Response.StatusCode = statusCode;
		var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
		await context.Response.WriteAsync(JsonSerializer.Serialize(result, options));
	}
}


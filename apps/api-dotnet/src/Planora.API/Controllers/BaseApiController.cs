using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Planora.Domain.Shared.Results;

namespace Planora.Api.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        // ==========================================================
        // 1. Success Envelopes (200, 201, 202)
        // ==========================================================

        // 200 OK (For GET requests)
        protected ObjectResult OkEnvelope<T>(T data, string message = "Operation completed successfully")
        {
            return StatusCode(StatusCodes.Status200OK, new
            {
                statusCode = 200,
                message = message,
                errors = (object)null,
                data = data
            });
        }

        // 201 Created (For POST /api/parcels)
        protected ObjectResult CreatedEnvelope<T>(T data, string message = "Resource created successfully")
        {
            return StatusCode(StatusCodes.Status201Created, new
            {
                statusCode = 201,
                message = message,
                errors = (object)null,
                data = data
            });
        }

        // 202 Accepted (For POST /api/{module}/jobs)
        protected ObjectResult AcceptedEnvelope<T>(T data, string message = "Job accepted for processing")
        {
            return StatusCode(StatusCodes.Status202Accepted, new
            {
                statusCode = 202,
                message = message,
                errors = (object)null,
                data = data
            });
        }

        // ==========================================================
        // 2. Error Envelopes (400, 401, 403, 404, 409, 429, 500)
        // ==========================================================
        protected ObjectResult Problem(List<Error> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                // Fallback for unknown errors 
                return CreateErrorResponse(StatusCodes.Status500InternalServerError, "Unknown error occurred",
                    new List<Error> { Error.Unexpected("INTERNAL_ERROR", "An unexpected error occurred.") });
            }

            // If all errors are Validation, map to 400 Bad Request 
            if (errors.All(error => error.Type == ErrorKind.Validation))
            {
                return CreateErrorResponse(StatusCodes.Status400BadRequest, "Request validation failed", errors);
            }

            // Otherwise, determine status code based on the first error 
            var topError = errors[0];
            var statusCode = topError.Type switch
            {
                ErrorKind.Validation => StatusCodes.Status400BadRequest,
                ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                ErrorKind.Conflict => StatusCodes.Status409Conflict,
                ErrorKind.TooManyRequests => StatusCodes.Status429TooManyRequests, // (See explanation below)
                _ => StatusCodes.Status500InternalServerError,
            };

            return CreateErrorResponse(statusCode, topError.Description, errors);
        }
        
        // 3. Helper to build the unified error envelope 
        private ObjectResult CreateErrorResponse(int statusCode, string message, List<Error> errors)
        {
            var formattedErrors = errors.Select(e => new
            {
                // Example logic to extract field name if format is "Entity.Field.Error"
                field = e.Type == ErrorKind.Validation ? ExtractFieldName(e.Code) : null,
                code = ExtractErrorCode(e.Code),
                message = e.Description,
                metaData = e.MetaData
            });

            return StatusCode(statusCode, new
            {
                statusCode = statusCode,
                message = message,
                errors = formattedErrors,
                data = (object)null
            });
        }

        private string? ExtractFieldName(string code)
        {
            var parts = code.Split('.');
            return parts.Length > 1 ? parts[1].ToLower() : null; // e.g., "User.Email.Invalid" -> "email"
        }

        private string ExtractErrorCode(string code)
        {
            var parts = code.Split('.');
            return parts.Length > 0 ? parts.Last().ToUpper() : code.ToUpper(); // e.g., "User.Email.Invalid" -> "INVALID"
        }
    }
}
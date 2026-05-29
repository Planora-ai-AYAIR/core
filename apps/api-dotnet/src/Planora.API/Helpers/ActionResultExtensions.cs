using Microsoft.AspNetCore.Mvc;
using Planora.Domain.Shared.Results;

namespace Planora.Api.Helpers;

public static class ActionResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, Response<T> response)
    {
        return controller.StatusCode((int)response.StatusCode, response);
    }
}

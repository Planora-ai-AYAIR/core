namespace Planora.Application.Common.Helpers;

public static class EmailDisplayNameHelper
{
    public static string GetDisplayName(string firstName, string lastName, string email)
    {
        var name = string.Join(" ", new[] { firstName, lastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(name) ? email : name;
    }
}

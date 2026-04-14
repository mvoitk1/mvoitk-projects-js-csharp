namespace VenuePlatform.Web.Api;

public static class ApiErrors
{
    public static IResult BadRequest(string code, string error, object? details = null)
        => Results.BadRequest(details is null ? new { code, error } : new { code, error, details });

    public static IResult NotFound(string code, string error, object? details = null)
        => Results.NotFound(details is null ? new { code, error } : new { code, error, details });

    public static IResult Conflict(string code, string error, object? details = null)
        => Results.Conflict(details is null ? new { code, error } : new { code, error, details });

    public static IResult Forbidden(string code, string error, object? details = null)
        => Results.Json(new { code, error, details }, statusCode: StatusCodes.Status403Forbidden);
}

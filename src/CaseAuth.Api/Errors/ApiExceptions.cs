namespace CaseAuth.Api.Errors;

public abstract class ApiException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
}

public class NotFoundApiException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}

public class ConflictApiException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
}

public class ValidationApiException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}

public class ForbiddenApiException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
}

namespace Backend.Common;

public class AppException : Exception
{
    public int StatusCode { get; }
    public AppException(string message, int statusCode) : base(message) => StatusCode = statusCode;
}

public class BadRequestException : AppException   { public BadRequestException(string m) : base(m, 400) { } }

public class UnauthorizedException : AppException { public UnauthorizedException(string m) : base(m, 401) { } }

public class ForbiddenException : AppException    { public ForbiddenException(string m) : base(m, 403) { } }

public class NotFoundException : AppException     { public NotFoundException(string m) : base(m, 404) { } }

public class ConflictException : AppException     { public ConflictException(string m) : base(m, 409) { } }

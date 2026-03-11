namespace imp_api.Services;

public class AppException : Exception
{
    public string Error { get; }

    public AppException(string error, string message) : base(message)
    {
        Error = error;
    }
}

// Backward-compatible alias
public class AuthException : AppException
{
    public AuthException(string error, string message) : base(error, message) { }
}

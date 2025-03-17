public struct DataRequestResult<T>
{
    public bool success;
    public string error;
    public T data;
        
    public static DataRequestResult<T> CreateSuccess(T result)
    {
        return new DataRequestResult<T>
        {
            success = true,
            error = string.Empty,
            data = result
        };
    }
        
    public static DataRequestResult<T> CreateError(string error)
    {
        return new DataRequestResult<T>
        {
            success = false,
            error = error,
            data = default
        };
    }
}

public struct RequestResult
{
    public bool success;
    public string error;
        
    public static RequestResult CreateSuccess()
    {
        return new RequestResult
        {
            success = true,
            error = string.Empty
        };
    }
        
    public static RequestResult CreateError(string error)
    {
        return new RequestResult
        {
            success = false,
            error = error
        };
    }
}

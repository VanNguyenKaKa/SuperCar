namespace HyperCar.BLL.Helpers
{
    /// <summary>
    /// Generic result pattern for BLL → Web layer communication.
    /// Avoids throwing exceptions for expected business failures.
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public static ServiceResult Ok() => new() { Success = true };
        public static ServiceResult Fail(string error) => new() { Success = false, Error = error };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public new static ServiceResult<T> Fail(string error) => new() { Success = false, Error = error };
    }
}

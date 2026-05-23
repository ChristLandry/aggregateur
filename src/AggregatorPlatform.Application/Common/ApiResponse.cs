namespace AggregatorPlatform.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public Guid? TransactionId { get; set; }
    public string? PartnerTransactionRef { get; set; }
    public string? Status { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? status = "OK") =>
        new() { Success = true, Data = data, Status = status };

    public static ApiResponse<T> Ok(T data, Guid? transactionId, string? partnerTransactionRef, string? status = "OK") =>
        new() { Success = true, Data = data, TransactionId = transactionId, PartnerTransactionRef = partnerTransactionRef, Status = status };

    public static ApiResponse<T> Fail(string errorCode, string errorMessage) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string? status = "OK") =>
        new() { Success = true, Status = status };

    public static new ApiResponse Fail(string errorCode, string errorMessage) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

namespace BankAccSim.Exceptions;

public abstract class AppException(string message, int statusCode, string title) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

public class BadRequestException(string message) : AppException(message, 400, "Bad Request");

public class UnauthorizedException(string message) : AppException(message, 401, "Unauthorized");

public class NotFoundException(string message) : AppException(message, 404, "Not Found");

public class ConflictException(string message) : AppException(message, 409, "Conflict");

public class InsufficientFundsException(decimal balance, decimal requested)
    : AppException($"Insufficient funds: balance is {balance:0.00}, requested {requested:0.00}.", 422, "Insufficient Funds");
using Pivot.Framework.Domain.Shared;

namespace RaidManager.ApiService.Features.Shared.Http;

/// <summary>Turns Pivot results into HTTP responses with ProblemDetails for failures.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Gives every minimal-API endpoint the same status mapping, driven by <see cref="ResultExceptionType"/>.
/// </remarks>
public static class ResultHttpExtensions
{
    #region Constants
    /// <summary>Defines the unit-of-work error for a lost optimistic-concurrency race.</summary>
    private const string ConcurrencyErrorCode = "DbUpdateConcurrencyError";
    #endregion Constants

    #region Fields
    /// <summary>Stores the unit-of-work error codes that mean the server failed, not the request.</summary>
    private static readonly HashSet<string> ServerErrorCodes = ["DatabaseError", "UnexpectedError", "DomainEventOutboxError"];
    #endregion Fields

    #region Public Methods
    /// <summary>Returns the success response, or ProblemDetails with the status that matches the failure.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The handler result.</param>
    /// <param name="onSuccess">Builds the success response from the value.</param>
    /// <returns>The HTTP result.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        if (result is IValidationResult validation)
        {
            return TypedResults.ValidationProblem(validation.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray()));
        }

        return TypedResults.Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: StatusCodeOf(result));
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Chooses the HTTP status for a failed result.</summary>
    /// <param name="result">The failed result.</param>
    /// <returns>The HTTP status code.</returns>
    private static int StatusCodeOf(Result result)
    {
        if (result.Error.Code == ConcurrencyErrorCode)
        {
            return StatusCodes.Status409Conflict;
        }

        if (ServerErrorCodes.Contains(result.Error.Code))
        {
            return StatusCodes.Status500InternalServerError;
        }

        return result.ResultExceptionType switch
        {
            ResultExceptionType.NotFound => StatusCodes.Status404NotFound,
            ResultExceptionType.Conflict => StatusCodes.Status409Conflict,
            ResultExceptionType.AuthenticationRequired => StatusCodes.Status401Unauthorized,
            ResultExceptionType.AccessDenied => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };
    }
    #endregion Private Helpers
}

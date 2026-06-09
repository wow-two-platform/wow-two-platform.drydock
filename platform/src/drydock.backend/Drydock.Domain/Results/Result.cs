namespace Drydock.Domain.Results;

/// <summary>Represents the outcome of an operation that returns no payload.</summary>
public abstract class Result
{
    private Result() { }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => this is Success;

    /// <summary>Operation completed successfully.</summary>
    public sealed class Success : Result
    {
        /// <summary>Creates a success outcome.</summary>
        public static Success Create() => new();
    }

    /// <summary>Operation failed — carries the error category and message.</summary>
    public sealed class Failure : Result
    {
        /// <summary>Gets the failure category.</summary>
        public ResultError Error { get; private init; }

        /// <summary>Gets the human-readable error message.</summary>
        public string Message { get; private init; } = "";

        /// <summary>Creates a failure outcome.</summary>
        public static Failure Create(ResultError error, string message) =>
            new() { Error = error, Message = message };
    }

    /// <summary>Convenience factory for a success outcome.</summary>
    public static Success Ok() => Success.Create();

    /// <summary>Convenience factory for a failure outcome.</summary>
    public static Failure Fail(ResultError error, string message) => Failure.Create(error, message);

    /// <summary>Collapses the result into a single value by invoking the handler for whichever case occurred.</summary>
    /// <typeparam name="TOut">The type both handlers project to.</typeparam>
    /// <param name="onSuccess">Invoked when the operation succeeded.</param>
    /// <param name="onFailure">Invoked with the error category and message when the operation failed.</param>
    /// <returns>The value produced by whichever handler ran.</returns>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<ResultError, string, TOut> onFailure) => this switch
    {
        Success _ => onSuccess(),
        Failure failure => onFailure(failure.Error, failure.Message),
        _ => throw new InvalidOperationException("Result has only Success and Failure cases.")
    };
}

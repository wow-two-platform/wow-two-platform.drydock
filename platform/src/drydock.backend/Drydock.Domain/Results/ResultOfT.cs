namespace Drydock.Domain.Results;

/// <summary>Represents the outcome of an operation that returns a <typeparamref name="T"/> payload.</summary>
/// <typeparam name="T">The success payload type.</typeparam>
public abstract class Result<T>
{
    private Result() { }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => this is Success;

    /// <summary>Operation succeeded — holds the returned value.</summary>
    public sealed class Success : Result<T>
    {
        /// <summary>Gets the returned value.</summary>
        public T Value { get; private init; } = default!;

        /// <summary>Creates a success outcome carrying <paramref name="value"/>.</summary>
        public static Success Create(T value) => new() { Value = value };
    }

    /// <summary>Operation failed — carries the error category and message.</summary>
    public sealed class Failure : Result<T>
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
    public static Success Ok(T value) => Success.Create(value);

    /// <summary>Convenience factory for a failure outcome.</summary>
    public static Failure Fail(ResultError error, string message) => Failure.Create(error, message);

    /// <summary>Collapses the result into a single value by invoking the handler for whichever case occurred.</summary>
    /// <typeparam name="TOut">The type both handlers project to.</typeparam>
    /// <param name="onSuccess">Invoked with the value when the operation succeeded.</param>
    /// <param name="onFailure">Invoked with the error category and message when the operation failed.</param>
    /// <returns>The value produced by whichever handler ran.</returns>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<ResultError, string, TOut> onFailure) => this switch
    {
        Success success => onSuccess(success.Value),
        Failure failure => onFailure(failure.Error, failure.Message),
        _ => throw new InvalidOperationException("Result<T> has only Success and Failure cases.")
    };

    /// <summary>Transforms the success value, propagating a failure unchanged.</summary>
    /// <typeparam name="TOut">The mapped payload type.</typeparam>
    /// <param name="selector">Projects the success value into the new payload.</param>
    /// <returns>A success carrying the mapped value, or the original failure.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> selector) =>
        Match<Result<TOut>>(value => Result<TOut>.Ok(selector(value)), Result<TOut>.Fail);
}

namespace Wheelhouse.Application.Abstractions;

/// <summary>Names the operator the current request acts for.</summary>
public interface IOperatorContext
{
    /// <summary>Gets the signed-in operator's login.</summary>
    string Actor { get; }
}

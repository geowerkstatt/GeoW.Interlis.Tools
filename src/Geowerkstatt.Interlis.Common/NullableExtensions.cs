namespace Geowerkstatt.Interlis.Common;

public static class NullableExtensions
{
    /// <summary>
    /// Invokes the specified function if the argument is not null and returns its result; otherwise, returns the default value for the result type.
    /// </summary>
    public static TResult? WhenNotNull<T, TResult>(this T? argument, Func<T, TResult> action)
    {
        return argument is null ? default : action(argument);
    }
}

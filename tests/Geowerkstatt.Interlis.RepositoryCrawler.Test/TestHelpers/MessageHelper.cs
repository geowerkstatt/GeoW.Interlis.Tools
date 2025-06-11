#nullable disable warnings
using System.Globalization;

namespace Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;

/// <summary>
/// Provides support in building error messages.
/// </summary>
public static class MessageHelper
{
    /// <summary>
    /// Executes an asserting action, and if it fails, adds the formatted error message.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
    /// <param name="parameters">An array of parameters to use when formatting <paramref name="message"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> or <paramref name="message"/> is <c>null</c>.</exception>
    public static void Assert(Action action, string message, params object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(message);

        // Wrap the action delegate in a function delegate to reuse
        // Assert(Func<TResult>, string, params object[]) method
        Func<bool> actionAsFunc = () =>
        {
            action();
            return true;
        };

        Assert(actionAsFunc, message, parameters);
    }

    /// <summary>
    /// Executes an asserting function, and if it fails, adds the formatted error message.
    /// </summary>
    /// <param name="f">A delegate to the method to execute.</param>
    /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
    /// <param name="parameters">An array of parameters to use when formatting <paramref name="message"/>.</param>
    /// <typeparam name="TResult">The type of the return value of the method that <paramref name="f"/> encapsulates.</typeparam>
    /// <returns>The result retured when calling <paramref name="f"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="f"/> or <paramref name="message"/> is <c>null</c>.</exception>
    public static TResult Assert<TResult>(Func<TResult> f, string message, params object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            return f();
        }
        catch (AssertInconclusiveException ex)
        {
            throw new AssertInconclusiveException(BuildMessage(ex, message, parameters), ex);
        }
        catch (AssertFailedException ex)
        {
            throw new AssertFailedException(BuildMessage(ex, message, parameters), ex);
        }
    }

    /// <summary>
    /// Extends an exception's error message with the specified message.
    /// </summary>
    /// <param name="ex">The base exception.</param>
    /// <param name="message">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
    /// <param name="parameters">An array of parameters to use when formatting <paramref name="message"/>.</param>
    /// <returns>The completed message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ex"/> or <paramref name="message"/> is <c>null</c>.</exception>
    public static string BuildMessage(Exception ex, string message, object[] parameters)
    {
        return BuildMessage(ex, message, parameters, null, null);
    }

    /// <summary>
    /// Extends an exception's error message with the specified message.
    /// </summary>
    /// <param name="ex">The base exception.</param>
    /// <param name="defaultMessage">A message to display if the assertion fails. This message can be seen in the unit test results.</param>
    /// <param name="defaultParameters">An array of parameters to use when formatting <paramref name="defaultMessage"/>.</param>
    /// <param name="customMessage">A custom message. May be null.</param>
    /// <param name="customParameters">An array of parameters to use when formatting <paramref name="customMessage"/>.</param>
    /// <returns>The completed message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ex"/> or <paramref name="defaultMessage"/> is <c>null</c>.</exception>
    public static string BuildMessage(Exception ex, string defaultMessage, object[] defaultParameters, string customMessage, object[] customParameters)
    {
        ArgumentNullException.ThrowIfNull(ex);

        return CombineMessages(ex.Message, CombineDefaultAndCustomMessage(defaultMessage, defaultParameters, customMessage, customParameters));
    }

    /// <summary>
    /// Combines a default message and a custom message with optional parameters.
    /// </summary>
    /// <param name="defaultMessage">The generic default message.</param>
    /// <param name="customMessage">A custom message. May be null.</param>
    /// <param name="customParameters">An array of parameters to use when formatting <paramref name="customMessage"/>.</param>
    /// <returns>The completed message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="defaultMessage"/> is <c>null</c>.</exception>
    public static string CombineDefaultAndCustomMessage(string defaultMessage, string customMessage, params object[] customParameters)
    {
        return CombineDefaultAndCustomMessage(defaultMessage, null, customMessage, customParameters);
    }

    /// <summary>
    /// Combines a default message with parameters and a custom message with optional parameters.
    /// </summary>
    /// <param name="defaultMessage">The generic default message.</param>
    /// <param name="defaultParameters">An array of parameters to use when formatting <paramref name="defaultMessage"/>.</param>
    /// <param name="customMessage">A custom message. May be null.</param>
    /// <param name="customParameters">An array of parameters to use when formatting <paramref name="customMessage"/>.</param>
    /// <returns>The completed message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="defaultMessage"/> is <c>null</c>.</exception>
    public static string CombineDefaultAndCustomMessage(string defaultMessage, object[] defaultParameters, string customMessage, object[] customParameters)
    {
        ArgumentNullException.ThrowIfNull(defaultMessage);

        if (defaultParameters != null && defaultParameters.Length > 0)
        {
            defaultMessage = string.Format(CultureInfo.InvariantCulture, defaultMessage, defaultParameters);
        }

        if (!string.IsNullOrEmpty(customMessage))
        {
            return CombineMessages(defaultMessage, string.Format(CultureInfo.InvariantCulture, customMessage, customParameters));
        }
        else
        {
            return defaultMessage;
        }
    }

    /// <summary>
    /// Combines the given messages with a space sign.
    /// </summary>
    /// <param name="messages">One or more messages to combine.</param>
    /// <returns>The combined string.</returns>
    public static string CombineMessages(params string[] messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Length == 0) throw new ArgumentOutOfRangeException(nameof(messages));

        return string.Join(" ", messages.Select(exp => exp.Trim()));
    }
}

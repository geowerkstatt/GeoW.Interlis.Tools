#nullable disable warnings
namespace Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;

/// <summary>
/// Provides extension methods for dictionaries to verify
/// true/false propositions with every item.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Verifies a single, specific element in a sequence.
    /// If the item cannot be found, the verification fails.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to test.</param>
    /// <param name="key">The key to assert the presence.</param>
    /// <param name="asserter">The asserting method that is called with the specified item, if found.</param>
    /// <returns><paramref name="dictionary"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// Asserting failed for the found item in <paramref name="dictionary"/>,
    /// <paramref name="dictionary"/> is <c>null</c>,
    /// or there is no element with the specified <paramref name="key"/>.
    /// </exception>
    public static IDictionary<TKey, TValue> AssertSingleItem<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Action<TValue> asserter)
    {
        return AssertSingleItem(dictionary, key, asserter, null);
    }

    /// <summary>
    /// Verifies a single, specific element in a sequence.
    /// If the item cannot be found, the verification fails.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to test.</param>
    /// <param name="key">The key to assert the presence.</param>
    /// <param name="asserter">The asserting method that is called with the specified item, if found.</param>
    /// <param name="message">The message to describe the assertion in case of failure.</param>
    /// <param name="parameters">Parameters for the <paramref name="message"/>.</param>
    /// <returns><paramref name="dictionary"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// Asserting failed for the found item in <paramref name="dictionary"/>,
    /// <paramref name="dictionary"/> is <c>null</c>,
    /// or there is no element with the specified <paramref name="key"/>.
    /// </exception>
    public static IDictionary<TKey, TValue> AssertSingleItem<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Action<TValue> asserter, string message, params object[] parameters)
    {
        if (asserter == null) throw new ArgumentNullException(nameof(asserter), MessageHelper.CombineDefaultAndCustomMessage("Asserter must not be null.", message, parameters));

        Assert.IsNotNull(dictionary, MessageHelper.CombineDefaultAndCustomMessage("The dictionary must not be null.", message, parameters));

        TValue item;

        try
        {
            item = dictionary[key];
        }
        catch (KeyNotFoundException)
        {
            throw new AssertFailedException(
                MessageHelper.CombineDefaultAndCustomMessage(
                    "The item with the key <{0}> was not in the dictionary.",
                    new string[] { key?.ToString() ?? string.Empty },
                    message,
                    parameters));
        }

        if (string.IsNullOrEmpty(message))
        {
            asserter(item);
        }
        else
        {
            MessageHelper.Assert(() => asserter(item), message, parameters);
        }

        return dictionary;
    }
}

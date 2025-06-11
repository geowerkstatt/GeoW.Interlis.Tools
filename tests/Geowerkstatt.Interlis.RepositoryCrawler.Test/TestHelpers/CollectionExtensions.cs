#nullable disable warnings
using System.Globalization;

namespace Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;

/// <summary>
/// Provides extension methods for collections to verify
/// true/false propositions with every item.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Verifies that <paramref name="collection"/> contains a particular <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="item">The expected item.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">If <paramref name="collection"/> or <paramref name="item"/> is
    /// <c>null</c> or <paramref name="item"/> is not part of <paramref name="collection"/>.</exception>
    public static IEnumerable<T> AssertContains<T>(this IEnumerable<T> collection, T item)
    {
        return AssertContains(collection, item, null);
    }

    /// <summary>
    /// Verifies that <paramref name="collection"/> contains a particular <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="item">The expected item.</param>
    /// <param name="message">The message to describe the assertion in case of failure.</param>
    /// <param name="parameters">Parameters for the <paramref name="message"/>.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">If <paramref name="collection"/> or <paramref name="item"/> is
    /// <c>null</c> or <paramref name="item"/> is not part of <paramref name="collection"/>.</exception>
    public static IEnumerable<T> AssertContains<T>(this IEnumerable<T> collection, T item, string message, params object[] parameters)
    {
        Assert.IsNotNull(collection, MessageHelper.CombineDefaultAndCustomMessage("The collection must not be null.", message, parameters));
        Assert.IsNotNull(item, MessageHelper.CombineDefaultAndCustomMessage("The item must not be null.", message, parameters));

        var comparer = EqualityComparer<T>.Default;
        foreach (T element in collection)
        {
            if (comparer.Equals(element, item))
            {
                return collection;
            }
        }

        var defaultMessage = string.Format(
            CultureInfo.InvariantCulture,
            "The expected item <{0}> was not part of source collection.",
            item);

        throw new AssertFailedException(
            MessageHelper.CombineDefaultAndCustomMessage(defaultMessage, message, parameters));
    }

    /// <summary>
    /// Verifies that <paramref name="collection"/> contains exactly <paramref name="expected"/> items.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="expected">The expected number of elements in <paramref name="collection"/>.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// The number of elements in <paramref name="collection"/> is not <paramref name="expected"/>,
    /// or <paramref name="collection"/> is <c>null</c>.
    /// </exception>
    public static IEnumerable<T> AssertCount<T>(this IEnumerable<T> collection, int expected)
    {
        return AssertCount(collection, expected, null);
    }

    /// <summary>
    /// Verifies that <paramref name="collection"/> contains exactly <paramref name="expected"/> items.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="expected">The expected number of elements in <paramref name="collection"/>.</param>
    /// <param name="message">The message to describe the assertion in case of failure.</param>
    /// <param name="parameters">Parameters for the <paramref name="message"/>.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// The number of elements in <paramref name="collection"/> is not <paramref name="expected"/>,
    /// or <paramref name="collection"/> is <c>null</c>.
    /// </exception>
    public static IEnumerable<T> AssertCount<T>(this IEnumerable<T> collection, int expected, string message, params object[] parameters)
    {
        return AssertItems(collection, null, null, expected, message, parameters);
    }

    /// <summary>
    /// Verifies that the sequence contains a single matching item.
    /// The item itself is not verified.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// <paramref name="collection"/> is <c>null</c>,
    /// no element satisfies the condition in <paramref name="predicate"/>,
    /// or more than one element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public static IEnumerable<T> AssertSingleItem<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
    {
        return AssertItems(collection, predicate, null, 1);
    }

    /// <summary>
    /// Verifies a single, specific element in a sequence.
    /// If the item cannot be found, or the predicate is not unique,
    /// the verification is considered failed.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="asserter">The asserting method that is called with the specified item, if found.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// Asserting failed for the found item in <paramref name="collection"/>,
    /// <paramref name="collection"/> is <c>null</c>,
    /// no element satisfies the condition in <paramref name="predicate"/>,
    /// or more than one element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public static IEnumerable<T> AssertSingleItem<T>(this IEnumerable<T> collection, Func<T, bool> predicate, Action<T> asserter)
    {
        return AssertSingleItem(collection, predicate, asserter, null);
    }

    /// <summary>
    /// Verifies a single, specific element in a sequence.
    /// If the item cannot be found, or the predicate is not unique,
    /// the verification is considered failed.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="asserter">The asserting method that is called with the specified item, if found.</param>
    /// <param name="message">The message to describe the assertion in case of failure.</param>
    /// <param name="parameters">Parameters for the <paramref name="message"/>.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// Asserting failed for the found item in <paramref name="collection"/>,
    /// <paramref name="collection"/> is <c>null</c>,
    /// no element satisfies the condition in <paramref name="predicate"/>,
    /// or more than one element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public static IEnumerable<T> AssertSingleItem<T>(this IEnumerable<T> collection, Func<T, bool> predicate, Action<T> asserter, string message, params object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(asserter);

        return AssertItems(collection, predicate, asserter, 1, message, parameters);
    }

    /// <summary>
    /// Verifies that the sequence contains a single item and verifies it.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="asserter">The asserting method that is called with the item, if found.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// Asserting failed for the found item in <paramref name="collection"/>,
    /// <paramref name="collection"/> is <c>null</c>,
    /// <paramref name="collection"/> is empty,
    /// or <paramref name="collection"/> contains more than one item.
    /// </exception>
    public static IEnumerable<T> AssertSingleItem<T>(this IEnumerable<T> collection, Action<T> asserter)
    {
        return AssertItems(collection, null, asserter, 1);
    }

    /// <summary>
    /// Verifies every item selected by the predicate against the asserter.
    /// The count of selected items must exactly match <paramref name="expectedCount"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="asserter">The asserting method that is called with the specified item, if found.</param>
    /// <param name="expectedCount">The expected number of elements in <paramref name="collection"/>.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// Asserting failed for a found item in <paramref name="collection"/>,
    /// <paramref name="collection"/> is <c>null</c>,
    /// not exactly <paramref name="expectedCount"/> elements satisfy the condition in <paramref name="predicate"/>.
    /// </exception>
    public static IEnumerable<T> AssertItems<T>(this IEnumerable<T> collection, Func<T, bool> predicate, Action<T> asserter, int expectedCount)
    {
        return AssertItems(collection, predicate, asserter, expectedCount, null);
    }

    /// <summary>
    /// Verifies every item selected by the predicate against the asserter.
    /// The count of selected items must exactly match <paramref name="expectedCount"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="asserter">The asserting method that is called with the specified item, if found.</param>
    /// <param name="expectedCount">The expected number of elements in <paramref name="collection"/>.</param>
    /// <param name="message">The message to describe the assertion in case of failure.</param>
    /// <param name="parameters">Parameters for the <paramref name="message"/>.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// Asserting failed for a found item in <paramref name="collection"/>,
    /// <paramref name="collection"/> is <c>null</c>,
    /// not exactly <paramref name="expectedCount"/> elements satisfy the condition in <paramref name="predicate"/>.
    /// </exception>
    public static IEnumerable<T> AssertItems<T>(this IEnumerable<T> collection, Func<T, bool> predicate, Action<T> asserter, int expectedCount, string message, params object[] parameters)
    {
        Assert.IsNotNull(collection, MessageHelper.CombineDefaultAndCustomMessage("The collection must not be null.", message, parameters));
        Assert.IsTrue(
            expectedCount == int.MinValue || expectedCount >= 0,
            MessageHelper.CombineDefaultAndCustomMessage("ExpectedCount must be '>= 0' or 'int.MinValue' to ignore.", message, parameters));

        IEnumerable<T> selected;

        if (predicate != null)
            selected = collection.Where(predicate);
        else
            selected = collection.ToList();

        if (expectedCount != int.MinValue && expectedCount != selected.Count())
        {
            throw new AssertFailedException(
                MessageHelper.CombineDefaultAndCustomMessage(
                    "Expected <{0}> selected elements, but was <{1}>.",
                    new object[] { expectedCount, selected.Count() },
                    message,
                    parameters));
        }

        if (asserter != null)
        {
            foreach (var item in selected)
            {
                MessageHelper.Assert(
                    () => asserter(item),
                    MessageHelper.CombineDefaultAndCustomMessage("The assertion failed for item <{0}>.", new string[] { item?.ToString() ?? string.Empty }, message, parameters));
            }
        }

        return collection;
    }

    /// <summary>
    /// Verifies that each item in <paramref name="collection"/> is non-null.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// At least one item in <paramref name="collection"/> is null,
    /// or <paramref name="collection"/> is <c>null</c>.
    /// </exception>
    public static IEnumerable<T> AssertAllNotNull<T>(this IEnumerable<T> collection)
    {
        return AssertAllNotNull(collection, null);
    }

    /// <summary>
    /// Verifies that each item in <paramref name="collection"/> is non-null.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="message">The message to describe the assertion in case of failure.</param>
    /// <param name="parameters">Parameters for the <paramref name="message"/>.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// At least one item in <paramref name="collection"/> is null,
    /// or <paramref name="collection"/> is <c>null</c>.
    /// </exception>
    public static IEnumerable<T> AssertAllNotNull<T>(this IEnumerable<T> collection, string message, params object[] parameters)
    {
        Assert.IsNotNull(collection, MessageHelper.CombineDefaultAndCustomMessage("The collection must not be null.", message, parameters));

        var errorIndexes = collection
            .Select((o, i) => new { Item = o, Index = i })
            .Where(x => x.Item == null)
            .Select(x => x.Index)
            .ToList();

        if (errorIndexes.Count > 0)
        {
            throw new AssertFailedException(
                MessageHelper.CombineDefaultAndCustomMessage(
                    "The collection contains {0} items which are null; their indexes are <{1}>.",
                    new object[] { errorIndexes.Count, string.Join(", ", errorIndexes.Select(n => n.ToString(CultureInfo.InvariantCulture))) },
                    message,
                    parameters));
        }

        return collection;
    }

    /// <summary>
    /// Verifies that a sequence does not contain any items matching the
    /// specified <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// One or more elements in <paramref name="collection"/> match the <paramref name="predicate"/>,
    /// or <paramref name="collection"/> is <c>null</c>.
    /// </exception>
    public static IEnumerable<T> AssertContainsNot<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
    {
        return AssertContainsNot(collection, predicate, null);
    }

    /// <summary>
    /// Verifies that a sequence does not contain any items matching the
    /// specified <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="message">The message to describe the assertion in case of failure.</param>
    /// <param name="parameters">Parameters for the <paramref name="message"/>.</param>
    /// <returns><paramref name="collection"/>.</returns>
    /// <exception cref="AssertFailedException">
    /// One or more elements in <paramref name="collection"/> match the <paramref name="predicate"/>,
    /// or <paramref name="collection"/> is <c>null</c>.
    /// </exception>
    public static IEnumerable<T> AssertContainsNot<T>(this IEnumerable<T> collection, Func<T, bool> predicate, string message, params object[] parameters)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate), MessageHelper.CombineDefaultAndCustomMessage("The predicate must not be null.", message, parameters));

        Assert.IsNotNull(collection, MessageHelper.CombineDefaultAndCustomMessage("The collection must not be null.", message, parameters));

        var n = collection.Count(predicate);
        if (n > 0)
        {
            throw new AssertFailedException(
                MessageHelper.CombineDefaultAndCustomMessage(
                    "The collection contains {0} items that match the predicate; expected were 0.",
                    new object[] { n },
                    message,
                    parameters));
        }

        return collection;
    }
}

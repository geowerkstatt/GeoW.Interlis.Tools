namespace Geowerkstatt.Interlis.Compiler;

/// <summary>
/// A single data-driven compiler test case. The same instance can drive an AST/log assertion
/// (<see cref="TestTools.AssertReadFile"/> / <see cref="TestTools.AssertReadRule"/>) and the ili2c
/// comparison (<c>CompilerComparisonTest</c>), so some members only apply to one of those usages.
/// Authored through the <see cref="Rule"/> / <see cref="FullFile"/> factories (pulled in with
/// <c>using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;</c>) in a class's chapter-ordered <c>GetCases()</c>.
/// </summary>
/// <param name="Name">The case display name, shown in the test output and used by the comparison (with a per-class prefix).</param>
/// <param name="Input">
/// The INTERLIS source to compile. For full-file assertions and the ili2c comparison this is a complete
/// file (starting with <c>INTERLIS 2.4;</c>); for rule-level assertions it is the fragment that matches the
/// parser rule under test (e.g. a single type, attribute or model).
/// </param>
/// <param name="Description">
/// Optional prose explaining what the case covers and why it is expected to behave the way it does — the INTERLIS
/// rule under test, the reason an input is rejected, the RefHB argument behind an expectation. Written to the test
/// output verbatim, so it shows up next to a failing test instead of only in the source. Purely informational and
/// optional; a case whose <paramref name="Name"/> already says everything needs none.
/// </param>
/// <param name="ExpectedLog">
/// The diagnostic messages the Geowerkstatt compiler is expected to emit, in order. <see langword="null"/>
/// is treated as "no diagnostics" (an empty list). Used by the AST/log assertions; ignored by the ili2c
/// comparison, which only looks at whether each compiler produced any diagnostics at all.
/// </param>
/// <param name="Expected">
/// The expected AST the parsed result is deep-compared against when <paramref name="AssertOutput"/> is
/// <see langword="true"/>. <see langword="null"/> asserts the produced AST is also <see langword="null"/>.
/// Ignored by the ili2c comparison.
/// </param>
/// <param name="AssertOutput">
/// Whether to deep-compare the produced AST against <paramref name="Expected"/>. Set to
/// <see langword="false"/> when only the emitted diagnostics matter (e.g. error cases, or large files whose
/// full AST is not worth spelling out). Ignored by the ili2c comparison.
/// </param>
/// <param name="RefHB">
/// Optional INTERLIS reference-handbook paragraph tag in digit form (e.g. <c>"3.5.3-4"</c>), written to the
/// test output as a clickable link for traceability. Purely informational.
/// </param>
/// <param name="Ech0117">
/// Optional eCH-0117 meta-attribute tag, written to the test output as a clickable link. Purely informational.
/// </param>
/// <param name="Ili2cDivergenceReason">
/// The reason ili2c is known to disagree with the Geowerkstatt compiler on this input — one compiler accepts it
/// while the other rejects it — or <see langword="null"/> when both compilers agree (the default). When set, the
/// ili2c comparison asserts that the two compilers DIVERGE instead of agree, so the test turns red if a future
/// ili2c version changes its behaviour for this input — surfacing the change rather than silently masking it —
/// and the reason is written to the test output. State the direction (which compiler accepts/rejects) and, where
/// relevant, the RefHB rule that backs our behaviour.
/// </param>
/// <param name="IsFullFile">
/// Whether <paramref name="Input"/> is a complete file (full-file parse) rather than a parser-rule fragment
/// (rule-level parse). Not set by hand: the <see cref="Rule"/> / <see cref="FullFile"/> factories set it, and the
/// <see cref="RuleRows"/> / <see cref="FullFileRows"/> / <see cref="ComparisonRows"/> projections use it to route
/// the case to the right data source.
/// </param>
public record CompilationTestCase(
    string Name,
    string Input,
    string? Description = null,
    List<string>? ExpectedLog = null,
    object? Expected = null,
    bool AssertOutput = true,
    string? RefHB = null,
    string? Ech0117 = null,
    string? Ili2cDivergenceReason = null,
    bool IsFullFile = false)
{
    /// <summary>
    /// Marks <paramref name="testCase"/> as a rule-level fragment: verified by the rule parse and compared against
    /// ili2c once the class scaffold wraps it into a complete file (see <see cref="ComparisonRows"/>).
    /// </summary>
    public static CompilationTestCase Rule(CompilationTestCase testCase)
        => testCase with { IsFullFile = false };

    /// <summary>
    /// Marks <paramref name="testCase"/> as a complete file: verified by the full-file parse and compared against
    /// ili2c as-is. For scenarios a single fragment cannot express, e.g. multiple models or definitions that need
    /// surrounding context.
    /// </summary>
    public static CompilationTestCase FullFile(CompilationTestCase testCase)
        => testCase with { IsFullFile = true };

    /// <summary>The rule-level data source (fragment cases), for <c>ReadXxxDef</c>.</summary>
    public static IEnumerable<TestDataRow<CompilationTestCase>> RuleRows(IEnumerable<CompilationTestCase> cases)
    {
        foreach (var testCase in cases)
        {
            if (!testCase.IsFullFile)
            {
                yield return new(testCase, DisplayName: testCase.Name);
            }
        }
    }

    /// <summary>The full-file data source (complete-file cases), for <c>ReadFullFile</c>.</summary>
    public static IEnumerable<TestDataRow<CompilationTestCase>> FullFileRows(IEnumerable<CompilationTestCase> cases)
    {
        foreach (var testCase in cases)
        {
            if (testCase.IsFullFile)
            {
                yield return new(testCase, DisplayName: testCase.Name);
            }
        }
    }

    /// <summary>
    /// The comparison data source (the per-class <c>GetFullTestCases</c> discovered by <c>CompilerComparisonTest</c>):
    /// every case as a complete file — fragments wrapped by <paramref name="wrap"/>, full files as-is — with the
    /// display name prefixed by the class name.
    /// </summary>
    /// <param name="wrap">Scaffold wrapping a fragment into a complete file; required only if the class has fragment cases.</param>
    public static IEnumerable<TestDataRow<CompilationTestCase>> ComparisonRows(
        string className,
        IEnumerable<CompilationTestCase> cases,
        Func<string, string>? wrap = null)
        => ComparisonRows(className, cases, wrap is null ? null : (fragment, _) => wrap(fragment));

    /// <summary>
    /// Overload whose <paramref name="wrap"/> scaffold also receives the case <see cref="Name"/>, letting a class
    /// pick the wrapping per case — e.g. route boolean vs numeric fragments to different constraint contexts by a
    /// name prefix so that a valid fragment is accepted (not just trivially rejected) by both compilers.
    /// </summary>
    /// <param name="wrap">Scaffold wrapping a fragment into a complete file, given the fragment and the case name.</param>
    public static IEnumerable<TestDataRow<CompilationTestCase>> ComparisonRows(
        string className,
        IEnumerable<CompilationTestCase> cases,
        Func<string, string, string>? wrap)
    {
        foreach (var testCase in cases)
        {
            var input = testCase.IsFullFile
                ? testCase.Input
                : (wrap ?? throw new InvalidOperationException(
                    $"{className}: a rule-level case feeds the comparison but no scaffold was supplied to wrap it into a complete file."))(testCase.Input, testCase.Name);

            yield return new(
                testCase with { Input = input },
                DisplayName: className + ": " + testCase.Name,
                Categories: ["Comparison"]);
        }
    }
}

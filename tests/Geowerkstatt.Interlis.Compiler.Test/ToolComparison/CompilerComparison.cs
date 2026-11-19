using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using System.Globalization;
using System.Text;

namespace Geowerkstatt.Interlis.Compiler.Test.ToolComparison;

/// <summary>
/// An INTERLIS file plus the model names it defines. Used to resolve a model's imports during a comparison:
/// the Geowerkstatt compiler resolves them through an <see cref="IModelResolver"/> and ili2c through a model
/// directory.
/// </summary>
/// <param name="ModelNames">The names of the models defined in the file.</param>
/// <param name="Content">The INTERLIS source of the file.</param>
/// <param name="SourceUri">The URL the file was fetched from, used as the loaded models' source URI for diagnostics.</param>
public sealed record ModelFile(IReadOnlyList<string> ModelNames, string Content, string? SourceUri = null);

/// <summary>
/// Provides methods to compare compilation results between
/// Geowerkstatt.Interlis.Compiler and ch.interlis.ili2c-tool.
/// </summary>
public sealed class CompilerComparison : IDisposable
{
    private readonly string jarPath;
    private readonly ObjectPool<Ili2cContainer> containerPool;

    public CompilerComparison()
    {
        // Find ili2c JAR in output directory
        var jarFile = Path.Combine(AppContext.BaseDirectory, "ili2c.jar");
        jarPath = File.Exists(jarFile) ? jarFile : throw new FileNotFoundException(
            "ili2c.jar not found in output directory. Ensure the project was built successfully.");

        containerPool = new DefaultObjectPoolProvider().Create(new Ili2cContainerPoolPolicy(jarPath));
    }

    /// <summary>
    /// Compiles an INTERLIS source string using ili2c-tool (Java) and returns error messages. The
    /// <paramref name="dependencies"/> are written alongside the input so ili2c can resolve its imports from them.
    /// </summary>
    /// <param name="interlisSource">The INTERLIS source code to compile.</param>
    /// <param name="dependencies">The imported model files ili2c may resolve against.</param>
    /// <returns>Error messages from ili2c-tool, or empty string if successful.</returns>
    /// <exception cref="Ili2cCompilationTimeoutException">ili2c did not terminate within the compile timeout (it hung on the input).</exception>
    public async Task<string> CompileWithIli2cAsync(string interlisSource, IReadOnlyList<ModelFile> dependencies)
    {
        var container = containerPool.Get();
        try
        {
            var result = await container.RunCompileAsync(interlisSource, dependencies);
            containerPool.Return(container);
            return result;
        }
        catch
        {
            // After an exception it is unsafe to reuse the container
            container.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Compiles an INTERLIS source string using Geowerkstatt.Interlis.Compiler and returns error messages. Imported
    /// models are loaded on demand from <paramref name="dependencies"/>.
    /// </summary>
    /// <param name="interlisSource">The INTERLIS source code to compile.</param>
    /// <param name="dependencies">The imported model files the compiler may resolve against.</param>
    /// <returns>Error messages from the compiler, or empty string if successful.</returns>
    public async Task<string> CompileWithGeowerkstattAsync(string interlisSource, IReadOnlyList<ModelFile> dependencies)
    {
        var loggerProvider = new TestLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(loggerProvider).SetMinimumLevel(LogLevel.Error));

        var reader = new InterlisReader(loggerFactory);
        await reader.ReadModelWithImportsAsync(new StringReader(interlisSource), new DictionaryModelResolver(dependencies));

        return string.Join(Environment.NewLine, loggerProvider.GetMessages()).Trim();
    }

    /// <summary>
    /// Compiles a self-contained source with both compilers and asserts that they agree on whether it is valid.
    /// </summary>
    public Task CompareCompilersAsync(string interlisSource, string? ili2cDivergenceReason = null)
        => CompareCompilersAsync(interlisSource, Array.Empty<ModelFile>(), ili2cDivergenceReason);

    /// <summary>
    /// Compiles the source with both compilers, resolving its imports from <paramref name="dependencies"/>, and
    /// asserts that they agree on whether it is valid.
    /// </summary>
    /// <param name="interlisSource">The INTERLIS source code to compile.</param>
    /// <param name="dependencies">The imported model files both compilers may resolve against.</param>
    /// <param name="ili2cDivergenceReason">
    /// When <see langword="null"/> (the default) both compilers must agree (both accept or both reject the
    /// input). When set, the input is a known divergence and the compilers must DISAGREE (one accepts, the other
    /// rejects, or ili2c hangs while the Geowerkstatt compiler terminates); the reason is written to the test
    /// output, and the assertion fails if ili2c starts agreeing again, surfacing a behaviour change instead of
    /// masking it.
    /// </param>
    public async Task CompareCompilersAsync(string interlisSource, IReadOnlyList<ModelFile> dependencies, string? ili2cDivergenceReason = null)
    {
        if (ili2cDivergenceReason is not null)
        {
            TestContext.Current!.Output.WriteLine(string.Empty);
            TestContext.Current!.Output.WriteLine($"Known ili2c divergence: {ili2cDivergenceReason}");
        }

        var ili2cTask = CompileWithIli2cAsync(interlisSource, dependencies);
        var geowerkstattErrors = await CompileWithGeowerkstattAsync(interlisSource, dependencies);

        bool compilersAgree;
        try
        {
            var ili2cErrors = await ili2cTask;

            if (!string.IsNullOrEmpty(ili2cErrors))
            {
                TestContext.Current!.Output.WriteLine(string.Empty);
                TestContext.Current!.Output.WriteLine("ili2c-tool errors:");
                TestContext.Current!.Output.WriteLine(ili2cErrors);
            }

            compilersAgree = string.IsNullOrEmpty(geowerkstattErrors) == string.IsNullOrEmpty(ili2cErrors);
        }
        catch (Ili2cCompilationTimeoutException ex)
        {
            // ili2c hung on this input while the Geowerkstatt compiler terminated: the two tools behaved
            // differently, so a hang counts as a divergence in its own right (it must carry an ili2cDivergenceReason).
            TestContext.Current!.Output.WriteLine(string.Empty);
            TestContext.Current!.Output.WriteLine($"ili2c-tool: {ex.Message}");
            compilersAgree = false;
        }

        if (!string.IsNullOrEmpty(geowerkstattErrors))
        {
            TestContext.Current!.Output.WriteLine(string.Empty);
            TestContext.Current!.Output.WriteLine("Geowerkstatt.Interlis.Compiler errors:");
            TestContext.Current!.Output.WriteLine(geowerkstattErrors);
        }

        await Assert.That(compilersAgree).IsEqualTo<bool>(ili2cDivergenceReason is null)
            .Because(ili2cDivergenceReason is not null
                ? $"<Geowerkstatt.Interlis.Compiler> and <ili2c-tool> are expected to diverge on this input ({ili2cDivergenceReason}); if they now agree, ili2c changed its behaviour and the test case should be revisited"
                : "<Geowerkstatt.Interlis.Compiler> should accept or reject the input in the same way as the <ili2c-tool> compiler does");
    }

    public void Dispose()
    {
        (containerPool as IDisposable)?.Dispose();
    }

    /// <summary>Resolves imported models for the Geowerkstatt compiler from a fixed set of <see cref="ModelFile"/>s.</summary>
    private sealed class DictionaryModelResolver : IModelResolver
    {
        private readonly Dictionary<string, ModelFile> fileByModelName;

        public DictionaryModelResolver(IReadOnlyList<ModelFile> dependencies)
        {
            fileByModelName = new Dictionary<string, ModelFile>(StringComparer.Ordinal);
            foreach (var dependency in dependencies)
            {
                foreach (var modelName in dependency.ModelNames)
                {
                    fileByModelName.TryAdd(modelName, dependency);
                }
            }
        }

        // The dependency set of a single case is resolved for one INTERLIS language version (see the case source),
        // so a model name identifies at most one file and languageVersion needs no further disambiguation here.
        public ValueTask<(TextReader Reader, string? SourceUri)?> OpenModelAsync(string modelName, double? languageVersion, CancellationToken cancellationToken) => new(Open(modelName));

        private (TextReader Reader, string? SourceUri)? Open(string modelName)
            => fileByModelName.TryGetValue(modelName, out var file)
                ? (new StringReader(file.Content), file.SourceUri)
                : null;
    }

    /// <summary>
    /// Thrown when ili2c does not finish compiling within the compile timeout. The comparison treats a hang as a
    /// divergence (ili2c behaved differently from the Geowerkstatt compiler, which terminated).
    /// </summary>
    private sealed class Ili2cCompilationTimeoutException(TimeSpan timeout)
        : Exception($"ili2c did not finish compiling within {timeout.TotalSeconds:0.#}s (hung; recorded as a divergence)")
    {
    }

    private sealed class Ili2cContainerPoolPolicy(string jarPath) : IPooledObjectPolicy<Ili2cContainer>
    {
        public Ili2cContainer Create() => new(jarPath);

        // A container whose compilation threw is disposed by CompileWithIli2cAsync and never returned.
        public bool Return(Ili2cContainer obj) => true;
    }

    /// <summary>
    /// A long-lived container holding the ili2c jar. Every compilation runs as its own exec in it, so a pooled
    /// container serves many compilations while being started only once.
    /// </summary>
    private sealed class Ili2cContainer(string jarPath) : IDisposable
    {
        private const string JarDirectory = "/jar";

        // ili2c hangs (never returns) on some malformed inputs. Kept comfortably above a normal ili2c run (~1-2s) so it
        // never trips a legitimate compilation.
        private static readonly TimeSpan CompileTimeout = TimeSpan.FromSeconds(10);

        // The exit code of coreutils `timeout` when the command ran into the time limit.
        private const long TimedOutExitCode = 124;

        // Every compilation starts a fresh, short-lived JVM, and one such JVM runs in parallel per test. By default
        // each JVM spreads JIT compilation and GC over all cores, which oversubscribes the machine several times
        // over. Restricting it to the C1 JIT, the serial GC and one core lowers its CPU use and shortens the run.
        private static readonly string[] JvmOptions = ["-XX:TieredStopAtLevel=1", "-XX:+UseSerialGC", "-XX:ActiveProcessorCount=1"];

        // The jar is copied in through the Docker API rather than bind-mounted, so it also reaches a daemon that
        // runs on a different host than the tests (e.g. a containerized CI agent talking to the host daemon).
        private readonly IContainer container = new ContainerBuilder("eclipse-temurin:17-jre")
            .WithEntrypoint("sleep", "infinity")
            .WithResourceMapping(FilePath.Of(jarPath), DirectoryPath.Of(JarDirectory))
            .WithLogger(NullLogger.Instance) // it logs every copy and exec, which would flood each test's output
            .Build();

        private readonly string jarName = Path.GetFileName(jarPath);
        private bool started;

        public async Task<string> RunCompileAsync(string targetSource, IReadOnlyList<ModelFile> dependencies)
        {
            if (!started)
            {
                await container.StartAsync();
                started = true;
            }

            // Each compilation gets its own work directory inside the (pooled, reused) container so leftover files
            // from a previous run cannot be picked up when ili2c scans the model directory for imports.
            var workDir = $"/work/{TestContext.Current!.Isolation.UniqueId}";
            var targetFile = $"{workDir}/model.ili";

            await container.CopyAsync(Encoding.UTF8.GetBytes(targetSource), targetFile);
            for (var i = 0; i < dependencies.Count; i++)
            {
                await container.CopyAsync(Encoding.UTF8.GetBytes(dependencies[i].Content), $"{workDir}/dep{i}.ili");
            }

            // --modeldir confines model resolution to the work directory (the target and its written dependencies),
            // so ili2c resolves imports from those files and never reaches out to the default network repository.
            // The timeout runs inside the container: cancelling an exec from outside would leave ili2c running.
            // The work directory is removed in the same exec, as every exec costs about 0.1s of CPU time.
            var result = await container.ExecAsync([
                "sh", "-c", """dir="$1"; shift; "$@"; status=$?; rm -rf "$dir"; exit $status""", "sh", workDir,
                "timeout", CompileTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                "java", .. JvmOptions, "-jar", $"{JarDirectory}/{jarName}",
                "--without-warnings", "--quiet", "--modeldir", workDir, targetFile]);

            if (result.ExitCode == TimedOutExitCode)
            {
                // ili2c hung on this input; surface a dedicated exception so the comparison can record the hang
                // as a divergence instead of failing the whole run.
                throw new Ili2cCompilationTimeoutException(CompileTimeout);
            }

            return (result.Stdout + result.Stderr).Trim();
        }

        public void Dispose() => container.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

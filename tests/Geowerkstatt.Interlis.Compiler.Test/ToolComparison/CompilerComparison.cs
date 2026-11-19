using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using System.Security.Cryptography;
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
    // The Docker build context of the worker image: its Dockerfile and source plus the ili2c.jar downloaded during build.
    private static readonly string WorkerDirectory = Path.Combine(AppContext.BaseDirectory, "ili2c-worker");

    // Shared by all comparisons of a test run and kept across runs (see BuildWorkerImageAsync).
    private static readonly Lazy<Task<IFutureDockerImage>> WorkerImage = new(BuildWorkerImageAsync);

    private readonly ObjectPool<Ili2cContainer> containerPool;

    public CompilerComparison()
    {
        if (!File.Exists(Path.Combine(WorkerDirectory, "ili2c.jar")))
        {
            throw new FileNotFoundException("ili2c.jar not found in output directory. Ensure the project was built successfully.");
        }

        containerPool = new DefaultObjectPoolProvider().Create(new Ili2cContainerPoolPolicy());
    }

    /// <summary>
    /// Builds the worker image unless it already exists. The tag is a hash of the build context, so a changed
    /// Dockerfile, worker or ili2c version gets a new image, while an unchanged one is reused across test runs
    /// instead of being rebuilt (about 15s) every time.
    /// </summary>
    private static async Task<IFutureDockerImage> BuildWorkerImageAsync()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in Directory.GetFiles(WorkerDirectory).Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetFileName(file)));
            hash.AppendData(await File.ReadAllBytesAsync(file));
        }

        var tag = Convert.ToHexStringLower(hash.GetHashAndReset())[..16];
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(WorkerDirectory)
            .WithName($"ili2c-comparison-worker:{tag}")
            .WithImageBuildPolicy(PullPolicy.Missing)
            .WithDeleteIfExists(false)
            .WithCleanUp(false)
            .WithLogger(NullLogger.Instance)
            .Build();
        await image.CreateAsync();
        return image;
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

    private sealed class Ili2cContainerPoolPolicy : IPooledObjectPolicy<Ili2cContainer>
    {
        public Ili2cContainer Create() => new();

        // A container whose compilation threw is disposed by CompileWithIli2cAsync and never returned.
        public bool Return(Ili2cContainer obj) => true;
    }

    /// <summary>
    /// A container running the ili2c worker (see Ili2cWorker/Ili2cWorker.java): one warm JVM that compiles a model
    /// per HTTP request, so a compilation pays neither a JVM startup nor a Docker exec.
    /// </summary>
    private sealed class Ili2cContainer : IDisposable
    {
        private const ushort WorkerPort = 8080;

        // ili2c hangs (never returns) on some malformed inputs. Kept comfortably above a normal ili2c run so it
        // never trips a legitimate compilation, including the first, still cold one of a container.
        private static readonly TimeSpan CompileTimeout = TimeSpan.FromSeconds(10);

        private IContainer? container;
        private HttpClient? worker;

        private async Task StartAsync()
        {
            container = new ContainerBuilder(await WorkerImage.Value)
                .WithPortBinding(WorkerPort, assignRandomHostPort: true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("^ready$"))
                .WithLogger(NullLogger.Instance) // it logs every copy, which would flood each test's output
                .Build();
            await container.StartAsync();

            // The worker is reached directly on its mapped port, never through a proxy configured for the machine.
            worker = new HttpClient(new HttpClientHandler { UseProxy = false })
            {
                BaseAddress = new UriBuilder("http", container.Hostname, container.GetMappedPublicPort(WorkerPort)).Uri,
                Timeout = CompileTimeout,
            };
        }

        public async Task<string> RunCompileAsync(string targetSource, IReadOnlyList<ModelFile> dependencies)
        {
            if (worker is null)
            {
                await StartAsync();
            }

            // Each compilation gets its own work directory inside the (pooled, reused) container so leftover files
            // from a previous run cannot be picked up when ili2c scans the model directory for imports. The worker
            // deletes it after the compilation.
            var workDir = $"/work/{TestContext.Current!.Isolation.UniqueId}";
            await container!.CopyAsync(Encoding.UTF8.GetBytes(targetSource), $"{workDir}/model.ili");
            for (var i = 0; i < dependencies.Count; i++)
            {
                await container.CopyAsync(Encoding.UTF8.GetBytes(dependencies[i].Content), $"{workDir}/dep{i}.ili");
            }

            try
            {
                using var response = await worker!.PostAsync("compile", new StringContent(workDir));
                var output = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"The ili2c worker failed ({(int)response.StatusCode}): {output}");
                }

                return output.Trim();
            }
            catch (TaskCanceledException)
            {
                // ili2c hung on this input; surface a dedicated exception so the comparison can record the hang as a
                // divergence instead of failing the whole run. The caller disposes the container and with it the
                // JVM stuck in the compilation.
                throw new Ili2cCompilationTimeoutException(CompileTimeout);
            }
        }

        public void Dispose()
        {
            worker?.Dispose();
            container?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}

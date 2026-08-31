using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics;
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
    private readonly ObjectPool<DockerContainer> containerPool;

    public CompilerComparison()
    {
        // Find ili2c JAR in output directory
        var jarFile = Path.Combine(AppContext.BaseDirectory, "ili2c.jar");
        jarPath = File.Exists(jarFile) ? jarFile : throw new FileNotFoundException(
            "ili2c.jar not found in output directory. Ensure the project was built successfully.");

        containerPool = new DefaultObjectPoolProvider().Create(new DockerContainerPoolPolicy(jarPath));
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
    public string CompileWithGeowerkstatt(string interlisSource, IReadOnlyList<ModelFile> dependencies)
    {
        var loggerProvider = new TestLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(loggerProvider).SetMinimumLevel(LogLevel.Error));

        var reader = new InterlisReader(loggerFactory);
        reader.ReadModelWithImports(new StringReader(interlisSource), new DictionaryModelResolver(dependencies));

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
        var geowerkstattErrors = CompileWithGeowerkstatt(interlisSource, dependencies);

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
        public (TextReader Reader, string? SourceUri)? OpenModel(string modelName, double? languageVersion)
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

    private sealed class DockerContainerPoolPolicy(string jarPath) : IPooledObjectPolicy<DockerContainer>
    {
        public DockerContainer Create() => new(jarPath);

        public bool Return(DockerContainer obj) => obj.IsAlive;
    }

    /// <summary>
    /// Encapsulates a single persistent Docker container running /bin/sh.
    /// </summary>
    private sealed class DockerContainer(string jarPath) : IDisposable
    {
        private const string ReadyMarker = "---READY---";
        private const string WriteMarker = "---WRITE-END---";
        private const string OutputMarker = "---COMPILE-END---";
        private readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        // The compile step is bounded more tightly than container startup: ili2c hangs (never returns) on some
        // malformed inputs. Kept comfortably above a normal ili2c run (~1-2s) so it never trips a legitimate compilation.
        private readonly TimeSpan CompileTimeout = TimeSpan.FromSeconds(10);

        private readonly string jarPath = jarPath;

        private Process? process;
        private StreamWriter? stdin;
        private StreamReader? stdout;
        private string? containerId;

        public bool IsAlive => process != null && !process.HasExited;

        private async Task<string> ReadUntilMarkerAsync(string marker, TimeSpan timeout, string timeoutMessage)
        {
            using var cts = new CancellationTokenSource(timeout);
            var output = new StringBuilder();

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var line = await stdout!.ReadLineAsync(cts.Token)
                        ?? throw new InvalidOperationException("Docker container stdout stream ended unexpectedly");
                    if (line.Contains(marker))
                    {
                        break;
                    }

                    output.AppendLine(line);
                }
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException(timeoutMessage);
            }

            return output.ToString().Trim();
        }

        private async Task WriteAsync(string command)
        {
            await stdin!.WriteAsync(command);
            await stdin.FlushAsync();
        }

        /// <summary>Writes <paramref name="content"/> to <paramref name="containerPath"/> inside the container, base64-encoded to avoid shell escaping issues.</summary>
        private async Task WriteFileAsync(string containerPath, string content)
        {
            var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
            await WriteAsync($"echo '{base64Content}' | base64 -d > {containerPath}\n");
        }

        private async Task StartAsync()
        {
            if (IsAlive) return;

            var cidFilePath = Path.GetTempFileName();
            File.Delete(cidFilePath); // docker requires the file to not exist yet

            // The jar is copied into the container with `docker cp` after the shell is ready
            // (see below) rather than bind-mounted. A bind mount (`-v`) only works when the jar's
            // directory is shared with the Docker daemon, which is not the case when the daemon runs
            // on a different host than the test process (e.g. a containerized CI agent talking to the
            // host daemon, where the path resolves in the host namespace and fails with
            // "path not shared"). `docker cp` streams the file through the CLI and works regardless.
            var dockerArgs = $"run --rm -i " +
                $"--cidfile \"{cidFilePath}\" " +
                $"-w /work " +
                $"eclipse-temurin:17-jre " +
                $"/bin/sh";

            process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = dockerArgs,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            })!;

            stdin = process.StandardInput;
            stdout = process.StandardOutput;

            // Wait for the shell to be ready by sending a probe command
            await WriteAsync($"mkdir -p /work /jar && echo && echo '{ReadyMarker}'\n");

            await ReadUntilMarkerAsync(ReadyMarker, Timeout, $"Docker container did not become ready within {Timeout:g}");

            containerId = File.ReadAllText(cidFilePath).Trim();
            File.Delete(cidFilePath);

            // Copy the ili2c jar into the container (see the note above on why it is not bind-mounted).
            using var copyProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"cp \"{jarPath}\" \"{containerId}:/jar/{Path.GetFileName(jarPath)}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            })!;
            await copyProcess.WaitForExitAsync();
            if (copyProcess.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to copy the ili2c jar into container {containerId} (docker cp exit code {copyProcess.ExitCode}).");
            }
        }

        public async Task<string> RunCompileAsync(string targetSource, IReadOnlyList<ModelFile> dependencies)
        {
            await StartAsync();

            // Each compilation gets its own work directory inside the (pooled, reused) container so leftover files
            // from a previous run cannot be picked up when ili2c scans the model directory for imports.
            var workDir = $"/work/{TestContext.Current!.Isolation.UniqueId}";
            var targetFile = $"{workDir}/model.ili";

            await WriteAsync($"rm -rf {workDir} && mkdir -p {workDir}\n");
            await WriteFileAsync(targetFile, targetSource);
            for (var i = 0; i < dependencies.Count; i++)
            {
                await WriteFileAsync($"{workDir}/dep{i}.ili", dependencies[i].Content);
            }

            await WriteAsync($"echo && echo '{WriteMarker}'\n");
            await ReadUntilMarkerAsync(WriteMarker, Timeout, $"Docker container did not finish writing the input files within {Timeout:g}");

            // --modeldir confines model resolution to the work directory (the target and its written dependencies),
            // so ili2c resolves imports from those files and never reaches out to the default network repository.
            var jarName = Path.GetFileName(jarPath);
            var command = $"java -jar /jar/{jarName} --without-warnings --quiet --modeldir {workDir} {targetFile} 2>&1; rm -rf {workDir}; echo && echo '{OutputMarker}'\n";
            await WriteAsync(command);

            try
            {
                return await ReadUntilMarkerAsync(OutputMarker, CompileTimeout, $"ili2c did not finish compiling within {CompileTimeout:g}");
            }
            catch (TimeoutException)
            {
                // ili2c hung on this input; surface a dedicated exception so the comparison can record the hang
                // as a divergence instead of failing the whole run with a raw timeout.
                throw new Ili2cCompilationTimeoutException(CompileTimeout);
            }
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(containerId))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"kill {containerId}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(5000);
                }
                catch
                {
                    // Ignore errors during container kill
                }
            }

            process?.Dispose();
        }
    }
}

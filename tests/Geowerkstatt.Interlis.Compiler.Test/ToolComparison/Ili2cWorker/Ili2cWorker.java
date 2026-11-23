import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpServer;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.PrintStream;
import java.io.PrintWriter;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.Permission;
import java.util.Comparator;
import java.util.stream.Stream;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

/**
 * Keeps one warm JVM with ili2c loaded, so a compilation does not pay the JVM startup and JIT warm-up. Each
 * POST /compile sends a ZIP archive holding model.ili and the files it imports; the response is ili2c's console
 * output (empty when the model is valid).
 *
 * Requests are handled one at a time on the server's dispatcher thread: ili2c logs through a process-wide
 * singleton (EhiLogger), so two compilations in the same JVM would mix their messages.
 */
public final class Ili2cWorker {
    private static final int PORT = 8080;

    /** Thrown instead of terminating the JVM when ili2c calls System.exit at the end of a compilation. */
    private static final class ExitTrapped extends SecurityException {
    }

    public static void main(String[] args) throws IOException {
        // ili2c's Main.main ends with System.exit. A security manager is the only way to keep the JVM alive without
        // re-implementing its option handling; it needs Java 23 or older, hence the pinned image in the Dockerfile.
        System.setSecurityManager(new SecurityManager() {
            @Override
            public void checkPermission(Permission permission) {
            }

            @Override
            public void checkPermission(Permission permission, Object context) {
            }

            @Override
            public void checkExit(int status) {
                throw new ExitTrapped();
            }
        });

        HttpServer server = HttpServer.create(new InetSocketAddress(PORT), 0);
        server.createContext("/compile", Ili2cWorker::handleCompile);
        server.start();
        System.out.println("ready"); // the wait strategy of the test container watches for this line
    }

    private static void handleCompile(HttpExchange exchange) throws IOException {
        int status = 200;
        String response;
        try {
            // A fresh work directory per compilation, so ili2c never picks up files of a previous one when it scans
            // the directory for imports.
            Path workDir = Files.createTempDirectory("compile");
            try {
                unzip(exchange.getRequestBody(), workDir);
                response = compile(workDir);
            } finally {
                deleteRecursively(workDir);
            }
        } catch (Throwable e) {
            status = 500;
            ByteArrayOutputStream trace = new ByteArrayOutputStream();
            e.printStackTrace(new PrintWriter(trace, true, StandardCharsets.UTF_8));
            response = trace.toString(StandardCharsets.UTF_8);
        }

        byte[] body = response.getBytes(StandardCharsets.UTF_8);
        exchange.sendResponseHeaders(status, body.length == 0 ? -1 : body.length);
        try (OutputStream out = exchange.getResponseBody()) {
            out.write(body);
        }
    }

    /** Runs ili2c exactly as its command line would and returns what it printed. */
    private static String compile(Path workDir) {
        // --modeldir confines model resolution to the work directory (the target and its written dependencies),
        // so ili2c resolves imports from those files and never reaches out to the default network repository.
        String[] arguments = {
            "--without-warnings", "--quiet", "--modeldir", workDir.toString(), workDir.resolve("model.ili").toString()
        };

        ByteArrayOutputStream output = new ByteArrayOutputStream();
        PrintStream capture = new PrintStream(output, true, StandardCharsets.UTF_8);
        PrintStream stdout = System.out;
        PrintStream stderr = System.err;
        System.setOut(capture);
        System.setErr(capture);
        try {
            ch.interlis.ili2c.Main.main(arguments);
        } catch (ExitTrapped e) {
            // ili2c finished; whether the model is valid shows in its output
        } finally {
            System.setOut(stdout);
            System.setErr(stderr);
        }

        return output.toString(StandardCharsets.UTF_8);
    }

    private static void unzip(InputStream archive, Path workDir) throws IOException {
        try (ZipInputStream zip = new ZipInputStream(archive)) {
            for (ZipEntry entry = zip.getNextEntry(); entry != null; entry = zip.getNextEntry()) {
                Files.copy(zip, workDir.resolve(entry.getName()));
            }
        }
    }

    private static void deleteRecursively(Path directory) throws IOException {
        try (Stream<Path> paths = Files.walk(directory)) {
            for (Path path : paths.sorted(Comparator.reverseOrder()).toList()) {
                Files.delete(path);
            }
        }
    }
}

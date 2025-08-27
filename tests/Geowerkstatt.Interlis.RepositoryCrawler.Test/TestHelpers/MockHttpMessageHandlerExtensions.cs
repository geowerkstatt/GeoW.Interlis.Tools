using RichardSzalay.MockHttp;
using System.Net;

namespace Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;

public static class MockHttpMessageHandlerExtensions
{
    /// <summary>
    /// Sets up the <see cref="MockHttpMessageHandler"/> with setups to retrieve the files in the Testdata folder.
    /// </summary>
    /// <returns>A dictionary with the URLs as keys and the <see cref="MockedRequest"/> as value. Useful to Verify the calls.</returns>
    public static Dictionary<string, MockedRequest> SetupHttpMockForTestdataFiles(this MockHttpMessageHandler mockHttp)
    {
        var mockRequests = new Dictionary<string, MockedRequest>();
        var testdataRoot = "./Testdata";

        foreach (var file in Directory.EnumerateFiles(testdataRoot, "*.*", SearchOption.AllDirectories))
        {
            // Build URL based on relative path from Testdata folder, using '/' as separator
            var relativePath = Path.GetRelativePath(testdataRoot, file).Replace('\\', '/');
            var url = $"https://{relativePath}";

            mockRequests.Add(url, mockHttp
                .When(url)
                .Respond("application/xml", new FileStream(file, FileMode.Open, FileAccess.Read)));
        }

        // Setup HEAD requests for each top-level directory
        foreach (var dir in Directory.GetDirectories(testdataRoot))
        {
            var dirName = Path.GetFileName(dir);
            mockHttp
                .When(HttpMethod.Head, $"https://{dirName}/")
                .Respond(HttpStatusCode.OK);
        }

        mockHttp.Fallback.Respond(HttpStatusCode.NotFound);
        return mockRequests;
    }
}

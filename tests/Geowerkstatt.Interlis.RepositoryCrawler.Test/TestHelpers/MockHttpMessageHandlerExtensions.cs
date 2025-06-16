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
        foreach (var dir in Directory.GetDirectories("./Testdata"))
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                var url = $"https://{Path.GetFileName(dir)}/{Path.GetFileName(file)}";
                mockRequests.Add(url, mockHttp
                    .When(url)
                    .Respond("application/xml", new FileStream(file, FileMode.Open, FileAccess.Read)));
            }

            mockHttp
                .When(HttpMethod.Head, $"https://{Path.GetFileName(dir)}/")
                .Respond(HttpStatusCode.OK);
        }

        mockHttp.Fallback.Respond(HttpStatusCode.NotFound);
        return mockRequests;
    }
}

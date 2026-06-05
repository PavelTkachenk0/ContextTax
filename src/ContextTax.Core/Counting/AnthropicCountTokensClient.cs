using System.Net;
using System.Text;

namespace ContextTax.Core.Counting;

/// <summary>
/// HTTP transport for Anthropic's count_tokens endpoint. One job: POST a JSON payload and
/// return the response body (retrying once on 429; throwing on a non-success status).
/// </summary>
public sealed class AnthropicCountTokensClient
{
    private const string Endpoint = "https://api.anthropic.com/v1/messages/count_tokens";
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public AnthropicCountTokensClient(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public async Task<string> PostAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(payloadJson, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.TooManyRequests) // one bounded retry on 429
        {
            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            response = await SendAsync(payloadJson, cancellationToken).ConfigureAwait(false);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new TokenCountException((int)response.StatusCode, body);
            return body;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string payloadJson, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        return await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

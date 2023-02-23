using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace C_Sharp;

public class ApiCaller
{
    private readonly List<string>? headersToPropagate;
    private readonly HttpClient httpClient;
    private readonly IHttpContextAccessor? httpContextAccessor;
    private readonly List<HttpMethod> methodsWithContent = new List<HttpMethod>() { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch };

    public ApiCaller(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public ApiCaller(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, List<string> headersToPropagate)
    {
        this.httpClient = httpClient;
        this.httpContextAccessor = httpContextAccessor;
        this.headersToPropagate = headersToPropagate;

        foreach (var header in headersToPropagate)
        {
            if (this.httpContextAccessor.HttpContext.Request.Headers.TryGetValue(header, out StringValues stringValues))
            {
                this.httpClient.DefaultRequestHeaders.Add(header, stringValues.ToArray());
            }
        }
    }

    public void AddAuthenticationHeader(string scheme, string? parameter)
    {
        this.httpClient.DefaultRequestHeaders.Authorization ??= new AuthenticationHeaderValue(scheme, parameter);
    }

    public void AddHeaders(Dictionary<string, string> headersToAdd)
    {
        foreach (var header in headersToAdd)
        {
            this.httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    public Task<HttpResponseMessage> Get(string uri)
    {
        return this.Call(HttpMethod.Get, uri, null);
    }

    public Task<HttpResponseMessage> Get(Uri uri)
    {
        return this.Call(HttpMethod.Get, uri, null);
    }

    public Task<HttpResponseMessage> Post(string uri, object bodyObject)
    {
        return this.Call(HttpMethod.Post, uri, bodyObject);
    }
    
    public Task<HttpResponseMessage> Post(Uri uri, object bodyObject)
    {
        return this.Call(HttpMethod.Post, uri, bodyObject);
    }

    public Task<HttpResponseMessage> Put(string uri, object bodyObject)
    {
        return this.Call(HttpMethod.Put, uri, bodyObject);
    }

    public Task<HttpResponseMessage> Put(Uri uri, object bodyObject)
    {
        return this.Call(HttpMethod.Put, uri, bodyObject);
    }

    private Task<HttpResponseMessage> Call(HttpMethod httpMethod, string uri, object? bodyObject)
    {
        return this.Call(httpMethod, new Uri(uri), bodyObject);
    }

    private Task<HttpResponseMessage> Call(HttpMethod httpMethod, Uri uri, object? bodyObject)
    {
        var httpRequestMessage = new HttpRequestMessage()
        {
            RequestUri = uri,
            Method = httpMethod,
        };

        if (methodsWithContent.Contains(httpMethod) && bodyObject != null)
        {
            var content = bodyObject.GetType() == typeof(string) ? (string)bodyObject : JsonConvert.SerializeObject(bodyObject);

            httpRequestMessage.Content = new StringContent(content);
        }

        return httpClient.SendAsync(httpRequestMessage);
    }
}
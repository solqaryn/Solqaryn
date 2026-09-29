using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace InventoryApp.API.Filters;

public enum PublicHttpCacheProfile
{
    Bootstrap,
    Identity,
    Categories,
    Products
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PublicHttpCacheAttribute : Attribute, IAsyncResultFilter
{
    private readonly PublicHttpCacheProfile _profile;

    public PublicHttpCacheAttribute(PublicHttpCacheProfile profile)
    {
        _profile = profile;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;

        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            await next();
            return;
        }

        if (!TryGetSuccessfulObject(context.Result, out var payload))
        {
            SetNoStore(response);
            await next();
            return;
        }

        var jsonOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value
            .JsonSerializerOptions;
        var etag = ComputeWeakETag(payload, jsonOptions);

        response.Headers[HeaderNames.CacheControl] = GetCacheControl(_profile);
        response.Headers[HeaderNames.ETag] = etag;
        AppendVary(response.Headers, HeaderNames.AcceptEncoding);

        if (MatchesIfNoneMatch(request.Headers[HeaderNames.IfNoneMatch], etag))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        await next();
    }

    internal static string GetCacheControl(PublicHttpCacheProfile profile) =>
        profile switch
        {
            PublicHttpCacheProfile.Identity => "public, max-age=120, s-maxage=300, stale-while-revalidate=600",
            PublicHttpCacheProfile.Categories => "public, max-age=120, s-maxage=300, stale-while-revalidate=600",
            PublicHttpCacheProfile.Bootstrap => "public, max-age=15, s-maxage=30, stale-while-revalidate=60",
            PublicHttpCacheProfile.Products => "public, max-age=5, s-maxage=15, must-revalidate",
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };

    internal static string ComputeWeakETag(object? payload, JsonSerializerOptions jsonOptions)
    {
        var payloadType = payload?.GetType() ?? typeof(object);
        var serialized = JsonSerializer.SerializeToUtf8Bytes(payload, payloadType, jsonOptions);
        var digest = Convert.ToHexString(SHA256.HashData(serialized)).ToLowerInvariant();
        return $"W/\"{digest}\"";
    }

    internal static bool MatchesIfNoneMatch(IEnumerable<string?> headerValues, string etag)
    {
        foreach (var headerValue in headerValues)
        {
            if (string.IsNullOrWhiteSpace(headerValue)) continue;

            foreach (var candidate in headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (candidate == "*" || string.Equals(candidate, etag, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    internal static void SetNoStore(HttpResponse response)
    {
        response.Headers[HeaderNames.CacheControl] = "private, no-store, max-age=0";
        response.Headers.Remove(HeaderNames.ETag);
    }

    private static bool TryGetSuccessfulObject(IActionResult result, out object? payload)
    {
        if (result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
            if (statusCode is >= 200 and < 300)
            {
                payload = objectResult.Value;
                return true;
            }
        }

        payload = null;
        return false;
    }

    private static void AppendVary(IHeaderDictionary headers, string value)
    {
        var existing = headers[HeaderNames.Vary]
            .SelectMany(item => item?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (existing.Add(value))
            headers[HeaderNames.Vary] = string.Join(", ", existing);
    }
}

using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Plugin.PotPlayerBridge.Web;

/// <summary>
/// Injects the embedded client script into Jellyfin Web without modifying files on disk.
/// </summary>
public sealed class IndexHtmlInjectionFilter : IStartupFilter
{
    private const string Marker = "Plugins/PotPlayerBridge/client.js";
    private static readonly string ScriptVersion =
        typeof(IndexHtmlInjectionFilter).Module.ModuleVersionId.ToString("N");
    private readonly ILogger<IndexHtmlInjectionFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexHtmlInjectionFilter"/> class.
    /// </summary>
    public IndexHtmlInjectionFilter(ILogger<IndexHtmlInjectionFilter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(InjectAsync);
            next(app);
        };
    }

    private async Task InjectAsync(HttpContext context, Func<Task> nextMiddleware)
    {
        if (!IsIndexRequest(context.Request))
        {
            await nextMiddleware().ConfigureAwait(false);
            return;
        }

        var originalBody = context.Response.Body;
        var originalAcceptEncoding = context.Request.Headers.AcceptEncoding;
        await using var capturedBody = new MemoryStream();
        context.Response.Body = capturedBody;
        // ResponseCompressionMiddleware runs inside this startup filter on Jellyfin.
        // Request an identity response so the captured body is HTML rather than gzip/br
        // bytes. The final index is only a few KiB and is deliberately marked no-store.
        context.Request.Headers.AcceptEncoding = new StringValues("identity");
        try
        {
            await nextMiddleware().ConfigureAwait(false);
        }
        finally
        {
            context.Request.Headers.AcceptEncoding = originalAcceptEncoding;
            context.Response.Body = originalBody;
        }

        capturedBody.Position = 0;
        var contentType = context.Response.ContentType ?? string.Empty;
        var contentEncoding = context.Response.Headers.ContentEncoding.ToString();
        if (!contentType.Contains("html", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(contentEncoding) && !contentEncoding.Equals("identity", StringComparison.OrdinalIgnoreCase)))
        {
            await capturedBody.CopyToAsync(originalBody).ConfigureAwait(false);
            return;
        }

        string html;
        using (var reader = new StreamReader(capturedBody, Encoding.UTF8, leaveOpen: true))
        {
            html = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(html) || html.Contains(Marker, StringComparison.Ordinal))
        {
            capturedBody.Position = 0;
            await capturedBody.CopyToAsync(originalBody).ConfigureAwait(false);
            return;
        }

        var prefix = context.Request.PathBase.HasValue
            ? context.Request.PathBase.Value!.TrimEnd('/')
            : string.Empty;
        var scriptTag = $"<script src=\"{prefix}/Plugins/PotPlayerBridge/client.js?v={ScriptVersion}\" defer></script>";
        var bodyEnd = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        var modified = bodyEnd >= 0
            ? html.Insert(bodyEnd, scriptTag)
            : html + scriptTag;
        var bytes = Encoding.UTF8.GetBytes(modified);
        context.Response.ContentLength = bytes.Length;
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
        await originalBody.WriteAsync(bytes).ConfigureAwait(false);
        _logger.LogDebug("Injected PotPlayer Bridge client script into {Path}", context.Request.Path);
    }

    private static bool IsIndexRequest(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? string.Empty;
        return path.Equals("/", StringComparison.Ordinal) ||
               path.Equals("/web", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/web/", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/web/index.html", StringComparison.OrdinalIgnoreCase);
    }
}

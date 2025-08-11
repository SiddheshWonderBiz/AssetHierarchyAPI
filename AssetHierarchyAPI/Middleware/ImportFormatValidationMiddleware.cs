using AssetHierarchyAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class ImportFormatValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _allowedFormat;
    private readonly ILoggingService _logger;

    public ImportFormatValidationMiddleware(RequestDelegate next, IConfiguration configuration, ILoggingService logger)
    {
        _next = next;
        _logger = logger;
        _allowedFormat = configuration["StorageType"]?.Trim()?.ToLowerInvariant() ?? "json";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/Hierarchy/upload", StringComparison.OrdinalIgnoreCase)
            && context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.Request.HasFormContentType)
            {
                _logger.LogError("Upload rejected: request is not multipart/form-data.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid upload request." });
                return;
            }

            context.Request.EnableBuffering();

            try
            {
                var form = await context.Request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();
                if (file == null)
                {
                    _logger.LogError("Upload rejected: no file found in request.");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { error = "No file provided." });
                    return;
                }

                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                bool isJson = ext == ".json";
                bool isXml = ext == ".xml";

                if ((_allowedFormat == "json" && !isJson) || (_allowedFormat == "xml" && !isXml))
                {
                    _logger.LogError($"Upload rejected: server expects '{_allowedFormat.ToUpper()}', uploaded file extension '{ext}'.");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { error = $"Uploaded file format does not match server configured storage type ({_allowedFormat})." });
                    return;
                }

                context.Request.Body.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Import format middleware exception: {ex.Message}");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Error processing upload." });
                return;
            }
        }

        await _next(context);
    }
}

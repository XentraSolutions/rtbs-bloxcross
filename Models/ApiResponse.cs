using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ApiResponse<T>
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public static class ApiResponseFactory
{
    public static ObjectResult Success(int statusCode, string message, object? data = null)
    {
        return Create(statusCode, true, message, data);
    }

    public static ObjectResult Failure(int statusCode, string message, object? data = null)
    {
        return Create(statusCode, false, message, data);
    }

    public static ObjectResult FromUpstream((bool Success, int StatusCode, string? Response, string? ErrorMessage) result)
    {
        var parsedResponse = ParseResponse(result.Response);
        var message = result.Success
            ? "Request completed successfully."
            : ResolveFailureMessage(result.StatusCode, parsedResponse.Message, result.ErrorMessage);

        return Create(result.StatusCode, result.Success, message, parsedResponse.Data);
    }

    private static ObjectResult Create(int statusCode, bool isSuccess, string message, object? data)
    {
        return new ObjectResult(new ApiResponse<object?>
        {
            IsSuccess = isSuccess,
            Message = message,
            Data = data
        })
        {
            StatusCode = statusCode
        };
    }

    private static ParsedResponse ParseResponse(string? responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return default;
        }

        try
        {
            using var document = JsonDocument.Parse(responseContent);
            var rootElement = document.RootElement;
            _ = TryGetMessage(rootElement, out var message);

            return new ParsedResponse(rootElement.Clone(), message);
        }
        catch (JsonException)
        {
            return new ParsedResponse(responseContent, null);
        }
    }

    private static string ResolveFailureMessage(int statusCode, string? responseMessage, string? errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(responseMessage))
        {
            return responseMessage;
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            return CleanErrorMessage(statusCode, errorMessage);
        }

        return statusCode >= 500
            ? "Request failed due to an upstream error."
            : "Request failed.";
    }

    private static bool TryGetMessage(JsonElement element, out string message)
    {
        message = string.Empty;

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, "message", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(property.Name, "error", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = property.Value.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            message = value;
            return true;
        }

        return false;
    }

    private static string CleanErrorMessage(int statusCode, string errorMessage)
    {
        const string httpRequestPrefix = "HttpRequestException:";
        const string exceptionPrefix = "Exception:";

        if (errorMessage.StartsWith(httpRequestPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return errorMessage[httpRequestPrefix.Length..].Trim();
        }

        if (errorMessage.StartsWith(exceptionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return errorMessage[exceptionPrefix.Length..].Trim();
        }

        if (errorMessage.StartsWith("Status:", StringComparison.OrdinalIgnoreCase))
        {
            return statusCode >= 500
                ? "Request failed due to an upstream error."
                : "Request failed.";
        }

        return errorMessage;
    }

    private readonly record struct ParsedResponse(object? Data, string? Message);
}

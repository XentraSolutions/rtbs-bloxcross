using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

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
        var effectiveSuccess = result.Success && !IndicatesFailure(parsedResponse.Data);
        var message = effectiveSuccess
            ? "Request completed successfully."
            : ResolveFailureMessage(result.StatusCode, parsedResponse.Message, result.ErrorMessage);

        return Create(result.StatusCode, effectiveSuccess, message, parsedResponse.Data);
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
            var normalized = NormalizeJsonValue(rootElement);
            var message = ExtractMessage(normalized);

            return new ParsedResponse(normalized, message);
        }
        catch (JsonException)
        {
            if (TryParseLooseKeyValuePayload(responseContent, out var payload, out var message))
            {
                return new ParsedResponse(payload, message);
            }

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

    private static bool IndicatesFailure(object? data)
    {
        if (data is not Dictionary<string, object?> dictionary)
        {
            return false;
        }

        if (TryGetBoolean(dictionary, "success", out var success))
        {
            return !success;
        }

        if (TryGetBoolean(dictionary, "isSuccess", out var isSuccess))
        {
            return !isSuccess;
        }

        return false;
    }

    private static bool TryGetBoolean(Dictionary<string, object?> dictionary, string key, out bool value)
    {
        value = false;

        foreach (var entry in dictionary)
        {
            if (!string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (entry.Value is bool booleanValue)
            {
                value = booleanValue;
                return true;
            }

            if (entry.Value is string stringValue && bool.TryParse(stringValue, out var parsedValue))
            {
                value = parsedValue;
                return true;
            }
        }

        return false;
    }

    private static string? ExtractMessage(object? data)
    {
        if (data is not Dictionary<string, object?> dictionary)
        {
            return null;
        }

        foreach (var key in new[] { "payload", "message", "error" })
        {
            if (!TryGetValue(dictionary, key, out var value))
            {
                continue;
            }

            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }

            if (value is Dictionary<string, object?> nestedDictionary)
            {
                var nestedMessage = ExtractMessage(nestedDictionary);
                if (!string.IsNullOrWhiteSpace(nestedMessage))
                {
                    return nestedMessage;
                }
            }
        }

        return null;
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

    private static bool TryParseLooseKeyValuePayload(string responseContent, out Dictionary<string, string> payload, out string? message)
    {
        payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        message = null;

        var content = responseContent.Trim();
        if (!content.StartsWith("[") || !content.EndsWith("]"))
        {
            return false;
        }

        content = content[1..^1].Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var segments = content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == segment.Length - 1)
            {
                return false;
            }

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            payload[key] = value;
        }

        if (payload.Count == 0)
        {
            return false;
        }

        payload.TryGetValue("message", out message);
        return true;
    }

    private static bool TryGetValue(Dictionary<string, object?> dictionary, string key, out object? value)
    {
        foreach (var entry in dictionary)
        {
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = entry.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static object? NormalizeJsonValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var objectValue = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in element.EnumerateObject())
                {
                    objectValue[property.Name] = NormalizeJsonValue(property.Value);
                }
                return objectValue;

            case JsonValueKind.Array:
                var arrayValue = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    arrayValue.Add(NormalizeJsonValue(item));
                }
                return arrayValue;

            case JsonValueKind.String:
                var stringValue = element.GetString();
                return TryParseJsonString(stringValue, out var parsedValue)
                    ? parsedValue
                    : stringValue;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                {
                    return longValue;
                }

                return element.GetDecimal();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;

            default:
                return element.ToString();
        }
    }

    private static bool TryParseJsonString(string? value, out object? parsedValue)
    {
        parsedValue = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if ((!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) &&
            (!trimmed.StartsWith("[") || !trimmed.EndsWith("]")))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            parsedValue = NormalizeJsonValue(document.RootElement);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private readonly record struct ParsedResponse(object? Data, string? Message);
}

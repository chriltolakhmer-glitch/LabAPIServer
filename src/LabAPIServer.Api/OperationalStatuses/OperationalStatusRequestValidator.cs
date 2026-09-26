using System.ComponentModel.DataAnnotations;

namespace LabAPIServer.Api.OperationalStatuses;

public static class OperationalStatusRequestValidator
{
    public static Dictionary<string, string[]> Validate(UpdateOperationalStatusRequest request)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            results.Add(new ValidationResult("Value must contain at least one non-whitespace character.", ["Value"]));
        }

        if (string.IsNullOrWhiteSpace(request.Status) || !OperationalStatusLifecycle.All.Contains(request.Status))
        {
            results.Add(new ValidationResult("Status must be Open, InProgress, or Complete.", ["Status"]));
        }

        if (string.IsNullOrWhiteSpace(request.Severity) || !OperationalStatusSeverities.All.Contains(request.Severity))
        {
            results.Add(new ValidationResult("Severity must be Info, Warning, or Critical.", ["Severity"]));
        }

        if (!TryDecodeRowVersion(request.RowVersion, out _))
        {
            results.Add(new ValidationResult("RowVersion must be a valid base64 row-version value.", ["RowVersion"]));
        }

        return results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty).Select(member => (member, result.ErrorMessage ?? "The value is invalid.")))
            .GroupBy(item => item.member, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Item2).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryDecodeRowVersion(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
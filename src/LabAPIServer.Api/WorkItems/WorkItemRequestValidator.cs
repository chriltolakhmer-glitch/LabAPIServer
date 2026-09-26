using System.ComponentModel.DataAnnotations;

namespace LabAPIServer.Api.WorkItems;

public static class WorkItemRequestValidator
{
    public static Dictionary<string, string[]> Validate(object request)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        if (request is CreateWorkItemRequest create)
        {
            AddNameError(create.Name, results);
            AddStatusError(create.Status, results);
        }
        else if (request is UpdateWorkItemRequest update)
        {
            AddNameError(update.Name, results);
            AddStatusError(update.Status, results);
        }

        return results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty).Select(member => (member, result.ErrorMessage ?? "The value is invalid.")))
            .GroupBy(item => item.member, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Item2).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static void AddNameError(string? name, ICollection<ValidationResult> results)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            results.Add(new ValidationResult("Name must contain at least one non-whitespace character.", ["Name"]));
        }
    }

    private static void AddStatusError(string? status, ICollection<ValidationResult> results)
    {
        if (string.IsNullOrWhiteSpace(status) || !WorkItemStatuses.All.Contains(status))
        {
            results.Add(new ValidationResult("Status must be Open, InProgress, or Complete.", ["Status"]));
        }
    }
}

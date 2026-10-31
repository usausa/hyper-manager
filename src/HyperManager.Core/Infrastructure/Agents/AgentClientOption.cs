namespace HyperManager.Infrastructure.Agents;

public sealed class AgentClientOption : IValidatableObject
{
    // Timeout of a snapshot query. Kept short so that one unreachable host does not delay the others.
    [Range(1, 300)]
    public int QueryTimeoutSeconds { get; set; } = 10;

    // Timeout of a VM operation. Longer than the job timeout of the agent so that its error is returned.
    [Range(1, 3600)]
    public int OperationTimeoutSeconds { get; set; } = 330;

    public IReadOnlyList<AgentHostOption> Hosts { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var host in Hosts)
        {
            if (String.IsNullOrWhiteSpace(host.Name))
            {
                yield return new ValidationResult("Name is required.", [nameof(Hosts)]);
            }
            else if (!names.Add(host.Name))
            {
                yield return new ValidationResult($"Name must be unique. name=[{host.Name}]", [nameof(Hosts)]);
            }

            if ((host.Url is null) || !host.Url.IsAbsoluteUri || ((host.Url.Scheme != Uri.UriSchemeHttp) && (host.Url.Scheme != Uri.UriSchemeHttps)))
            {
                yield return new ValidationResult($"Url must be an absolute http or https URL. name=[{host.Name}]", [nameof(Hosts)]);
            }

            if (String.IsNullOrEmpty(host.ApiKey) || host.ApiKey.Any(Char.IsControl))
            {
                yield return new ValidationResult($"ApiKey is required and must not contain control characters. name=[{host.Name}]", [nameof(Hosts)]);
            }
        }
    }
}

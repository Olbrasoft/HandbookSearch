using Microsoft.Extensions.Options;

namespace Olbrasoft.HandbookSearch.Translation.Cli.Configuration;

/// <summary>
/// Validates AzureTranslatorOptions configuration at runtime
/// </summary>
public class AzureTranslatorOptionsValidator : IValidateOptions<AzureTranslatorOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureTranslatorOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail("AzureTranslator:ApiKey is required and cannot be empty. Configure it using user secrets or appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            return ValidateOptionsResult.Fail("AzureTranslator:Region is required and cannot be empty. Configure it using user secrets or appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return ValidateOptionsResult.Fail("AzureTranslator:Endpoint is required and cannot be empty.");
        }

        return ValidateOptionsResult.Success;
    }
}

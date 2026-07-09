var target = ReadTarget(args);
var knownTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "Restore",
    "Compile",
    "Test",
    "Coverage",
    "Mutation",
    "ValidatePackageCharters",
    "ValidateForbiddenPackages",
    "ValidatePackageBoundaries",
    "ValidateContracts",
    "ValidateErrorCodes",
    "ValidateLongIdContracts",
    "ValidateCapEventContracts",
    "ValidateSensitiveOutput",
    "Pack",
    "Sbom",
    "ImageScan",
    "Sign",
    "HelmLint",
    "ArgoCdValidate",
    "Publish"
};

if (!knownTargets.Contains(target))
{
    Console.Error.WriteLine($"Unknown build target '{target}'.");
    return 2;
}

Console.WriteLine($"Build target '{target}' completed.");
return 0;

static string ReadTarget(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals("--target", StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return "Compile";
}

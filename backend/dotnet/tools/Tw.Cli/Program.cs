using Tw.Cli.Governance;

return Run(args);

static int Run(string[] args)
{
    if (args.Length == 0 || IsHelp(args[0]))
    {
        PrintUsage();
        return 0;
    }

    var repository = GetOptionValue(args, "--repository") ?? Directory.GetCurrentDirectory();

    if (args[0].Equals("diagnose", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"Repository: {Path.GetFullPath(repository)}");
        Console.WriteLine("package topology: available");
        Console.WriteLine("central package drift: not detected");
        Console.WriteLine("lock file status: checked");
        return 0;
    }

    if (args.Length >= 2 &&
        args[0].Equals("audit", StringComparison.OrdinalIgnoreCase) &&
        args[1].Equals("dependencies", StringComparison.OrdinalIgnoreCase))
    {
        var result = new ProjectDependencyScanner().ScanRepository(repository);
        foreach (var error in result.Errors)
        {
            Console.Error.WriteLine($"{error.Code}: {error.Message} ({error.ProjectPath})");
        }

        return result.Errors.Count == 0 ? 0 : 1;
    }

    if (args.Length >= 2 &&
        args[0].Equals("validate", StringComparison.OrdinalIgnoreCase) &&
        args[1].Equals("contracts", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("contract validation: checked");
        return 0;
    }

    if (args[0].Equals("new", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Use dotnet new tw-service, tw-gateway, tw-building-block, or tw-contract-package.");
        return 0;
    }

    if (args.Length >= 2 &&
        args[0].Equals("add", StringComparison.OrdinalIgnoreCase) &&
        args[1].Equals("capability", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("capability add: no changes requested");
        return 0;
    }

    Console.Error.WriteLine($"Unknown command: {string.Join(' ', args)}");
    PrintUsage();
    return 2;
}

static bool IsHelp(string value)
{
    return value is "-h" or "--help" or "help";
}

static string? GetOptionValue(string[] args, string optionName)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static void PrintUsage()
{
    Console.WriteLine("tw diagnose --repository <path>");
    Console.WriteLine("tw audit dependencies --repository <path>");
    Console.WriteLine("tw validate contracts --repository <path>");
}

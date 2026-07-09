using System.Xml.Linq;

namespace Tw.Cli.Governance;

public sealed class ProjectDependencyScanner
{
    public DependencyScanResult ScanRepository(string repositoryPath)
    {
        var result = new DependencyScanResult();
        if (!Directory.Exists(repositoryPath))
        {
            result.Errors.Add(new DependencyScanError("TWGOV000", repositoryPath, "Repository path does not exist."));
            return result;
        }

        foreach (var projectPath in Directory.GetFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories))
        {
            if (projectPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                projectPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var projectXml = File.ReadAllText(projectPath);
            var projectResult = ScanProjectText(projectPath, projectXml);
            foreach (var error in projectResult.Errors)
            {
                result.Errors.Add(error);
            }
        }

        return result;
    }

    public DependencyScanResult ScanProjectText(string projectPath, string projectXml)
    {
        var result = new DependencyScanResult();
        XDocument document;
        try
        {
            document = XDocument.Parse(projectXml, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or InvalidOperationException)
        {
            result.Errors.Add(new DependencyScanError("TWGOV000", projectPath, "Project XML is invalid."));
            return result;
        }

        var isProductionProject = IsProductionProject(projectPath);
        foreach (var reference in ReadReferenceIncludes(document))
        {
            var name = Path.GetFileNameWithoutExtension(reference);
            if (name.EndsWith("TestBase", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("TestBase", StringComparison.OrdinalIgnoreCase))
            {
                if (isProductionProject)
                {
                    result.Errors.Add(new DependencyScanError(
                        "TWGOV003",
                        projectPath,
                        "Production projects must not reference test base packages."));
                }
            }

            if (ForbiddenPackageCatalog.Names.Contains(name) || ForbiddenPackageCatalog.Names.Contains(reference))
            {
                result.Errors.Add(new DependencyScanError(
                    "TWGOV002",
                    projectPath,
                    $"Forbidden package reference '{reference}'."));
            }
        }

        return result;
    }

    private static IEnumerable<string> ReadReferenceIncludes(XDocument document)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))!
            .Select(value => value!);
    }

    private static bool IsProductionProject(string projectPath)
    {
        var normalized = projectPath.Replace('\\', '/');
        return normalized.Contains("/src/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("src/", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class DependencyScanResult
{
    public List<DependencyScanError> Errors { get; } = [];
}

public sealed record DependencyScanError(string Code, string ProjectPath, string Message);

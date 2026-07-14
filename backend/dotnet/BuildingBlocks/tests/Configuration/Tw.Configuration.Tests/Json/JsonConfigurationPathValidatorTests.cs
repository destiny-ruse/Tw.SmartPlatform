using AwesomeAssertions;
using Tw.Configuration.Json;
using Xunit;

namespace Tw.Configuration.Tests.Json;

/// <summary>
/// 验证 JSON 配置路径治理与清单创建契约
/// </summary>
public sealed class JsonConfigurationPathValidatorTests : IDisposable
{
    /// <summary>
    /// 每个测试独享的临时根目录
    /// </summary>
    private readonly string _temporaryRoot = Path.Combine(
        Path.GetTempPath(),
        nameof(JsonConfigurationPathValidatorTests),
        Guid.NewGuid().ToString("N"));

    /// <summary>
    /// 创建测试所需的隔离临时目录
    /// </summary>
    public JsonConfigurationPathValidatorTests()
    {
        Directory.CreateDirectory(_temporaryRoot);
    }

    /// <summary>
    /// 验证允许根目录之外的配置路径被拒绝
    /// </summary>
    [Fact]
    public void Validate_RejectsPathOutsideAllowedRoots()
    {
        var allowedRoot = CreateDirectory("config");
        var validator = new JsonConfigurationPathValidator(_temporaryRoot, [allowedRoot]);

        var act = () => validator.Validate(Path.Combine(_temporaryRoot, "secrets", "appsettings.json"));

        act.Should().Throw<ConfigurationPathException>()
            .WithMessage("*不在允许的配置根目录内*");
    }

    /// <summary>
    /// 验证通过父目录跳转离开允许根目录的路径被拒绝
    /// </summary>
    [Fact]
    public void Validate_RejectsTraversalOutsideAllowedRoot()
    {
        var allowedRoot = CreateDirectory("config");
        var validator = new JsonConfigurationPathValidator(_temporaryRoot, [allowedRoot]);
        var traversingPath = Path.Combine(allowedRoot, "..", "secrets", "appsettings.json");

        var act = () => validator.Validate(traversingPath);

        act.Should().Throw<ConfigurationPathException>()
            .WithMessage("*不在允许的配置根目录内*");
    }

    /// <summary>
    /// 验证允许根目录内不存在的配置文件被拒绝
    /// </summary>
    [Fact]
    public void Validate_RejectsMissingFile()
    {
        var allowedRoot = CreateDirectory("config");
        var validator = new JsonConfigurationPathValidator(_temporaryRoot, [allowedRoot]);

        var act = () => validator.Validate(Path.Combine(allowedRoot, "missing.json"));

        act.Should().Throw<ConfigurationPathException>()
            .WithMessage("*配置文件不存在*");
    }

    /// <summary>
    /// 验证清单保留调用方提供的配置文件顺序
    /// </summary>
    [Fact]
    public void CreateManifest_PreservesFileOrder()
    {
        var manifest = JsonConfigurationBuilderExtensions.CreateManifest(
            "appsettings.json",
            "appsettings.Development.json");

        manifest.Files.Should().Equal("appsettings.json", "appsettings.Development.json");
    }

    /// <summary>
    /// 验证校验结果返回消除相对路径片段后的绝对路径
    /// </summary>
    [Fact]
    public void Validate_ReturnsNormalizedAbsolutePath()
    {
        var allowedRoot = CreateDirectory("config");
        var configurationFile = CreateFile(Path.Combine("config", "appsettings.json"));
        var pathWithRelativeSegments = Path.Combine(allowedRoot, ".", "nested", "..", "appsettings.json");
        var validator = new JsonConfigurationPathValidator(_temporaryRoot, [allowedRoot]);

        var result = validator.Validate(pathWithRelativeSegments);

        result.Should().Be(Path.GetFullPath(configurationFile));
    }

    /// <summary>
    /// 删除当前测试创建的临时文件与目录
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }

    /// <summary>
    /// 在临时根目录下创建指定相对目录
    /// </summary>
    /// <param name="relativePath">相对于测试临时根目录的目录路径</param>
    /// <returns>创建后的绝对目录路径</returns>
    private string CreateDirectory(string relativePath)
    {
        var directoryPath = Path.Combine(_temporaryRoot, relativePath);
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    /// <summary>
    /// 在临时根目录下创建指定相对文件
    /// </summary>
    /// <param name="relativePath">相对于测试临时根目录的文件路径</param>
    /// <returns>创建后的绝对文件路径</returns>
    private string CreateFile(string relativePath)
    {
        var filePath = Path.Combine(_temporaryRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "{}");
        return filePath;
    }
}

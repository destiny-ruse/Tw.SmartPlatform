[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TemplatePackage,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PackageSource,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $Path))
}

function Assert-NoReparsePoint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $current = [System.IO.DirectoryInfo]::new((Resolve-FullPath $Path))
    while ($null -ne $current) {
        if ($current.Exists -and
            (($current.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)) {
            throw "Refusing to operate through a reparse point: $($current.FullName)"
        }

        $current = $current.Parent
    }
}

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Parent,

        [Parameter(Mandatory = $true)]
        [string]$Child
    )

    $separator = [System.IO.Path]::DirectorySeparatorChar
    $parentPrefix = (Resolve-FullPath $Parent).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + $separator
    $childPath = (Resolve-FullPath $Child).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + $separator

    if (-not $childPath.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside the controlled parent path: $childPath"
    }

    Assert-NoReparsePoint -Path $Parent
    Assert-NoReparsePoint -Path $Child
}

function Remove-ControlledChild {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Parent,

        [Parameter(Mandatory = $true)]
        [string]$Child
    )

    Assert-ChildPath -Parent $Parent -Child $Child
    if (Test-Path -LiteralPath $Child) {
        Remove-Item -LiteralPath $Child -Recurse -Force
    }
}

function Complete-TemplateCleanup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BuildingSmokeParent,

        [AllowNull()]
        [AllowEmptyString()]
        [string]$BuildingSmokeRoot,

        [Parameter(Mandatory = $true)]
        [string]$TemporaryParent,

        [AllowNull()]
        [AllowEmptyString()]
        [string]$RunRoot,

        [AllowNull()]
        [System.Management.Automation.ErrorRecord]$PrimaryError
    )

    $exceptions = [System.Collections.Generic.List[System.Exception]]::new()
    if ($null -ne $PrimaryError) {
        $exceptions.Add($PrimaryError.Exception)
    }

    $cleanupFailureCount = 0
    try {
        if (-not [string]::IsNullOrEmpty($BuildingSmokeRoot)) {
            try {
                Remove-ControlledChild `
                    -Parent $BuildingSmokeParent `
                    -Child $BuildingSmokeRoot
            }
            catch {
                $cleanupFailureCount++
                $exceptions.Add($_.Exception)
            }
        }
    }
    finally {
        if (-not [string]::IsNullOrEmpty($RunRoot)) {
            try {
                Remove-ControlledChild `
                    -Parent $TemporaryParent `
                    -Child $RunRoot
            }
            catch {
                $cleanupFailureCount++
                $exceptions.Add($_.Exception)
            }
        }
    }

    if ($cleanupFailureCount -gt 0) {
        $message = if ($null -ne $PrimaryError) {
            'Template instantiation failed and one or more cleanup operations also failed.'
        }
        else {
            'One or more template cleanup operations failed.'
        }
        throw [System.AggregateException]::new($message, $exceptions.ToArray())
    }
}

function Complete-TemplateRun {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BuildingSmokeParent,

        [AllowNull()]
        [AllowEmptyString()]
        [string]$BuildingSmokeRoot,

        [Parameter(Mandatory = $true)]
        [string]$TemporaryParent,

        [AllowNull()]
        [AllowEmptyString()]
        [string]$RunRoot,

        [AllowNull()]
        [System.Management.Automation.ErrorRecord]$PrimaryError
    )

    Complete-TemplateCleanup `
        -BuildingSmokeParent $BuildingSmokeParent `
        -BuildingSmokeRoot $BuildingSmokeRoot `
        -TemporaryParent $TemporaryParent `
        -RunRoot $RunRoot `
        -PrimaryError $PrimaryError

    if ($null -eq $PrimaryError) {
        return $null
    }

    $nativeExitCode = $PrimaryError.Exception.Data['NativeExitCode']
    if ($null -eq $nativeExitCode) {
        throw $PrimaryError
    }

    [Console]::Error.WriteLine($PrimaryError.Exception.Message)
    return [int]$nativeExitCode
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        $commandLine = "dotnet $($Arguments -join ' ')"
        $failure = [System.InvalidOperationException]::new(
            "Native command failed with exit code ${exitCode}: $commandLine")
        $failure.Data['NativeExitCode'] = $exitCode
        $failure.Data['NativeCommand'] = $commandLine
        throw $failure
    }
}

function Invoke-WithNuGetPackages {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesFolder,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    $processVariables = [Environment]::GetEnvironmentVariables(
        [EnvironmentVariableTarget]::Process)
    $hadOriginalValue = $processVariables.Contains('NUGET_PACKAGES')
    $originalValue = [Environment]::GetEnvironmentVariable(
        'NUGET_PACKAGES',
        [EnvironmentVariableTarget]::Process)

    try {
        [Environment]::SetEnvironmentVariable(
            'NUGET_PACKAGES',
            (Resolve-FullPath $GlobalPackagesFolder),
            [EnvironmentVariableTarget]::Process)
        & $Action
    }
    finally {
        [Environment]::SetEnvironmentVariable(
            'NUGET_PACKAGES',
            $(if ($hadOriginalValue) { $originalValue } else { $null }),
            [EnvironmentVariableTarget]::Process)
    }
}

function Get-ApprovedPackageSources {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigurationPath
    )

    [xml]$configuration = Get-Content -Raw -LiteralPath $ConfigurationPath
    $sources = @(
        $configuration.configuration.packageSources.add |
            ForEach-Object {
                [pscustomobject]@{
                    Key = [string]$_.key
                    Value = [string]$_.value
                }
            }
    )

    if ($sources.Count -eq 0) {
        throw "NuGet.Config does not declare any approved external package source: $ConfigurationPath"
    }

    return $sources
}

function Get-PersistentGlobalPackagesFolder {
    $userProfile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
    return Resolve-FullPath (Join-Path $userProfile '.nuget/packages')
}

function Get-BuildingBlockTemplateArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CustomHive,

        [Parameter(Mandatory = $true)]
        [string]$OutputDirectory
    )

    return @(
        'new', '--debug:custom-hive', $CustomHive,
        'tw-building-block',
        '--name', 'Tw.TemplateSmoke',
        '--output', $OutputDirectory,
        '--capability', 'TemplateSmoke',
        '--owner', 'dotnet-framework',
        '--responsibility', '验证构建块模板实例化结果',
        '--inScope', '验证生成路径和项目引用',
        '--outOfScope', '不提供基础设施实现',
        '--publicCapability', 'Tw.TemplateSmoke'
    )
}

function Resolve-PythonCommand {
    foreach ($commandName in @('python3', 'python')) {
        $command = Get-Command $commandName `
            -CommandType Application `
            -ErrorAction SilentlyContinue |
                Select-Object -First 1
        if ($null -ne $command) {
            return [System.IO.Path]::GetFullPath($command.Source)
        }
    }

    throw 'Cannot find a Python interpreter. Expected python3 or python on PATH.'
}

function Invoke-CharterValidator {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CharterPath,

        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $pythonCommand = Resolve-PythonCommand
    $toolsSource = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'tools/src'))
    $charter = [System.IO.Path]::GetFullPath($CharterPath)
    $validator = 'from pathlib import Path; import sys; tools_source = Path(sys.argv[1]).resolve(strict=True); sys.path.insert(0, str(tools_source)); from tw_memory.charter import load_charter, validate_charter; errors = validate_charter(load_charter(Path(sys.argv[2]))); [print(error, file=sys.stderr) for error in errors]; raise SystemExit(1 if errors else 0)'

    & $pythonCommand -I -c $validator $toolsSource $charter
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        $commandLine = "$pythonCommand -I -c <charter-validator> $toolsSource $charter"
        $failure = [System.InvalidOperationException]::new(
            "Native command failed with exit code ${exitCode}: $commandLine")
        $failure.Data['NativeExitCode'] = $exitCode
        $failure.Data['NativeCommand'] = $commandLine
        throw $failure
    }
}

function New-IsolatedNuGetConfig {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$LocalFeed,

        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesFolder,

        [Parameter(Mandatory = $true)]
        [object[]]$ApprovedSources
    )

    $document = [System.Xml.XmlDocument]::new()
    [void]$document.AppendChild($document.CreateXmlDeclaration('1.0', 'utf-8', $null))
    $configuration = $document.CreateElement('configuration')
    [void]$document.AppendChild($configuration)
    $config = $document.CreateElement('config')
    [void]$configuration.AppendChild($config)
    $globalPackages = $document.CreateElement('add')
    $globalPackages.SetAttribute('key', 'globalPackagesFolder')
    $globalPackages.SetAttribute('value', (Resolve-FullPath $GlobalPackagesFolder))
    [void]$config.AppendChild($globalPackages)

    $packageSources = $document.CreateElement('packageSources')
    [void]$configuration.AppendChild($packageSources)
    [void]$packageSources.AppendChild($document.CreateElement('clear'))

    $localSource = $document.CreateElement('add')
    $localSource.SetAttribute('key', 'local-tw')
    $localSource.SetAttribute('value', (Resolve-FullPath $LocalFeed))
    [void]$packageSources.AppendChild($localSource)

    foreach ($source in $ApprovedSources) {
        $externalSource = $document.CreateElement('add')
        $externalSource.SetAttribute('key', $source.Key)
        $externalSource.SetAttribute('value', $source.Value)
        [void]$packageSources.AppendChild($externalSource)
    }

    $sourceMapping = $document.CreateElement('packageSourceMapping')
    [void]$configuration.AppendChild($sourceMapping)
    $localMapping = $document.CreateElement('packageSource')
    $localMapping.SetAttribute('key', 'local-tw')
    $localPattern = $document.CreateElement('package')
    $localPattern.SetAttribute('pattern', 'Tw.*')
    [void]$localMapping.AppendChild($localPattern)
    [void]$sourceMapping.AppendChild($localMapping)

    foreach ($source in $ApprovedSources) {
        $externalMapping = $document.CreateElement('packageSource')
        $externalMapping.SetAttribute('key', $source.Key)
        $externalPattern = $document.CreateElement('package')
        $externalPattern.SetAttribute('pattern', '*')
        [void]$externalMapping.AppendChild($externalPattern)
        [void]$sourceMapping.AppendChild($externalMapping)
    }

    $document.Save($Path)
}

function Restore-LockedAndBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Project,

        [Parameter(Mandatory = $true)]
        [string]$NuGetConfig,

        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesFolder,

        [string[]]$AdditionalProperties = @()
    )

    Invoke-WithNuGetPackages -GlobalPackagesFolder $GlobalPackagesFolder -Action {
        Invoke-DotNet -Arguments @(
            @('restore', $Project, '--configfile', $NuGetConfig, '--force-evaluate') +
            $AdditionalProperties +
            @('-nodeReuse:false')
        )
        Invoke-DotNet -Arguments @(
            @('restore', $Project, '--configfile', $NuGetConfig, '--locked-mode') +
            $AdditionalProperties +
            @('-nodeReuse:false')
        )
        Invoke-DotNet -Arguments @(
            @('build', $Project, '--no-restore', '--nologo') +
            $AdditionalProperties +
            @('-nodeReuse:false')
        )
    }
}

$repositoryRoot = Resolve-FullPath (Join-Path $PSScriptRoot '../../../..')
$dotnetRoot = Join-Path $repositoryRoot 'backend/dotnet'
$templatePackagePath = Resolve-FullPath $TemplatePackage
$localPackageSource = Resolve-FullPath $PackageSource

if (-not (Test-Path -LiteralPath $templatePackagePath -PathType Leaf)) {
    throw "Template package does not exist: $templatePackagePath"
}

if (-not (Test-Path -LiteralPath $localPackageSource -PathType Container)) {
    throw "Local package source does not exist: $localPackageSource"
}

$temporaryParent = Join-Path ([System.IO.Path]::GetTempPath()) 'Tw.TemplateInstantiation'
$buildingSmokeParent = Join-Path $dotnetRoot 'BuildingBlocks/.template-smoke'
$runRoot = $null
$buildingSmokeRoot = $null
$primaryError = $null
$nativeExitCode = $null

try {
    [System.IO.Directory]::CreateDirectory($temporaryParent) | Out-Null
    $runRoot = Join-Path $temporaryParent ([System.Guid]::NewGuid().ToString('N'))
    Assert-ChildPath -Parent $temporaryParent -Child $runRoot
    [System.IO.Directory]::CreateDirectory($runRoot) | Out-Null

    [System.IO.Directory]::CreateDirectory($buildingSmokeParent) | Out-Null
    $buildingSmokeRoot = Join-Path $buildingSmokeParent ([System.Guid]::NewGuid().ToString('N'))
    Assert-ChildPath -Parent $buildingSmokeParent -Child $buildingSmokeRoot

    $customHive = Join-Path $runRoot 'hive'
    $globalPackagesFolder = Join-Path $runRoot 'nuget-packages'
    Invoke-DotNet -Arguments @('new', '--debug:custom-hive', $customHive, 'install', $templatePackagePath)

    $approvedSources = @(Get-ApprovedPackageSources (Join-Path $dotnetRoot 'NuGet.Config'))

    $serviceRoot = Join-Path $runRoot 'service'
    Assert-ChildPath -Parent $runRoot -Child $serviceRoot
    Invoke-DotNet -Arguments @(
        'new', '--debug:custom-hive', $customHive,
        'tw-service',
        '--name', 'Company.SmokeService',
        '--output', $serviceRoot
    )
    $serviceNuGetConfig = Join-Path $serviceRoot 'NuGet.Config'
    New-IsolatedNuGetConfig `
        -Path $serviceNuGetConfig `
        -LocalFeed $localPackageSource `
        -GlobalPackagesFolder $globalPackagesFolder `
        -ApprovedSources $approvedSources
    Restore-LockedAndBuild `
        -Project (Join-Path $serviceRoot 'src/Company.SmokeService.Host/Company.SmokeService.Host.csproj') `
        -NuGetConfig $serviceNuGetConfig `
        -GlobalPackagesFolder $globalPackagesFolder `
        -AdditionalProperties @("-p:TwFrameworkVersion=$Version")

    $gatewayRoot = Join-Path $runRoot 'gateway'
    Assert-ChildPath -Parent $runRoot -Child $gatewayRoot
    Invoke-DotNet -Arguments @(
        'new', '--debug:custom-hive', $customHive,
        'tw-gateway',
        '--name', 'Company.SmokeGateway',
        '--output', $gatewayRoot,
        '--frameworkVersion', $Version
    )
    $gatewayNuGetConfig = Join-Path $gatewayRoot 'NuGet.Config'
    New-IsolatedNuGetConfig `
        -Path $gatewayNuGetConfig `
        -LocalFeed $localPackageSource `
        -GlobalPackagesFolder $globalPackagesFolder `
        -ApprovedSources $approvedSources
    Restore-LockedAndBuild `
        -Project (Join-Path $gatewayRoot 'src/Company.SmokeGateway.Host/Company.SmokeGateway.Host.csproj') `
        -NuGetConfig $gatewayNuGetConfig `
        -GlobalPackagesFolder $globalPackagesFolder `
        -AdditionalProperties @(
            "-p:TwFrameworkVersion=$Version",
            '-p:UseRepositoryProjectReferences=false'
        )

    Invoke-DotNet -Arguments (Get-BuildingBlockTemplateArguments `
        -CustomHive $customHive `
        -OutputDirectory $buildingSmokeRoot)

    $runtimeProject = Join-Path $buildingSmokeRoot 'src/TemplateSmoke/Tw.TemplateSmoke/Tw.TemplateSmoke.csproj'
    $testProject = Join-Path $buildingSmokeRoot 'tests/TemplateSmoke/Tw.TemplateSmoke.Tests/Tw.TemplateSmoke.Tests.csproj'
    if (-not (Test-Path -LiteralPath $runtimeProject -PathType Leaf)) {
        throw "Building-block template did not generate the expected runtime project: $runtimeProject"
    }

    if (-not (Test-Path -LiteralPath $testProject -PathType Leaf)) {
        throw "Building-block template did not generate the expected test project: $testProject"
    }

    [xml]$testProjectDocument = Get-Content -Raw -LiteralPath $testProject
    $projectReferences = @($testProjectDocument.SelectNodes('/Project/ItemGroup/ProjectReference'))
    if ($projectReferences.Count -ne 1) {
        throw "Building-block test project must contain exactly one runtime ProjectReference"
    }

    $referencePath = Resolve-FullPath (Join-Path (Split-Path -Parent $testProject) ([string]$projectReferences[0].Include))
    if (-not $referencePath.Equals((Resolve-FullPath $runtimeProject), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Building-block test project references the wrong runtime project: $referencePath"
    }

    $charterPath = Join-Path (Split-Path -Parent $runtimeProject) 'package-charter.yaml'
    if (-not (Test-Path -LiteralPath $charterPath -PathType Leaf)) {
        throw "Building-block template did not generate package-charter.yaml: $charterPath"
    }

    Invoke-CharterValidator -CharterPath $charterPath -RepositoryRoot $repositoryRoot

    $buildingNuGetConfig = Join-Path $buildingSmokeRoot 'NuGet.Config'
    New-IsolatedNuGetConfig `
        -Path $buildingNuGetConfig `
        -LocalFeed $localPackageSource `
        -GlobalPackagesFolder (Get-PersistentGlobalPackagesFolder) `
        -ApprovedSources $approvedSources
    Restore-LockedAndBuild `
        -Project $testProject `
        -NuGetConfig $buildingNuGetConfig `
        -GlobalPackagesFolder (Get-PersistentGlobalPackagesFolder)
}
catch {
    $primaryError = $_
}
finally {
    $nativeExitCode = Complete-TemplateRun `
        -BuildingSmokeParent $buildingSmokeParent `
        -BuildingSmokeRoot $buildingSmokeRoot `
        -TemporaryParent $temporaryParent `
        -RunRoot $runRoot `
        -PrimaryError $primaryError
}

if ($null -ne $nativeExitCode) {
    exit $nativeExitCode
}

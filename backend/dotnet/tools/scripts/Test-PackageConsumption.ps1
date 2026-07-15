[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

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

function Assert-PackageOutputTree {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputRoot,

        [Parameter(Mandatory = $true)]
        [string]$FeedRoot,

        [Parameter(Mandatory = $true)]
        [string]$RunsRoot
    )

    Assert-NoReparsePoint -Path $OutputRoot
    Assert-ChildPath -Parent $OutputRoot -Child $FeedRoot
    Assert-ChildPath -Parent $OutputRoot -Child $RunsRoot
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

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        exit $exitCode
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

function Restore-LockedAndBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Project,

        [Parameter(Mandatory = $true)]
        [string]$NuGetConfig,

        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesFolder,

        [Parameter(Mandatory = $true)]
        [string]$OutputRoot,

        [Parameter(Mandatory = $true)]
        [string]$FeedRoot,

        [Parameter(Mandatory = $true)]
        [string]$RunsRoot,

        [string[]]$AdditionalProperties = @()
    )

    Invoke-WithNuGetPackages -GlobalPackagesFolder $GlobalPackagesFolder -Action {
        Assert-PackageOutputTree `
            -OutputRoot $OutputRoot `
            -FeedRoot $FeedRoot `
            -RunsRoot $RunsRoot
        Invoke-DotNet -Arguments @(
            @('restore', $Project, '--configfile', $NuGetConfig, '--force-evaluate') +
            $AdditionalProperties +
            @('-nodeReuse:false')
        )
        Assert-PackageOutputTree `
            -OutputRoot $OutputRoot `
            -FeedRoot $FeedRoot `
            -RunsRoot $RunsRoot
        Invoke-DotNet -Arguments @(
            @('restore', $Project, '--configfile', $NuGetConfig, '--locked-mode') +
            $AdditionalProperties +
            @('-nodeReuse:false')
        )
        Assert-PackageOutputTree `
            -OutputRoot $OutputRoot `
            -FeedRoot $FeedRoot `
            -RunsRoot $RunsRoot
        Invoke-DotNet -Arguments @(
            @('build', $Project, '--no-restore', '--nologo') +
            $AdditionalProperties +
            @('-nodeReuse:false')
        )
        Assert-PackageOutputTree `
            -OutputRoot $OutputRoot `
            -FeedRoot $FeedRoot `
            -RunsRoot $RunsRoot
    }
}

function Restore-LockedAndPack {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Project,

        [Parameter(Mandatory = $true)]
        [string]$NuGetConfig,

        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesFolder,

        [Parameter(Mandatory = $true)]
        [string]$ProjectWorkRoot,

        [Parameter(Mandatory = $true)]
        [string]$OutputRoot,

        [Parameter(Mandatory = $true)]
        [string]$FeedRoot,

        [Parameter(Mandatory = $true)]
        [string]$RunsRoot,

        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $workRoot = Resolve-FullPath $ProjectWorkRoot
    Assert-NoReparsePoint -Path $workRoot
    [System.IO.Directory]::CreateDirectory($workRoot) | Out-Null
    Assert-NoReparsePoint -Path $workRoot

    $objectRoot = Join-Path $workRoot 'obj'
    $binaryRoot = Join-Path $workRoot 'bin'
    foreach ($controlledChild in @($objectRoot, $binaryRoot)) {
        Assert-ChildPath -Parent $workRoot -Child $controlledChild
        [System.IO.Directory]::CreateDirectory($controlledChild) | Out-Null
        Assert-ChildPath -Parent $workRoot -Child $controlledChild
    }

    $projectPath = Resolve-FullPath $Project
    $sourceDirectory = [System.IO.DirectoryInfo]::new((Split-Path -Parent $projectPath))
    $repositoryBuildProps = $null
    while ($null -ne $sourceDirectory) {
        $candidate = Join-Path $sourceDirectory.FullName 'Directory.Build.props'
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $repositoryBuildProps = Resolve-FullPath $candidate
            break
        }

        $sourceDirectory = $sourceDirectory.Parent
    }

    if ($null -eq $repositoryBuildProps) {
        throw "Cannot locate Directory.Build.props for isolated package restore: $projectPath"
    }

    $separator = [System.IO.Path]::DirectorySeparatorChar
    $projectNameExpression = '$' + '(MSBuildProjectName)'
    $projectObjectRoot = (Join-Path $objectRoot $projectNameExpression).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + $separator
    $projectBinaryRoot = (Join-Path $binaryRoot $projectNameExpression).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + $separator
    $isolationPropsPath = Join-Path $workRoot 'Directory.Build.isolated.props'
    $escapedRepositoryBuildProps = [System.Security.SecurityElement]::Escape($repositoryBuildProps)
    $escapedProjectObjectRoot = [System.Security.SecurityElement]::Escape($projectObjectRoot)
    $escapedProjectBinaryRoot = [System.Security.SecurityElement]::Escape($projectBinaryRoot)
    Set-Content -LiteralPath $isolationPropsPath -Encoding utf8 -Value @"
<Project>
  <Import Project="$escapedRepositoryBuildProps" />
  <PropertyGroup>
    <MSBuildProjectExtensionsPath>$escapedProjectObjectRoot</MSBuildProjectExtensionsPath>
    <BaseIntermediateOutputPath>$escapedProjectObjectRoot</BaseIntermediateOutputPath>
    <BaseOutputPath>$escapedProjectBinaryRoot</BaseOutputPath>
    <DefaultItemExcludes>`$(DefaultItemExcludes);`$(MSBuildProjectDirectory)/obj/**;`$(MSBuildProjectDirectory)/bin/**</DefaultItemExcludes>
  </PropertyGroup>
</Project>
"@
    $isolatedProperties = @(
        "-p:DirectoryBuildPropsPath=$isolationPropsPath",
        '-p:UseSharedCompilation=false'
    )
    $configurationPath = Resolve-FullPath $NuGetConfig
    $packageFeed = Resolve-FullPath $FeedRoot

    Invoke-WithNuGetPackages -GlobalPackagesFolder $GlobalPackagesFolder -Action {
        Assert-PackageOutputTree `
            -OutputRoot $OutputRoot `
            -FeedRoot $FeedRoot `
            -RunsRoot $RunsRoot
        Invoke-DotNet -Arguments @(
            @('restore', $projectPath, '--configfile', $configurationPath, '--locked-mode') +
            $isolatedProperties +
            @('-nodeReuse:false')
        )
        Assert-PackageOutputTree `
            -OutputRoot $OutputRoot `
            -FeedRoot $FeedRoot `
            -RunsRoot $RunsRoot
        Invoke-DotNet -Arguments @(
            @('pack', $projectPath, '-c', 'Release', '--no-restore', '--nologo', '-o', $packageFeed) +
            $isolatedProperties +
            @(
                "-p:TwPackageVersion=$Version",
                "-p:PackageVersion=$Version",
                '-nodeReuse:false'
            )
        )
        Assert-PackageOutputTree `
            -OutputRoot $OutputRoot `
            -FeedRoot $FeedRoot `
            -RunsRoot $RunsRoot
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

function Get-NuspecDependencyIds {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackagePath
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [System.StringComparison]::OrdinalIgnoreCase) })
        if ($nuspecEntries.Count -ne 1) {
            throw "Package must contain exactly one nuspec: $PackagePath"
        }

        $stream = $nuspecEntries[0].Open()
        $reader = [System.IO.StreamReader]::new($stream)
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }

        return @(
            $nuspec.SelectNodes("//*[local-name()='dependency']") |
                ForEach-Object { [string]$_.id }
        )
    }
    finally {
        $archive.Dispose()
    }
}

function Get-ControlledFeedPackages {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputRoot,

        [Parameter(Mandatory = $true)]
        [string]$FeedRoot,

        [Parameter(Mandatory = $true)]
        [string]$RunsRoot
    )

    Assert-PackageOutputTree `
        -OutputRoot $OutputRoot `
        -FeedRoot $FeedRoot `
        -RunsRoot $RunsRoot
    return @(Get-ChildItem -LiteralPath $FeedRoot -Filter '*.nupkg' -File)
}

function Get-ControlledNuspecDependencyIds {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackagePath,

        [Parameter(Mandatory = $true)]
        [string]$OutputRoot,

        [Parameter(Mandatory = $true)]
        [string]$FeedRoot,

        [Parameter(Mandatory = $true)]
        [string]$RunsRoot
    )

    Assert-PackageOutputTree `
        -OutputRoot $OutputRoot `
        -FeedRoot $FeedRoot `
        -RunsRoot $RunsRoot
    Assert-ChildPath -Parent $FeedRoot -Child $PackagePath
    return @(Get-NuspecDependencyIds -PackagePath $PackagePath)
}

function Get-LockDependencyIds {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LockPath
    )

    $lock = Get-Content -Raw -LiteralPath $LockPath | ConvertFrom-Json
    $packageIds = [System.Collections.Generic.List[string]]::new()
    foreach ($framework in $lock.dependencies.PSObject.Properties) {
        foreach ($package in $framework.Value.PSObject.Properties) {
            $packageIds.Add([string]$package.Name)
            $dependencies = $package.Value.PSObject.Properties['dependencies']
            if ($null -ne $dependencies) {
                foreach ($dependency in $dependencies.Value.PSObject.Properties) {
                    $packageIds.Add([string]$dependency.Name)
                }
            }
        }
    }

    return $packageIds.ToArray()
}

function Assert-NoForbiddenDependencies {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]]$PackageIds,

        [Parameter(Mandatory = $true)]
        [System.Collections.Generic.HashSet[string]]$RetiredPackageIds
    )

    $violations = @(
        $PackageIds |
            Where-Object {
                $RetiredPackageIds.Contains($_) -or
                $_ -ieq 'Autofac' -or
                $_ -ilike 'Autofac.*' -or
                $_ -ieq 'Castle' -or
                $_ -ilike 'Castle.*'
            } |
            Sort-Object -Unique
    )

    if ($violations.Count -gt 0) {
        throw "$Source contains forbidden dependencies: $($violations -join ', ')"
    }
}

$repositoryRoot = Resolve-FullPath (Join-Path $PSScriptRoot '../../../..')
$dotnetRoot = Join-Path $repositoryRoot 'backend/dotnet'
$buildingBlocksRoot = Join-Path $dotnetRoot 'BuildingBlocks'
$topologyPath = Join-Path $buildingBlocksRoot 'building-blocks-topology.json'
$topology = Get-Content -Raw -LiteralPath $topologyPath | ConvertFrom-Json
$runtimeProjects = @($topology.runtimeProjects)

if ($runtimeProjects.Count -ne 57) {
    throw "Topology must contain exactly 57 runtime projects; found $($runtimeProjects.Count)"
}

$retiredPackageIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($retiredPackage in $topology.retiredPackages) {
    [void]$retiredPackageIds.Add([string]$retiredPackage.packageId)
}

$outputRoot = Resolve-FullPath $OutputDirectory
$feedRoot = Join-Path $outputRoot 'feed'
$runsRoot = Join-Path $outputRoot 'runs'
Assert-NoReparsePoint -Path $outputRoot
[System.IO.Directory]::CreateDirectory($outputRoot) | Out-Null
Assert-NoReparsePoint -Path $outputRoot
Assert-ChildPath -Parent $outputRoot -Child $feedRoot
[System.IO.Directory]::CreateDirectory($feedRoot) | Out-Null
Assert-ChildPath -Parent $outputRoot -Child $feedRoot
Assert-ChildPath -Parent $outputRoot -Child $runsRoot
[System.IO.Directory]::CreateDirectory($runsRoot) | Out-Null
Assert-ChildPath -Parent $outputRoot -Child $runsRoot

$runRoot = Join-Path $runsRoot ([System.Guid]::NewGuid().ToString('N'))
Assert-ChildPath -Parent $runsRoot -Child $runRoot
[System.IO.Directory]::CreateDirectory($runRoot) | Out-Null
Assert-ChildPath -Parent $runsRoot -Child $runRoot

try {
    $approvedSources = @(Get-ApprovedPackageSources (Join-Path $dotnetRoot 'NuGet.Config'))
    $consumerNuGetConfig = Join-Path $runRoot 'NuGet.Config'
    $globalPackagesFolder = Join-Path $runRoot 'nuget-packages'
    $packRoot = Join-Path $runRoot 'pack'
    Assert-ChildPath -Parent $runRoot -Child $packRoot
    [System.IO.Directory]::CreateDirectory($packRoot) | Out-Null
    Assert-ChildPath -Parent $runRoot -Child $packRoot
    New-IsolatedNuGetConfig `
        -Path $consumerNuGetConfig `
        -LocalFeed $feedRoot `
        -GlobalPackagesFolder $globalPackagesFolder `
        -ApprovedSources $approvedSources

    $packages = [System.Collections.Generic.List[object]]::new()
    foreach ($runtimeProject in $runtimeProjects) {
        $projectPath = Join-Path (Join-Path $buildingBlocksRoot 'src') ([string]$runtimeProject.path)
        if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
            throw "Topology project does not exist: $projectPath"
        }

        $packageId = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
        $packageWorkRoot = Join-Path $packRoot $packageId
        Assert-ChildPath -Parent $packRoot -Child $packageWorkRoot
        Write-Host "Packing $packageId $Version"
        Restore-LockedAndPack `
            -Project $projectPath `
            -NuGetConfig $consumerNuGetConfig `
            -GlobalPackagesFolder $globalPackagesFolder `
            -ProjectWorkRoot $packageWorkRoot `
            -OutputRoot $outputRoot `
            -FeedRoot $feedRoot `
            -RunsRoot $runsRoot `
            -Version $Version

        $packages.Add([pscustomobject]@{
            PackageId = $packageId
            ProjectPath = $projectPath
            LockPath = Join-Path (Split-Path -Parent $projectPath) 'packages.lock.json'
        })
    }

    $feedPackages = @(Get-ControlledFeedPackages `
        -OutputRoot $outputRoot `
        -FeedRoot $feedRoot `
        -RunsRoot $runsRoot)
    foreach ($package in $packages) {
        $expectedFileName = "$($package.PackageId).$Version.nupkg"
        $matches = @($feedPackages | Where-Object { $_.Name.Equals($expectedFileName, [System.StringComparison]::OrdinalIgnoreCase) })
        if ($matches.Count -ne 1) {
            throw "Expected exactly one package for $($package.PackageId) $Version; found $($matches.Count)"
        }

        $package | Add-Member -NotePropertyName PackagePath -NotePropertyValue $matches[0].FullName
        Assert-NoForbiddenDependencies `
            -Source $matches[0].FullName `
            -PackageIds @(Get-ControlledNuspecDependencyIds `
                -PackagePath $matches[0].FullName `
                -OutputRoot $outputRoot `
                -FeedRoot $feedRoot `
                -RunsRoot $runsRoot) `
            -RetiredPackageIds $retiredPackageIds

        if (-not (Test-Path -LiteralPath $package.LockPath -PathType Leaf)) {
            throw "Runtime project does not contain a lock file: $($package.LockPath)"
        }

        Assert-NoForbiddenDependencies `
            -Source $package.LockPath `
            -PackageIds @(Get-LockDependencyIds $package.LockPath) `
            -RetiredPackageIds $retiredPackageIds
    }

    Set-Content -LiteralPath (Join-Path $runRoot 'Directory.Build.props') -Encoding utf8 -Value @'
<Project>
  <PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
'@
    Set-Content -LiteralPath (Join-Path $runRoot 'Directory.Build.targets') -Encoding utf8 -Value '<Project />'

    foreach ($package in $packages) {
        $consumerRoot = Join-Path $runRoot $package.PackageId
        Assert-ChildPath -Parent $runRoot -Child $consumerRoot
        [System.IO.Directory]::CreateDirectory($consumerRoot) | Out-Null
        $escapedPackageId = [System.Security.SecurityElement]::Escape($package.PackageId)
        $escapedVersion = [System.Security.SecurityElement]::Escape($Version)
        $consumerProject = Join-Path $consumerRoot 'Consumer.csproj'
        Set-Content -LiteralPath $consumerProject -Encoding utf8 -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$escapedPackageId" Version="$escapedVersion" />
  </ItemGroup>
</Project>
"@

        Write-Host "Consuming $($package.PackageId) $Version"
        Restore-LockedAndBuild `
            -Project $consumerProject `
            -NuGetConfig $consumerNuGetConfig `
            -GlobalPackagesFolder $globalPackagesFolder `
            -OutputRoot $outputRoot `
            -FeedRoot $feedRoot `
            -RunsRoot $runsRoot
    }
}
finally {
    Remove-ControlledChild -Parent $runsRoot -Child $runRoot
}

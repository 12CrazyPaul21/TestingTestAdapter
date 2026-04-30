param(
    [string]$Target,
    [string]$Version
)

if (!(Test-Path Env:VSINSTALLDIR)) {
  Write-Error "ERROR: must run in Developer Command Prompt for Visual Studio"
  exit 1
}

function Update-Appveyor {
    param([string]$MajorMinorPatch)
    
    $content = Get-Content .\appveyor.yml
    $content = $content -replace "(?<=version: ')\d+\.\d+\.\d+", $MajorMinorPatch
    Set-Content .\appveyor.yml $content
}

function Update-VsixVersion {
    if ($Version) {
        $vsix_version = $Version
    } else {
        $vsix_version = dotnet tool run dotnet-gitversion /showvariable MajorMinorPatch
        if (-not $vsix_version) {
            throw "Failed to get version from GitVersion"
        }
        $vsix_version = $vsix_version + ".0"
    }

    msbuild .\build\BumpVersion.proj /t:BumpVsixVersion /p:VsixVersion=$vsix_version /nologo /v:quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to bump VSIX version"
    }
    Update-Appveyor ($vsix_version -replace '\.[^.]+$', '')

    Write-Host "New VSIX version: $vsix_version"
    Write-Host "VSIX version bumped successfully!"
}

function Update-ProjectVersion {
    param([string]$ProjectName, [string]$ProjectVersion)

    $csproj = [System.IO.Path]::GetFullPath("src\$ProjectName\$ProjectName.csproj")

    msbuild .\build\BumpVersion.proj /t:BumpProductVersion /p:ProjectPath=$csproj /p:ProjectVersion=$ProjectVersion /nologo /v:quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to bump $ProjectName version"
    }

    Write-Host "$ProjectName version bumped successfully!"
}

switch ($Target) {
    "vsix" { Update-VsixVersion }
    Default {
        if ($Target -and (Test-Path "src\$Target\$Target.csproj")) {
            if (-not $Version) {
                Write-Host "ERROR: Version is required for project target" -ForegroundColor Red
                Write-Host "Usage: bump.bat $Target 1.2.3.4" -ForegroundColor Yellow
                exit 1
            }
            Update-ProjectVersion -ProjectName $Target -ProjectVersion $Version
        } else {
            Write-Host "Invalid target: $Target" -ForegroundColor Red
        }
    }
}

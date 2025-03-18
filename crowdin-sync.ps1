param (
    [Parameter(Mandatory=$false)]
    [string]$Action = "help",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectId = $env:CROWDIN_PROJECT_ID,
    
    [Parameter(Mandatory=$false)]
    [string]$ApiToken = $env:CROWDIN_API_TOKEN
)

$ErrorActionPreference = "Stop"

function EnsureCrowdinCliIsInstalled {
    try {
        $crowdinVersion = crowdin --version
        Write-Host "Crowdin CLI is installed: $crowdinVersion"
    } catch {
        Write-Host "Installing Crowdin CLI..."
        npm install -g @crowdin/cli
    }
}

function SetEnvironmentVariables {
    if (-not $ProjectId) {
        $ProjectId = Read-Host "Enter your Crowdin Project ID"
    }
    
    if (-not $ApiToken) {
        $ApiToken = Read-Host "Enter your Crowdin API Token"
    }
    
    $env:CROWDIN_PROJECT_ID = $ProjectId
    $env:CROWDIN_API_TOKEN = $ApiToken
    
    Write-Host "Environment variables set for this session"
}

function UploadSources {
    Write-Host "Uploading source files to Crowdin..."
    crowdin upload sources
}

function UploadTranslations {
    Write-Host "Uploading existing translations to Crowdin..."
    crowdin upload translations
}

function DownloadTranslations {
    Write-Host "Downloading translations from Crowdin..."
    crowdin download
}

function ShowHelp {
    Write-Host @"
Crowdin Sync Script for FoliCon

Usage:
    .\crowdin-sync.ps1 -Action <action> [-ProjectId <project_id>] [-ApiToken <api_token>]

Actions:
    upload-sources     - Upload source files to Crowdin
    upload-translations - Upload existing translations to Crowdin
    download           - Download translations from Crowdin
    help               - Show this help message

If ProjectId and ApiToken are not provided, the script will use environment variables
CROWDIN_PROJECT_ID and CROWDIN_API_TOKEN or prompt for them.

Example:
    .\crowdin-sync.ps1 -Action download
"@
}

# Main execution flow
EnsureCrowdinCliIsInstalled
SetEnvironmentVariables

switch ($Action.ToLower()) {
    "upload-sources" { UploadSources }
    "upload-translations" { UploadTranslations }
    "download" { DownloadTranslations }
    default { ShowHelp }
}
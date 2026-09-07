# switch-model.ps1
# Usage:
#   .\switch-model.ps1 glm         → switch to GLM 5.2 (z.ai)
#   .\switch-model.ps1 openrouter  → switch to OpenRouter free
#   .\switch-model.ps1 omnirouter  → switch to OmniRouter Local
#   .\switch-model.ps1             → show current active model

param(
    [string]$Model = ""
)

$settingsPath = ".claude\settings.local.json"
$glmProfile   = ".claude\settings.glm.json"
$orProfile    = ".claude\settings.openrouter.json"
$omniProfile  = ".claude\settings.omnirouter.json"

function Get-ActiveModel {
    if (Test-Path $settingsPath) {
        $json = Get-Content -Raw $settingsPath | ConvertFrom-Json
        $url = $json.env.ANTHROPIC_BASE_URL
        if ($url -like "*z.ai*")          { return "glm" }
        if ($url -like "*openrouter*")    { return "openrouter" }
        if ($url -like "*localhost:20128*" -or $url -like "*omnirouter*") { return "omnirouter" }
        return "unknown"
    }
    return "none"
}

if ($Model -eq "") {
    $current = Get-ActiveModel
    Write-Host "Active model profile: $current" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\switch-model.ps1 glm        (GLM 5.2 via z.ai)"
    Write-Host "       .\switch-model.ps1 openrouter  (free model via OpenRouter)"
    Write-Host "       .\switch-model.ps1 omnirouter  (OmniRouter Local)"
    exit
}

switch ($Model.ToLower()) {
    "glm" {
        Copy-Item $glmProfile $settingsPath -Force
        Write-Host "✓ Switched to GLM 5.2 (z.ai)" -ForegroundColor Green
    }
    "openrouter" {
        Copy-Item $orProfile $settingsPath -Force
        Write-Host "✓ Switched to OpenRouter (free)" -ForegroundColor Green
    }
    "omnirouter" {
        Copy-Item $omniProfile $settingsPath -Force
        Write-Host "✓ Switched to OmniRouter" -ForegroundColor Green
    }
    default {
        Write-Host "Unknown model '$Model'. Use 'glm', 'openrouter', or 'omnirouter'." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Restart Claude Code for changes to take effect." -ForegroundColor Yellow

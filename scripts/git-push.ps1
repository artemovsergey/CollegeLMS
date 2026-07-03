$token = $env:GITHUB_TOKEN
if (-not $token) {
    Write-Error "GITHUB_TOKEN env var is not set"
    exit 1
}

$repo = git rev-parse --show-toplevel 2>$null
if (-not $repo) {
    Write-Error "Not a git repository"
    exit 1
}

git add -A

$msg = "auto: $((Get-Date -Format 'yyyy-MM-dd HH:mm'))"
git commit -m $msg

$origin = git remote get-url origin
if ($origin -match "^https://(.+@)?github\.com") {
    $path = $origin -replace "^https://[^/]+", ""
    git remote set-url origin "https://x-access-token:$token@github.com$path"
}

git push

git remote set-url origin $origin

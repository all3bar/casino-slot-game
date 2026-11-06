<#
.SYNOPSIS
    현재 실행 폴더 이름으로 Gitea에 자동 커밋 & 푸시하는 파워쉘 스크립트
.DESCRIPTION
    1. 현재 스크립트가 실행되는 폴더명을 자동으로 인식합니다.
    2. Git 저장소가 없으면 자동 git init 수행
    3. Gitea(http://localhost:3000)에 해당 폴더명의 원격 저장소가 없으면 API로 자동 생성
    4. 변경 사항 감지 후 커밋 및 푸시
.PARAMETER Message
    커밋 메시지 (생략 시 입력 창이 나타나며, 엔터 시 '[폴더명] Update: 날짜시간' 사용)
#>

param(
    [Parameter(Position = 0, Mandatory = $false)]
    [string]$Target,

    [Parameter(Position = 1, Mandatory = $false)]
    [string]$Message,

    [switch]$NoPause
)

# 콘솔 UTF-8 인코딩 설정
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# 대상 폴더 결정 (인자로 경로가 전달된 경우 해당 폴더, 아니면 스크립트 위치 폴더)
$targetPath = $PSScriptRoot

if (-not [string]::IsNullOrWhiteSpace($Target)) {
    if (Test-Path -Path $Target -PathType Container) {
        # 첫 번째 인자가 존재하는 디렉터리인 경우
        $targetPath = (Resolve-Path -Path $Target).Path
    } elseif (-not [string]::IsNullOrWhiteSpace($Message)) {
        # 첫 번째 인자가 폴더가 아닌데 두 번째 인자(Message)도 전달된 경우
        if (Test-Path -Path $Target) {
            $targetPath = (Get-Item $Target).DirectoryName
        } else {
            Write-Host "❌ 오류: 지정한 대상 폴더를 찾을 수 없습니다: $Target" -ForegroundColor Red
            if (-not $NoPause) { Read-Host "Enter 키를 누르면 종료합니다..." }
            exit 1
        }
    } else {
        # 첫 번째 인자만 전달되었고 디렉터리가 아닌 경우 -> 커밋 메시지로 간주
        $Message = $Target
        $targetPath = $PSScriptRoot
    }
}

# 대상 폴더로 이동
Set-Location -Path $targetPath

# 현재 폴더 이름 동적 추출
$folderName = (Get-Item $targetPath).Name
$giteaServer = "http://localhost:3000"
$giteaUser = "admin"
$giteaPass = "admin123456"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "       🚀 Gitea Git Push 도구             " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "📁 대상 프로젝트 (폴더명): $folderName" -ForegroundColor Yellow
Write-Host "📂 실행 경로: $targetPath" -ForegroundColor Gray

# Git 설치 확인
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "❌ 오류: Git이 설치되어 있지 않거나 PATH에 없습니다." -ForegroundColor Red
    if (-not $NoPause) { Read-Host "Enter 키를 누르면 종료합니다..." }
    exit 1
}

# Git 저장소 초기화 여부 확인 (없으면 자동 git init)
$isGitRepo = git rev-parse --is-inside-work-tree 2>$null
if ($isGitRepo -ne "true") {
    Write-Host "📦 Git 저장소가 아닙니다. 새로운 저장소로 초기화합니다 (git init -b main)..." -ForegroundColor Yellow
    git init -b main
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Git 초기화 실패" -ForegroundColor Red
        if (-not $NoPause) { Read-Host "Enter 키를 누르면 종료합니다..." }
        exit 1
    }
}

# 로컬 Git 권장 설정 (CRLF 줄바꿈 경고 및 서브모듈 힌트 메시지 억제)
git config core.autocrlf false
git config core.safecrlf false
git config advice.addEmbeddedRepo false

# 기본 .gitignore 생성 (없는 경우)
if (-not (Test-Path "$targetPath\.gitignore")) {
    $defaultGitignore = @"
node_modules/
.env
*.log
npm-debug.log*
__pycache__/
*.py[cod]
.DS_Store
Thumbs.db
"@
    Set-Content -Path "$targetPath\.gitignore" -Value $defaultGitignore -Encoding UTF8
    Write-Host "📄 기본 .gitignore 파일을 생성했습니다." -ForegroundColor Gray
}

# 한글을 로마자로 변환하는 헬퍼 함수 (Gitea 저장소명 규격 준수용)
function Convert-KoreanToRoman ([string]$text) {
    $cho = @('g','kk','n','d','tt','r','m','b','pp','s','ss','','j','jj','ch','k','t','p','h')
    $jung = @('a','ae','ya','yae','eo','e','yeo','ye','o','wa','wae','oe','yo','u','wo','we','wi','yu','eu','ui','i')
    $jong = @('','k','k','ks','n','nj','nh','t','l','lg','lm','lb','ls','lt','lp','lh','m','p','ps','t','t','ng','t','t','k','t','p','h')
    $res = ""
    foreach ($ch in $text.ToCharArray()) {
        $code = [int][char]$ch
        if ($code -ge 44032 -and $code -le 55203) {
            $offset = $code - 44032
            $c = [Math]::Floor($offset / 588)
            $u = [Math]::Floor(($offset % 588) / 28)
            $j = $offset % 28
            $res += $cho[$c] + $jung[$u] + $jong[$j]
        } elseif ($ch -match '[a-zA-Z0-9_\-\.]') {
            $res += $ch
        } elseif ($ch -match '\s') {
            $res += '-'
        }
    }
    return ($res.ToLower() -replace '-+', '-' -replace '^[._\-]+|[._\-]+$', '')
}

# Gitea 저장소 이름 결정 (Gitea는 영문, 숫자, 대시, 점, 밑줄만 지원)
$repoName = $folderName
if ($repoName -match '[^a-zA-Z0-9_\-\.]') {
    $suggestedName = Convert-KoreanToRoman $folderName
    if ([string]::IsNullOrWhiteSpace($suggestedName)) {
        $suggestedName = "repo-" + (Get-Date -Format "yyyyMMddHHmmss")
    }

    Write-Host "⚠️ 폴더명('$folderName')에 Gitea에서 지원하지 않는 문자(한글/공백 등)가 포함되어 있습니다." -ForegroundColor Yellow
    if ($NoPause) {
        $repoName = $suggestedName
        Write-Host "💡 Gitea 저장소 이름으로 자동 변환된 이름을 사용합니다: $repoName" -ForegroundColor Cyan
    } else {
        Write-Host "💡 Gitea에 생성할 저장소 이름을 입력하세요 (Enter 입력 시: '$suggestedName'):" -ForegroundColor Yellow -NoNewline
        $inputRepo = Read-Host " "
        if ([string]::IsNullOrWhiteSpace($inputRepo) -or $inputRepo.Trim() -eq '`n') {
            $repoName = $suggestedName
        } else {
            $repoName = $inputRepo.Trim()
        }
    }
}

# 현재 브랜치 확인
$branch = (git branch --show-current 2>$null)
if (-not $branch) {
    $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
}
if (-not $branch -or $branch -eq "HEAD") {
    $branch = "main"
}

# 원격 저장소(origin) 확인 및 Gitea 저장소 자동 연동
$remoteUrl = (git remote get-url origin 2>$null)
$targetRemote = "$giteaServer/$giteaUser/$repoName.git"
$authHeader = @{ Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${giteaUser}:${giteaPass}")) }

# 기존 origin이 Gitea가 아닌 외부 저장소(GitHub 등)인 경우 upstream으로 백업
if ($remoteUrl -and ($remoteUrl -notlike "$giteaServer*")) {
    Write-Host "⚠️ 기존 origin 원격지가 Gitea가 아닙니다: $remoteUrl" -ForegroundColor Yellow
    git remote remove upstream 2>$null
    git remote rename origin upstream 2>$null
    Write-Host "📦 기존 원격지를 'upstream'으로 보관하고, Gitea를 'origin'으로 새로 연동합니다." -ForegroundColor Gray
    $remoteUrl = $null
}

# Gitea 서버에서 실제 저장소 존재 여부 항상 확인 (API 호출)
Write-Host "🔍 Gitea 서버($giteaServer)에서 저장소('$repoName') 확인 중..." -ForegroundColor Cyan
$repoExists = $false
try {
    $encodedRepoName = [System.Uri]::EscapeDataString($repoName)
    $null = Invoke-RestMethod -Uri "$giteaServer/api/v1/repos/$giteaUser/$encodedRepoName" -Headers $authHeader -Method Get -ErrorAction Stop
    $repoExists = $true
    Write-Host "✅ Gitea에 기존 저장소 '$repoName' 확인됨" -ForegroundColor Green
} catch {
    $repoExists = $false
}

if (-not $repoExists) {
    Write-Host "✨ Gitea에 '$repoName' 저장소가 없어 새로 자동 생성합니다..." -ForegroundColor Yellow
    $createBody = @{ name = $repoName; description = "$folderName project"; private = $false } | ConvertTo-Json
    try {
        $createdRepo = Invoke-RestMethod -Uri "$giteaServer/api/v1/user/repos" -Headers $authHeader -Method Post -Body $createBody -ContentType "application/json; charset=utf-8" -ErrorAction Stop
        Write-Host "🎉 Gitea 저장소 생성 완료: $($createdRepo.html_url)" -ForegroundColor Green
    } catch {
        Write-Host "❌ Gitea 저장소 API 생성 실패: $_" -ForegroundColor Red
        if (-not $NoPause) { Read-Host "Enter 키를 누르면 종료합니다..." }
        exit 1
    }
}

# 로컬 Git 원격지(origin)를 올바른 Gitea URL로 동기화
if (-not $remoteUrl) {
    git remote add origin $targetRemote
    Write-Host "🔗 원격 저장소(origin) 설정: $targetRemote" -ForegroundColor Cyan
} elseif ($remoteUrl -ne $targetRemote) {
    git remote set-url origin $targetRemote
    Write-Host "🔄 원격 저장소(origin) URL 갱신: $targetRemote" -ForegroundColor Cyan
}
$remoteUrl = $targetRemote

Write-Host "🌿 브랜치: $branch" -ForegroundColor Gray
Write-Host "🌐 원격지: $remoteUrl" -ForegroundColor Gray
Write-Host ""

# 변경 사항 확인 (unstaged, staged, untracked)
$status = git status --porcelain

if (-not $status) {
    # 워킹 트리가 깨끗한 경우, 아직 푸시되지 않은 로컬 커밋이 있는지 확인
    $hasUnpushed = $false
    $hasRemoteBranch = git rev-parse --verify "origin/$branch" 2>$null

    if ($hasRemoteBranch) {
        $unpushed = git cherry -v "origin/$branch" "$branch" 2>$null
        if ($unpushed) { $hasUnpushed = $true }
    } else {
        # 원격에 해당 브랜치가 아직 없는 경우 로컬에 커밋이 있는지 확인
        $hasCommits = git rev-parse --verify HEAD 2>$null
        if ($hasCommits) { $hasUnpushed = $true }
    }

    if ($hasUnpushed) {
        Write-Host "ℹ️ 아직 원격에 푸시되지 않은 로컬 커밋이 있습니다:" -ForegroundColor Yellow
        if ($hasRemoteBranch) {
            git log "origin/$branch..$branch" --oneline -n 5 2>$null
        } else {
            git log -n 5 --oneline 2>$null
        }
        Write-Host ""
        Write-Host "🚀 원격 저장소($branch)로 푸시합니다..." -ForegroundColor Yellow
        git push -u origin $branch
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ 푸시가 성공적으로 완료되었습니다!" -ForegroundColor Green
        } else {
            Write-Host "❌ 푸시 중 오류가 발생했습니다." -ForegroundColor Red
        }
    } else {
        Write-Host "✨ 변경 사항이 없습니다. 모든 커밋이 최신 상태입니다." -ForegroundColor Green
    }

    if (-not $NoPause) {
        Write-Host ""
        Read-Host "Enter 키를 누르면 종료합니다..."
    }
    exit 0
}

# 변경된 파일 목록 출력
Write-Host "📋 변경된 파일 목록:" -ForegroundColor Yellow
git status --short
Write-Host ""

# 커밋 메시지 결정 (기본값에 폴더명 포함)
if ([string]::IsNullOrWhiteSpace($Message)) {
    $currentTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $defaultMsg = "[$folderName] Update: $currentTime"
    
    Write-Host "💡 커밋 메시지를 입력하세요 (Enter 입력 시: '$defaultMsg'):" -ForegroundColor Yellow -NoNewline
    $inputMsg = Read-Host " "
    
    if ([string]::IsNullOrWhiteSpace($inputMsg) -or $inputMsg.Trim() -eq '`n') {
        $Message = $defaultMsg
    } else {
        $Message = $inputMsg.Trim()
    }
}

Write-Host ""
Write-Host "1️⃣ 변경 사항 추가 (git add -A)..." -ForegroundColor Cyan
git add -A
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 파일 변경 사항 추가(git add)에 실패했습니다." -ForegroundColor Red
    if (-not $NoPause) { Read-Host "Enter 키를 누르면 종료합니다..." }
    exit 1
}

Write-Host "2️⃣ 커밋 생성 (git commit -m `"$Message`")..." -ForegroundColor Cyan
git commit -m "$Message"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 커밋 생성에 실패했습니다." -ForegroundColor Red
    if (-not $NoPause) { Read-Host "Enter 키를 누르면 종료합니다..." }
    exit 1
}

Write-Host "3️⃣ 원격 저장소로 푸시 (git push -u origin $branch)..." -ForegroundColor Cyan
git push -u origin $branch

# 저장소 웹 주소 계산
$webUrl = "$giteaServer/$giteaUser/$repoName"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host "  ✅ Gitea 푸시 완료!                      " -ForegroundColor Green
    Write-Host "  📁 프로젝트: $folderName                  " -ForegroundColor Green
    Write-Host "  🔗 저장소 확인: $webUrl                   " -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ 푸시 중 오류가 발생했습니다. 네트워크 또는 권한을 확인해주세요." -ForegroundColor Red
}

if (-not $NoPause) {
    Write-Host ""
    Read-Host "Enter 키를 누르면 종료합니다..."
}

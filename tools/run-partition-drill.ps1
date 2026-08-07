$ErrorActionPreference = 'Stop'
$env:WALLET_ENCRYPTION_KEY = [Convert]::ToBase64String((0..31 | ForEach-Object { [byte]$_ }))
Write-Output "WALLET_ENCRYPTION_KEY=$env:WALLET_ENCRYPTION_KEY"

$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    docker compose -f deploy/cometbft/docker-compose.multinode.yml up --build -d
    Start-Sleep -Seconds 15
    foreach ($port in 26657, 26658, 26659, 26660) {
        $attempt = 0
        do {
            try {
                Invoke-RestMethod "http://localhost:$port/status" | Out-Null
                Write-Output "Port $port OK"
                break
            }
            catch {
                $attempt++
                if ($attempt -ge 10) {
                    throw "Port $port did not become ready in time."
                }
                Start-Sleep -Seconds 2
            }
        } while ($true)
    }
}
finally {
    docker compose -f deploy/cometbft/docker-compose.multinode.yml down -v
    Pop-Location
}

</parameter>
<task_progress>
- [x] Menganalisis penggunaan LiteDB di seluruh proyek
- [x] Mempelajari struktur DB dan konsumen
- [x] Menambahkan LightningDB, menghapus LiteDB
- [x] Membuat LmdbSerializer dan LmdbStore
- [x] Memigrasi 6 wrapper DB dan DbService
- [x] Memperbarui konsumen GetAll() di Facade dan gRPC
- [x] Build succeeded, 143/143 test passed
- [x] Commit migrasi di branch feat/storage-migrate-litedb-to-lmdb
- [ ] Menjalankan drill test partition recovery
</task_progress>
</write_to_file>
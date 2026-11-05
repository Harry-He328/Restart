param([string]$Method = 'resources/read', [string]$ParamsJson = '{"uri":"mcpforunity://editor/state"}')
$ErrorActionPreference = 'Stop'
$headers = @{ Accept = 'application/json, text/event-stream' }
$endpoint = 'http://127.0.0.1:8080/mcp'
function Send-Mcp($payload) {
    $response = Invoke-WebRequest $endpoint -Method Post -Headers $headers -ContentType 'application/json' -Body ($payload | ConvertTo-Json -Depth 30 -Compress) -TimeoutSec 55
    foreach ($line in ($response.Content -split "`n")) {
        if ($line.StartsWith('data: ')) {
            $message = $line.Substring(6) | ConvertFrom-Json
            if ($null -ne $message.id) { return $message }
        }
    }
}
$r = Invoke-WebRequest $endpoint -Method Post -Headers $headers -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"restart-development","version":"1"}}}' -TimeoutSec 10
$headers['Mcp-Session-Id'] = [string]$r.Headers['mcp-session-id'][0]
Invoke-WebRequest $endpoint -Method Post -Headers $headers -ContentType 'application/json' -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' -TimeoutSec 10 | Out-Null
Send-Mcp @{ jsonrpc='2.0'; id=2; method=$Method; params=($ParamsJson | ConvertFrom-Json) } | ConvertTo-Json -Depth 40

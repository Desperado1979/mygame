# Risk Doctor Report

- Checked At: 2026-04-15T09:36:23.534Z
- Base: http://127.0.0.1:8788
- Timeout: 5000ms
- Retries: 0
- Parallel: on
- Auto Fallback Serial: on
- Simulate Fail: dashboard
- Result: FAIL (1)
- Fallback Attempted: yes

| Check | Status | HTTP | Detail | Attempts | Fix |
|---|---|---:|---|---:|---|
| health | OK | 200 | ok | 1 |  |
| metrics_report | OK | 200 | ok | 1 |  |
| alerts | OK | 200 | ok | 1 |  |
| player_alerts | OK | 200 | ok | 1 |  |
| dashboard | FAIL | 200 | unexpected_body | 1 | upgrade server to latest code with /metrics/dashboard endpoint |


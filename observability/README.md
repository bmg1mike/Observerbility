# Observability: Prometheus + Grafana + OpenTelemetry (.NET)

This guide shows how to:
- Instrument an ASP.NET app with OpenTelemetry and expose Prometheus metrics
- Run Prometheus and Grafana via Docker
- Wire Grafana to Prometheus and import a starter dashboard

Works on Windows (Git Bash/WSL) and Linux/macOS. Replace paths/ports if needed.

## 1) Instrument ASP.NET with OpenTelemetry metrics

Install packages (already present in this repo):
- OpenTelemetry.Extensions.Hosting
- OpenTelemetry.Instrumentation.AspNetCore
- OpenTelemetry.Instrumentation.Runtime
- OpenTelemetry.Exporter.Prometheus.AspNetCore

Minimal setup (Program.cs):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            // Add your custom meters if any
            .AddMeter("Todos.Api")
            .AddPrometheusExporter();
    });

var app = builder.Build();

// Expose /metrics for Prometheus to scrape
app.MapPrometheusScrapingEndpoint();

app.Run();
```

Ensure the app listens on an HTTP port Prometheus can reach (e.g., http://localhost:5222).

## 2) Prometheus setup

Create a `prometheus.yml`:

```yaml
global:
  scrape_interval: 15s
  scrape_timeout: 10s

scrape_configs:
  - job_name: 'todos-api'
    metrics_path: /metrics
    scheme: http
    static_configs:
      - targets: ['host.docker.internal:5222'] # use your app's host:port
        labels:
          service: 'Todos.Api'
```

- Windows with Docker Desktop: `host.docker.internal` points from containers to the host.
- Linux: replace with your host IP or run the app in the same Docker network and target by container name.

Run Prometheus (Git Bash on Windows uses /c/ path style; disable path conversion):

```bash
# Windows Git Bash
MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' \
docker run -d --name prometheus --network observability \
  -p 9090:9090 \
  -v /c/Users/<you>/path/to/observability/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro \
  --restart unless-stopped prom/prometheus:v2.53.0
```

```bash
# Linux/macOS
docker run -d --name prometheus --network observability \
  -p 9090:9090 \
  -v "$PWD/observability/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro" \
  --restart unless-stopped prom/prometheus:v2.53.0
```

Open Prometheus: http://localhost:9090 (Status -> Targets should show todos-api up once the app is running).

## 3) Grafana setup

Recommended: create a Docker network and a persistent volume.

```bash
# Create a shared network for Prometheus & Grafana
docker network create observability || true

# Persistent storage for Grafana
docker volume create grafana-storage
```

Run Grafana and point it at Prometheus automatically (provisioning):

- Datasource file: `observability/grafana/provisioning/datasources/datasource.yml`

```yaml
apiVersion: 1

deleteDatasources:
  - name: Prometheus
    orgId: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    jsonData:
      timeInterval: 15s
    editable: true
```

- Dashboards provider: `observability/grafana/provisioning/dashboards/dashboards.yml`

```yaml
apiVersion: 1

providers:
  - name: 'Default'
    orgId: 1
    folder: ''
    type: file
    disableDeletion: true
    updateIntervalSeconds: 30
    options:
      path: /etc/grafana/provisioning/dashboards-json
```

- Place dashboards (JSON) in `observability/grafana/provisioning/dashboards-json/`.

Start Grafana (mount provisioning and storage):

```bash
# Windows Git Bash
docker run -d --name grafana --network observability \
  -p 3000:3000 \
  -v grafana-storage:/var/lib/grafana \
  -v /c/Users/<you>/path/to/observability/grafana/provisioning/datasources:/etc/grafana/provisioning/datasources \
  -v /c/Users/<you>/path/to/observability/grafana/provisioning/dashboards:/etc/grafana/provisioning/dashboards \
  -v /c/Users/<you>/path/to/observability/grafana/provisioning/dashboards-json:/etc/grafana/provisioning/dashboards-json \
  --restart unless-stopped grafana/grafana:10.4.3
```

```bash
# Linux/macOS
docker run -d --name grafana --network observability \
  -p 3000:3000 \
  -v grafana-storage:/var/lib/grafana \
  -v "$PWD/observability/grafana/provisioning/datasources:/etc/grafana/provisioning/datasources" \
  -v "$PWD/observability/grafana/provisioning/dashboards:/etc/grafana/provisioning/dashboards" \
  -v "$PWD/observability/grafana/provisioning/dashboards-json:/etc/grafana/provisioning/dashboards-json" \
  --restart unless-stopped grafana/grafana:10.4.3
```

Open Grafana: http://localhost:3000 (default admin/admin; you’ll be prompted to set a new password). The Prometheus datasource should be present automatically.

## 4) Import a starter dashboard

Option A: Provisioning (auto). Save the JSON below to `observability/grafana/provisioning/dashboards-json/todos-api-overview.json`. Grafana will auto-load it on startup.

Option B: Manual import. In Grafana -> Dashboards -> New -> Import, paste the JSON below.

```json
{
  "title": "Todos API Overview",
  "annotations": {
    "list": [
      {
        "builtIn": 1,
        "datasource": { "type": "grafana", "uid": "grafana" },
        "enable": true,
        "hide": true,
        "iconColor": "rgba(0, 211, 255, 1)",
        "name": "Annotations & Alerts",
        "target": { "limit": 100, "matchAny": false, "tags": [], "type": "dashboard" },
        "type": "dashboard"
      }
    ]
  },
  "editable": true,
  "fiscalYearStartMonth": 0,
  "graphTooltip": 0,
  "links": [],
  "liveNow": false,
  "panels": [
    {
      "type": "stat",
      "title": "Requests/sec",
      "gridPos": { "h": 6, "w": 8, "x": 0, "y": 0 },
      "fieldConfig": { "defaults": { "unit": "reqps" }, "overrides": [] },
      "options": {
        "reduceOptions": { "calcs": ["lastNotNull"], "fields": "", "values": false },
        "orientation": "horizontal"
      },
      "targets": [
        {
          "expr": "sum(rate(http_server_request_duration_seconds_count[1m]))",
          "legendFormat": "req/s",
          "refId": "A"
        }
      ]
    },
    {
      "type": "timeseries",
      "title": "HTTP Request Duration (p95)",
      "gridPos": { "h": 6, "w": 16, "x": 8, "y": 0 },
      "fieldConfig": { "defaults": { "unit": "s" }, "overrides": [] },
      "options": { "legend": { "displayMode": "list" } },
      "targets": [
        {
          "expr": "histogram_quantile(0.95, sum by (le) (rate(http_server_request_duration_seconds_bucket[5m])))",
          "legendFormat": "p95",
          "refId": "A"
        }
      ]
    },
    {
      "type": "timeseries",
      "title": "Process CPU %",
      "gridPos": { "h": 6, "w": 8, "x": 0, "y": 6 },
      "fieldConfig": { "defaults": { "unit": "percentunit" }, "overrides": [] },
      "targets": [
        {
          "expr": "avg(rate(process_cpu_seconds_total[1m])) * 100",
          "legendFormat": "cpu",
          "refId": "A"
        }
      ]
    },
    {
      "type": "timeseries",
      "title": ".NET GC Collections/sec",
      "gridPos": { "h": 6, "w": 16, "x": 8, "y": 6 },
      "targets": [
        {
          "expr": "rate(process_runtime_dotnet_gc_collections_count_total[1m])",
          "legendFormat": "gc",
          "refId": "A"
        }
      ]
    }
  ],
  "refresh": "10s",
  "schemaVersion": 38,
  "style": "dark",
  "tags": ["todos", "aspnetcore"],
  "templating": { "list": [] },
  "time": { "from": "now-1h", "to": "now" },
  "timezone": ""
}
```

Notes:
- Metric names may vary with exporter versions. If a panel shows no data, open Prometheus at http://localhost:9090 and search for available metrics (e.g., type `http_server` or `process_runtime_dotnet`). Adjust queries accordingly.
- If running on Linux, replace `host.docker.internal` with an IP or run everything in Docker.

## 5) Quick checklist
- ASP.NET running and exposing /metrics
- Prometheus up and targets show `todos-api` UP
- Grafana up with Prometheus datasource OK
- Dashboard panels show data

## Troubleshooting
- Windows Git Bash path conversion: prefix `MSYS_NO_PATHCONV=1` or use `/c/...` style absolute paths in `-v` mounts.
- Port conflicts: change `-p` mappings or app URL.
- TLS/HTTPS dev certs: prefer HTTP scrape in development; if you must use HTTPS, set `scheme: https` and `tls_config.insecure_skip_verify: true` in `prometheus.yml`.

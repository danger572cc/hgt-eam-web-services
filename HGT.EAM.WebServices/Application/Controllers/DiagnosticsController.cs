using HGT.EAM.WebServices.Infrastructure.Architecture.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HGT.EAM.WebServices.Application.Controllers;

/// <summary>
/// Panel interno de diagnóstico. Protegido con la MISMA Basic Auth que los endpoints de la API
/// (cualquier usuario de <c>EAMCredentials</c> entra con sus mismas credenciales).
/// Expone: estado de la conexión con EAM, visor del log de Serilog y métricas de rendimiento.
/// Se excluye de la documentación OpenAPI/Scalar (ApiExplorerSettings.IgnoreApi).
/// </summary>
[Authorize]
[ApiController]
[Route("diagnostics")]
[ApiExplorerSettings(IgnoreApi = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DiagnosticsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DiagnosticsMetrics _metrics;
    private readonly IWebHostEnvironment _env;

    public DiagnosticsController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        DiagnosticsMetrics metrics,
        IWebHostEnvironment env)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _metrics = metrics;
        _env = env;
    }

    /// <summary>Página HTML del panel (autocontenida, sin dependencias externas / CDN).</summary>
    [HttpGet("")]
    public ContentResult Index() => Content(BuildHtml(), "text/html; charset=utf-8");

    /// <summary>
    /// Sonda de conectividad a EAM. Hace un GET al EAMBaseUrl por el mismo camino de red
    /// (mismo proxy del sistema) y devuelve el error REAL sin enmascarar (p. ej. el fallo del proxy).
    /// </summary>
    [HttpGet("eam")]
    public async Task<IActionResult> Eam(CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["EAMBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return Ok(new { ok = false, reachable = false, message = "EAMBaseUrl no está configurado.", checkedAtUtc = DateTime.UtcNow });

        var client = _httpClientFactory.CreateClient("eam-probe");
        client.Timeout = TimeSpan.FromSeconds(10);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            using var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            return Ok(new
            {
                ok = true,               // hubo respuesta HTTP => el transporte hacia EAM funciona
                reachable = true,
                httpStatus = (int)response.StatusCode,
                elapsedMs = stopwatch.ElapsedMilliseconds,
                target = baseUrl,
                proxy = ResolveProxy(baseUrl),
                checkedAtUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Ok(new
            {
                ok = false,
                reachable = false,
                elapsedMs = stopwatch.ElapsedMilliseconds,
                target = baseUrl,
                proxy = ResolveProxy(baseUrl),
                error = FlattenException(ex),   // aquí aparece el "proxy tunnel ... 503" real
                checkedAtUtc = DateTime.UtcNow
            });
        }
    }

    /// <summary>Últimas entradas del archivo rotativo de Serilog (logs/HGT.WebServices*.log).</summary>
    [HttpGet("logs")]
    public IActionResult Logs([FromQuery] int take = 200, [FromQuery] string? level = null)
    {
        var logsDir = Path.Combine(_env.ContentRootPath, "logs");
        if (!Directory.Exists(logsDir))
            return Ok(new { available = false, message = "No existe la carpeta de logs.", dir = logsDir });

        var file = new DirectoryInfo(logsDir)
            .GetFiles("*.log")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (file is null)
            return Ok(new { available = false, message = "No hay archivos .log.", dir = logsDir });

        var entries = ReadTailEntries(file.FullName, Math.Clamp(take, 1, 2000), level);
        return Ok(new { available = true, file = file.Name, dir = logsDir, count = entries.Count, lines = entries });
    }

    /// <summary>Métricas de rendimiento: uptime, memoria, CPU, tráfico y caché.</summary>
    [HttpGet("perf")]
    public IActionResult Perf()
    {
        using var proc = Process.GetCurrentProcess();

        return Ok(new
        {
            app = new
            {
                environment = _env.EnvironmentName,
                machine = Environment.MachineName,
                dotnet = Environment.Version.ToString(),
                startedUtc = _metrics.StartedUtc,
                uptime = _metrics.Uptime.ToString(@"dd\.hh\:mm\:ss")
            },
            process = new
            {
                workingSetMB = Math.Round(proc.WorkingSet64 / 1048576.0, 1),
                gcHeapMB = Math.Round(GC.GetTotalMemory(false) / 1048576.0, 1),
                threads = proc.Threads.Count,
                cpuTotalSec = Math.Round(proc.TotalProcessorTime.TotalSeconds, 1),
                gcGen0 = GC.CollectionCount(0),
                gcGen1 = GC.CollectionCount(1),
                gcGen2 = GC.CollectionCount(2)
            },
            traffic = new
            {
                totalRequests = _metrics.TotalRequests,
                totalErrors = _metrics.TotalErrors,
                averageMs = Math.Round(_metrics.AverageMs, 1),
                requestsPerMin = _metrics.Uptime.TotalMinutes < 0.1
                    ? 0
                    : Math.Round(_metrics.TotalRequests / _metrics.Uptime.TotalMinutes, 1)
            },
            cache = new
            {
                enabled = _configuration.GetValue<bool>("GridCache:Enabled"),
                expirationMinutes = _configuration.GetValue<int>("GridCache:ExpirationMinutes"),
                database = _configuration.GetConnectionString("GridCache")
            }
        });
    }

    // ---------- helpers ----------

    private static string ResolveProxy(string url)
    {
        try
        {
            var uri = new Uri(url);
            var proxy = HttpClient.DefaultProxy;
            if (proxy is null || proxy.IsBypassed(uri))
                return "(directo, sin proxy)";
            return proxy.GetProxy(uri)?.ToString() ?? "(directo, sin proxy)";
        }
        catch
        {
            return "(desconocido)";
        }
    }

    private static string FlattenException(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        Exception? current = ex;
        var depth = 0;
        while (current is not null && depth < 6)
        {
            if (sb.Length > 0) sb.Append("  →  ");
            sb.Append(current.GetType().Name).Append(": ").Append(current.Message);
            current = current.InnerException;
            depth++;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Lee el final del archivo (hasta ~512 KB) y agrupa las líneas en entradas de log
    /// (una entrada por marca de tiempo), para que los stack traces multilínea no se corten.
    /// </summary>
    private static List<string> ReadTailEntries(string path, int take, string? level)
    {
        const long maxBytes = 512 * 1024;
        string content;
        long start;

        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            start = Math.Max(0, fs.Length - maxBytes);
            fs.Seek(start, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            content = reader.ReadToEnd();
        }

        var entries = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var raw in content.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            // Cada entrada empieza con "[yyyy-MM-dd HH:mm:ss..." según el outputTemplate de Serilog.
            var isHeader = line.StartsWith('[') && line.Length > 21 && char.IsDigit(line[1]);
            if (isHeader && current.Length > 0)
            {
                entries.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.Append('\n');
            current.Append(line);
        }
        if (current.Length > 0) entries.Add(current.ToString());

        // Descartar la primera entrada si empezamos a leer a mitad de archivo (probablemente parcial).
        if (start > 0 && entries.Count > 0) entries.RemoveAt(0);

        IEnumerable<string> query = entries;
        if (!string.IsNullOrWhiteSpace(level))
        {
            var token = level.Trim().ToUpperInvariant() switch
            {
                "ERROR" or "ERR" => "ERR",
                "FATAL" or "FTL" => "FTL",
                "WARNING" or "WARN" or "WRN" => "WRN",
                "INFO" or "INFORMATION" or "INF" => "INF",
                _ => level.ToUpperInvariant()
            };
            // El nivel aparece como "[2026-... ERR]" en la cabecera de la entrada.
            query = query.Where(e => e.Contains(token + "]") || (token == "ERR" && e.Contains("FTL]")));
        }

        return query.TakeLast(take).ToList();
    }

    private static string BuildHtml() => """
<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>HGT Grid API — Diagnóstico</title>
<style>
 *{box-sizing:border-box}
 body{margin:0;font-family:'Segoe UI',Arial,sans-serif;background:#f4f6f8;color:#1f2a2e}
 header{background:#222A36;color:#fff;padding:14px 20px;display:flex;align-items:center;gap:10px}
 header strong{font-size:16px}
 header .env{margin-left:auto;font-size:12px;opacity:.85}
 .wrap{max-width:1120px;margin:18px auto;padding:0 16px;display:grid;gap:16px;grid-template-columns:1fr 1fr}
 .card{background:#fff;border:1px solid #e2e7ea;border-radius:10px;padding:16px}
 .card.full{grid-column:1/-1}
 .card h2{font-size:12px;text-transform:uppercase;letter-spacing:.05em;color:#5a6e6b;margin:0 0 12px}
 .badge{display:inline-block;padding:2px 12px;border-radius:12px;font-size:12px;font-weight:700;margin-bottom:10px}
 .ok{background:#e6f4ea;color:#1e7e34}.fail{background:#fdecea;color:#c0392b}
 table{width:100%;border-collapse:collapse;font-size:13px}
 td{padding:4px 0;vertical-align:top}td.k{color:#6a7a77;width:40%}
 pre{background:#0f1720;color:#d6e2e0;padding:12px;border-radius:8px;overflow:auto;max-height:360px;font-size:12px;line-height:1.45;white-space:pre-wrap;word-break:break-word;margin:0}
 .er{color:#ff8a80}
 .controls{display:flex;gap:8px;align-items:center;margin-bottom:10px;font-size:13px;flex-wrap:wrap}
 button,select{font:inherit;padding:5px 10px;border:1px solid #cbd5d3;border-radius:6px;background:#fff;cursor:pointer}
 .muted{color:#8a9794;font-size:12px}
</style>
</head>
<body>
<header><strong>HGT Grid API</strong><span>· Panel de diagnóstico</span><span class="env" id="env">—</span></header>
<div class="wrap">
 <div class="card"><h2>Conexión con EAM</h2><div id="eam">Cargando…</div></div>
 <div class="card"><h2>Rendimiento</h2><div id="perf">Cargando…</div></div>
 <div class="card full">
  <h2>Registro (Serilog)</h2>
  <div class="controls">
   <label>Nivel:</label>
   <select id="level"><option value="">Todos</option><option value="error">Errores</option><option value="warning">Advertencias</option></select>
   <label>Entradas:</label>
   <select id="take"><option>100</option><option selected>200</option><option>500</option></select>
   <button onclick="loadLogs()">Refrescar</button>
   <span class="muted" id="logmeta"></span>
  </div>
  <pre id="logs">Cargando…</pre>
 </div>
</div>
<script>
const $=id=>document.getElementById(id);
const esc=s=>(s==null?'':s.toString()).replace(/[&<>]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;'}[c]));
const row=(k,v)=>'<tr><td class="k">'+k+'</td><td>'+v+'</td></tr>';
async function j(u){const r=await fetch(u,{headers:{'Accept':'application/json'}});return r.json();}
async function loadEam(){
 try{
  const d=await j('/diagnostics/eam');
  let h=(d.ok?'<span class="badge ok">CONECTADO</span>':'<span class="badge fail">FALLO</span>')+'<table>';
  h+=row('Destino',esc(d.target));
  if(d.httpStatus)h+=row('HTTP',d.httpStatus);
  h+=row('Proxy',esc(d.proxy||'—'));
  h+=row('Tiempo',(d.elapsedMs!=null?d.elapsedMs:'—')+' ms');
  if(d.error)h+=row('Error real','<span class="er">'+esc(d.error)+'</span>');
  if(d.message)h+=row('Nota',esc(d.message));
  h+=row('Verificado',new Date(d.checkedAtUtc).toLocaleString());
  h+='</table>';$('eam').innerHTML=h;
 }catch(e){$('eam').innerHTML='<span class="badge fail">ERROR</span> '+esc(e.message);}
}
async function loadPerf(){
 try{
  const d=await j('/diagnostics/perf');
  $('env').textContent=d.app.environment+' · '+d.app.machine;
  let h='<table>';
  h+=row('Ambiente',esc(d.app.environment));
  h+=row('Uptime',esc(d.app.uptime));
  h+=row('Requests',d.traffic.totalRequests+' ('+d.traffic.requestsPerMin+'/min)');
  h+=row('Errores 5xx',d.traffic.totalErrors);
  h+=row('Promedio',d.traffic.averageMs+' ms');
  h+=row('Memoria',d.process.workingSetMB+' MB · heap '+d.process.gcHeapMB+' MB');
  h+=row('Hilos',d.process.threads);
  h+=row('CPU total',d.process.cpuTotalSec+' s');
  h+=row('Caché',(d.cache.enabled?'ON':'OFF')+' · exp '+d.cache.expirationMinutes+' min');
  h+=row('.NET',esc(d.app.dotnet));
  h+='</table>';$('perf').innerHTML=h;
 }catch(e){$('perf').innerHTML='<span class="er">'+esc(e.message)+'</span>';}
}
async function loadLogs(){
 try{
  const d=await j('/diagnostics/logs?take='+$('take').value+'&level='+encodeURIComponent($('level').value));
  if(!d.available){$('logs').textContent=d.message+' ('+(d.dir||'')+')';$('logmeta').textContent='';return;}
  $('logmeta').textContent=d.file+' · '+d.count+' entradas';
  $('logs').innerHTML=d.lines.map(esc).join('\n')||'(sin entradas)';
 }catch(e){$('logs').innerHTML='<span class="er">'+esc(e.message)+'</span>';}
}
loadEam();loadPerf();loadLogs();
setInterval(loadEam,10000);setInterval(loadPerf,10000);setInterval(loadLogs,20000);
</script>
</body>
</html>
""";
}

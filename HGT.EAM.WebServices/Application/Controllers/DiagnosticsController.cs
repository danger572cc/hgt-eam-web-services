using HGT.EAM.WebServices.Infrastructure.Architecture.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace HGT.EAM.WebServices.Application.Controllers;

/// <summary>
/// Panel interno de diagnóstico. Protegido con la MISMA Basic Auth que los endpoints de la API
/// (cualquier usuario de <c>EAMCredentials</c> entra con sus mismas credenciales).
/// Expone: estado de la conexión con EAM, visor del log (archivo + tabla SQL) y métricas de rendimiento.
/// La página (HTML físico y editable) vive en <c>Diagnostics/index.html</c>. Se excluye de OpenAPI/Scalar.
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

    /// <summary>
    /// Sirve la página del panel desde un HTML físico y editable (<c>Diagnostics/index.html</c> en el
    /// directorio de la app). Al estar detrás de <c>[Authorize]</c> queda protegido y NO se expone por
    /// archivos estáticos. Editable en caliente (se relee del disco en cada petición).
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        var path = Path.Combine(_env.ContentRootPath, "Diagnostics", "index.html");
        if (!System.IO.File.Exists(path))
            return Content($"<h1>Panel de diagnóstico</h1><p>No se encontró la página en <code>{path}</code>.</p>",
                           "text/html; charset=utf-8");
        return PhysicalFile(path, "text/html; charset=utf-8");
    }

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
        client.Timeout = TimeSpan.FromMinutes(5);

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

    /// <summary>
    /// Sonda combinada: Network Ping + SOAP Auth Ping a la organización configurada.
    /// </summary>
    [HttpGet("eam-auth")]
    public async Task<IActionResult> EamAuth(CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["EAMBaseUrl"];
        // Organización y credenciales del USUARIO AUTENTICADO (claims puestos por la Basic Auth en
        // AuthorizationExtensions), NO de un índice fijo: el ping valida contra la organización con
        // la que se autenticó quien llama al panel.
        var user = User?.Identity?.Name ?? "";
        var org = User?.FindFirst("Organization")?.Value ?? "(sin organización)";

        if (string.IsNullOrWhiteSpace(baseUrl))
            return Ok(new { ok = false, organization = org, error = "EAMBaseUrl no configurado." });

        var stopwatch = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient("eam-probe");
        client.Timeout = TimeSpan.FromSeconds(15);
        
        bool networkOk = false;
        long networkMs = 0;
        string networkError = "";
        
        // 1. Network Ping
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            using var res = await client.SendAsync(req, cancellationToken);
            networkOk = true;
            networkMs = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            networkError = FlattenException(ex);
        }

        // 2. Auth Ping (SOAP WSSecurity)
        bool authOk = false;
        long authMs = 0;
        string authError = "";
        
        if (networkOk)
        {
            var swAuth = Stopwatch.StartNew();
            try
            {
                var pass = User?.FindFirst("Password")?.Value ?? "";
                string soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Header>
    <Security xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
      <UsernameToken>
        <Username>{user}</Username>
        <Password>{pass}</Password>
      </UsernameToken>
    </Security>
    <Organization xmlns=""http://schemas.datastream.net/MP_functions"">{org}</Organization>
  </soap:Header>
  <soap:Body><Ping/></soap:Body>
</soap:Envelope>";

                using var authReq = new HttpRequestMessage(HttpMethod.Post, baseUrl);
                authReq.Content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml");
                authReq.Headers.Add("SOAPAction", "\"\"");
                
                using var authRes = await client.SendAsync(authReq, cancellationToken);
                var content = await authRes.Content.ReadAsStringAsync(cancellationToken);
                swAuth.Stop();
                authMs = swAuth.ElapsedMilliseconds;

                if (content.Contains("Invalid user", StringComparison.OrdinalIgnoreCase) || 
                    content.Contains("Invalid password", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("authentication", StringComparison.OrdinalIgnoreCase))
                {
                    authOk = false;
                    authError = "Fallo de autenticación en EAM (Credenciales inválidas).";
                }
                else
                {
                    authOk = true; // El servidor aceptó la llamada (aunque devuelva fault por <Ping/>)
                    if (!authRes.IsSuccessStatusCode)
                    {
                        var match = Regex.Match(content, @"<faultstring>(.*?)</faultstring>", RegexOptions.IgnoreCase);
                        if (match.Success) authError = match.Groups[1].Value; 
                    }
                }
            }
            catch (Exception ex)
            {
                authError = FlattenException(ex);
            }
        }
        stopwatch.Stop();
        
        return Ok(new
        {
            ok = networkOk && authOk,
            organization = org,
            user = user,
            network = new { ok = networkOk, elapsedMs = networkMs, error = networkError },
            auth = new { ok = authOk, elapsedMs = authMs, error = authError },
            totalElapsedMs = stopwatch.ElapsedMilliseconds,
            checkedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Visor de log. <paramref name="source"/> = "file" (archivo rotativo de Serilog) o "db"
    /// (la tabla donde escribe el sink MSSqlServer, p. ej. U5HGTEAMWEB). Devuelve entradas
    /// estructuradas { ts, level, source, message, detail }.
    /// </summary>
    [HttpGet("logs")]
    public async Task<IActionResult> Logs(
        [FromQuery] int take = 200,
        [FromQuery] string? level = null,
        [FromQuery] string source = "file",
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 2000);
        string? authenticatedUser = User?.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

        if (string.Equals(source, "db", StringComparison.OrdinalIgnoreCase))
            return await ReadDbLogsAsync(take, level, authenticatedUser, cancellationToken);
        return ReadFileLogs(take, level, authenticatedUser);
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

    /// <summary>
    /// Salud del archivo SQLite del caché de grillas. Responde a "¿el error de la API viene del
    /// archivo SQLite?": ruta REAL que abre SQLite, existencia, tamaño, última escritura, integridad
    /// (<c>PRAGMA quick_check</c>), modo de journal, espera ante lock, tablas/filas cacheadas y el
    /// error REAL si algo falla. La sonda abre en SOLO LECTURA: nunca crea el archivo si la ruta está
    /// mal configurada ni toma locks de escritura sobre el caché en uso.
    /// </summary>
    [HttpGet("sqlite")]
    public async Task<IActionResult> Sqlite(CancellationToken cancellationToken)
    {
        var rawConnectionString = _configuration.GetConnectionString("GridCache") ?? "Data Source=gridcache.db";

        string dataSource;
        int busyTimeoutSeconds;
        SqliteOpenMode mode;
        try
        {
            var parsed = new SqliteConnectionStringBuilder(rawConnectionString);
            dataSource = parsed.DataSource;
            busyTimeoutSeconds = parsed.DefaultTimeout;
            mode = parsed.Mode;
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                ok = false,
                stage = "cadena de conexión",
                error = FlattenException(ex),
                checkedAtUtc = DateTime.UtcNow
            });
        }

        // Caché en memoria: no hay archivo que revisar.
        if (string.IsNullOrWhiteSpace(dataSource)
            || mode == SqliteOpenMode.Memory
            || string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                ok = true,
                fileBased = false,
                dataSource,
                mode = mode.ToString(),
                message = "El caché no usa un archivo en disco; no hay archivo que revisar.",
                checkedAtUtc = DateTime.UtcNow
            });
        }

        // SQLite resuelve las rutas relativas contra el directorio de TRABAJO del proceso, que bajo
        // IIS o como servicio puede NO ser el ContentRoot: se reportan ambos para poder compararlos.
        var currentDirectory = Directory.GetCurrentDirectory();
        string path;
        try
        {
            path = Path.GetFullPath(dataSource, currentDirectory);
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                ok = false,
                stage = "ruta",
                dataSource,
                error = FlattenException(ex),
                checkedAtUtc = DateTime.UtcNow
            });
        }

        var file = new FileInfo(path);
        var exists = file.Exists;
        var directoryExists = file.Directory?.Exists == true;

        // Escritura: SQLite necesita escribir el .db Y crear el journal/-wal en la MISMA carpeta; una
        // carpeta de solo lectura rompe las escrituras aunque el archivo sí sea escribible.
        var fileWritable = exists && CanWriteFile(path);
        var directoryWritable = directoryExists && CanWriteDirectory(file.Directory!.FullName);

        var canOpen = false;
        string? openedPath = null;
        string? integrity = null;
        string? journalMode = null;
        string? stage = null;
        string? error = null;
        var tableStats = new List<(string Name, long? Rows)>();

        if (!exists)
        {
            error = directoryExists
                ? "El archivo no existe en la ruta resuelta."
                : "No existe la carpeta que debería contener el archivo.";
        }
        else
        {
            try
            {
                stage = "abrir";
                var probeConnectionString = new SqliteConnectionStringBuilder(rawConnectionString)
                {
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString();

                await using var connection = new SqliteConnection(probeConnectionString);
                await connection.OpenAsync(cancellationToken);
                canOpen = true;

                // Ruta que SQLite abrió realmente: fuente de verdad frente a rutas relativas.
                openedPath = await ScalarAsync(connection, "SELECT file FROM pragma_database_list WHERE name = 'main';", cancellationToken);
                journalMode = await ScalarAsync(connection, "PRAGMA journal_mode;", cancellationToken);

                stage = "quick_check";
                integrity = await ScalarAsync(connection, "PRAGMA quick_check;", cancellationToken);

                stage = "tablas";
                foreach (var table in await ReadTableNamesAsync(connection, cancellationToken))
                {
                    var rows = await ScalarAsync(connection, $"SELECT COUNT(*) FROM \"{table.Replace("\"", "\"\"")}\";", cancellationToken);
                    tableStats.Add((table, long.TryParse(rows, out var count) ? count : null));
                }

                stage = null;
            }
            catch (Exception ex)
            {
                error = FlattenException(ex);   // aquí aparece el error REAL de SQLite
            }
        }

        return Ok(new
        {
            ok = exists
                 && canOpen
                 && string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase)
                 && fileWritable
                 && directoryWritable,
            fileBased = true,
            path = openedPath ?? path,
            exists,
            sizeBytes = exists ? file.Length : (long?)null,
            sizeMB = exists ? Math.Round(file.Length / 1048576.0, 2) : (double?)null,
            lastWriteUtc = exists ? file.LastWriteTimeUtc : (DateTime?)null,
            canOpen,
            writable = fileWritable,
            directoryWritable,
            integrity,
            journalMode,
            busyTimeoutSeconds,
            mode = mode.ToString(),
            cachedRows = tableStats.Sum(t => t.Rows ?? 0),
            tables = tableStats.Select(t => new { name = t.Name, rows = t.Rows }),
            contentRoot = _env.ContentRootPath,
            currentDirectory,
            stage,
            error,
            checkedAtUtc = DateTime.UtcNow
        });
    }

    // ---------- caché SQLite ----------

    /// <summary>¿El proceso puede abrir el archivo para escritura? (permisos NTFS / atributo de solo lectura).</summary>
    private static bool CanWriteFile(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>¿Se puede crear un archivo en la carpeta? SQLite lo necesita para el journal/-wal.</summary>
    private static bool CanWriteDirectory(string directory)
    {
        var probe = Path.Combine(directory, $".diag-write-{Guid.NewGuid():N}.tmp");
        try
        {
            using var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> ScalarAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null || value == DBNull.Value ? null : value.ToString();
    }

    private static async Task<List<string>> ReadTableNamesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var names = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            names.Add(reader.GetString(0));
        return names;
    }

    // ---------- fuente: archivo ----------

    private IActionResult ReadFileLogs(int take, string? level, string? userFilter)
    {
        var logsDir = Path.Combine(_env.ContentRootPath, "logs");
        if (!Directory.Exists(logsDir))
            return Ok(new { available = false, source = "file", message = "No existe la carpeta de logs.", dir = logsDir, entries = Array.Empty<object>() });

        var file = new DirectoryInfo(logsDir)
            .GetFiles("*.log")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (file is null)
            return Ok(new { available = false, source = "file", message = "No hay archivos .log.", dir = logsDir, entries = Array.Empty<object>() });

        var entries = ReadFileEntries(file.FullName, take, level, userFilter);
        return Ok(new { available = true, source = "file", file = file.Name, dir = logsDir, count = entries.Count, entries });
    }

    /// <summary>
    /// Lee el final del archivo (~512 KB) y agrupa las líneas en entradas por marca de tiempo,
    /// tolerando los formatos de esta API (<c>[fecha NIVEL] [Origen] msg</c>) y el de otros
    /// servicios (<c>fecha [NIVEL] msg {json}</c>). Los stack traces multilínea quedan en <c>detail</c>.
    /// </summary>
    private static List<object> ReadFileEntries(string path, int take, string? level, string? userFilter)
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

        var raw = new List<List<string>>();
        List<string>? cur = null;
        foreach (var line0 in content.Split('\n'))
        {
            var line = line0.TrimEnd('\r');
            if (HeaderRegex.IsMatch(line))
            {
                cur = new List<string> { line };
                raw.Add(cur);
            }
            else if (cur != null)
            {
                cur.Add(line);
            }
        }
        if (start > 0 && raw.Count > 0) raw.RemoveAt(0); // primera entrada posiblemente parcial

        var wanted = string.IsNullOrWhiteSpace(level) ? null : ShortLevel(level);
        var result = new List<object>();
        foreach (var entry in raw)
        {
            var header = entry[0];
            var detail = entry.Count > 1 ? string.Join("\n", entry.Skip(1)).TrimEnd() : "";
            var parsed = ParseHeader(header);
            if (wanted != null && parsed.level != wanted) continue;
            
            // Filtro por usuario autenticado
            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                bool hasUser = entry.Any(l => l.Contains(userFilter, StringComparison.OrdinalIgnoreCase));
                if (!hasUser) continue;
            }
            
            result.Add(new { ts = parsed.ts, level = parsed.level, source = parsed.source, message = parsed.message, detail });
        }
        return result.Skip(Math.Max(0, result.Count - take)).ToList();
    }

    private static readonly Regex HeaderRegex =
        new(@"^\[?\s*\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}", RegexOptions.Compiled);
    private static readonly Regex TsRegex =
        new(@"\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:\s*[+-]\d{2}:\d{2})?", RegexOptions.Compiled);
    private static readonly Regex LevelRegex =
        new(@"\b(VRB|DBG|INF|WRN|ERR|FTL|Verbose|Debug|Information|Warning|Error|Fatal)\b", RegexOptions.Compiled);
    private static readonly Regex SourceRegex =
        new(@"\]\s*\[([\w.]+)\]", RegexOptions.Compiled);

    private static (string ts, string level, string source, string message) ParseHeader(string header)
    {
        var tsm = TsRegex.Match(header);
        var ts = tsm.Success ? tsm.Value.Trim() : "";
        var lvlm = LevelRegex.Match(header.Length > 60 ? header[..60] : header);
        var level = lvlm.Success ? ShortLevel(lvlm.Value) : "INF";

        string source = "";
        var sm = SourceRegex.Match(header);
        if (sm.Success) source = sm.Groups[1].Value;

        var msg = header;
        if (ts.Length > 0) { var i = msg.IndexOf(ts, StringComparison.Ordinal); if (i >= 0) msg = msg.Remove(i, ts.Length); }
        if (lvlm.Success) { var i = msg.IndexOf(lvlm.Value, StringComparison.Ordinal); if (i >= 0) msg = msg.Remove(i, lvlm.Value.Length); }
        msg = msg.TrimStart(' ', '[', ']', '-', ':', '\t');
        msg = Regex.Replace(msg, @"^\[[\w.]+\]\s*", "");     // quita [Origen] inicial
        var j = msg.IndexOf(" {\"", StringComparison.Ordinal); // quita contexto JSON final
        if (j > 0 && msg.TrimEnd().EndsWith("}", StringComparison.Ordinal))
        {
            if (source.Length == 0)
            {
                var scm = Regex.Match(msg[j..], "\"SourceContext\"\\s*:\\s*\"([^\"]+)\"");
                if (scm.Success) source = scm.Groups[1].Value;
            }
            msg = msg[..j];
        }
        return (ts, level, source, msg.Trim());
    }

    // ---------- fuente: base de datos (tabla del sink MSSqlServer) ----------

    private async Task<IActionResult> ReadDbLogsAsync(int take, string? level, string? userFilter, CancellationToken ct)
    {
        var (conn, table) = ResolveLogDb();
        if (string.IsNullOrWhiteSpace(conn))
            return Ok(new { available = false, source = "db", message = "No se encontró la cadena de conexión del sink MSSqlServer.", entries = Array.Empty<object>() });

        var safeTable = SanitizeTable(table);
        try
        {
            var entries = new List<object>();
            await using var cn = new SqlConnection(conn);
            await cn.OpenAsync(ct);

            var conditions = new List<string>();
            if (!string.IsNullOrWhiteSpace(level)) conditions.Add("[Level] = @lvl");
            if (!string.IsNullOrWhiteSpace(userFilter)) conditions.Add("[CurrentUser] = @usr");
            
            var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
            var sql = $"SELECT TOP (@n) [TimeStamp],[Level],[Message],[Exception],[RequestPath],[CurrentUser] FROM {safeTable}{where} ORDER BY [Id] DESC";

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@n", take);
            if (!string.IsNullOrWhiteSpace(level)) cmd.Parameters.AddWithValue("@lvl", FullLevel(level));
            if (!string.IsNullOrWhiteSpace(userFilter)) cmd.Parameters.AddWithValue("@usr", userFilter);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var ts = r["TimeStamp"] is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss.fff") : (r["TimeStamp"]?.ToString() ?? "");
                var lvl = ShortLevel(r["Level"]?.ToString());
                var msg = r["Message"]?.ToString() ?? "";
                var exc = r["Exception"] as string ?? "";
                var src = r["RequestPath"] as string;
                var usr = r["CurrentUser"] as string;
                entries.Add(new { ts, level = lvl, source = string.IsNullOrWhiteSpace(src) ? (usr ?? "") : src, message = msg, detail = exc });
            }
            entries.Reverse(); // de más antiguo a más reciente, como el archivo
            return Ok(new { available = true, source = "db", table = safeTable, count = entries.Count, entries });
        }
        catch (Exception ex)
        {
            return Ok(new { available = false, source = "db", table = safeTable, error = FlattenException(ex), entries = Array.Empty<object>() });
        }
    }

    /// <summary>Resuelve conexión + tabla del log en BD: primero config explícita, luego el sink MSSqlServer de Serilog.</summary>
    private (string? conn, string table) ResolveLogDb()
    {
        var conn = _configuration["Diagnostics:LogConnectionString"];
        var table = _configuration["Diagnostics:LogTable"];
        if (!string.IsNullOrWhiteSpace(conn))
            return (conn, string.IsNullOrWhiteSpace(table) ? "U5HGTEAMWEB" : table!);

        var (c, t) = FindMsSqlSink(_configuration.GetSection("Serilog"));
        return (c, string.IsNullOrWhiteSpace(t) ? (string.IsNullOrWhiteSpace(table) ? "U5HGTEAMWEB" : table!) : t!);
    }

    private static (string? conn, string? table) FindMsSqlSink(IConfigurationSection section)
    {
        foreach (var child in section.GetChildren())
        {
            if (string.Equals(child["Name"], "MSSqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var args = child.GetSection("Args");
                return (args["connectionString"], args["tableName"]);
            }
            var found = FindMsSqlSink(child);
            if (found.conn != null) return found;
        }
        return (null, null);
    }

    private static string SanitizeTable(string table)
    {
        var name = new string((table ?? "U5HGTEAMWEB").Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (name.Length == 0) name = "U5HGTEAMWEB";
        return $"[{name}]";
    }

    // ---------- helpers comunes ----------

    private static string ShortLevel(string? lvl) => (lvl ?? "").Trim().ToUpperInvariant() switch
    {
        "INFORMATION" or "INF" => "INF",
        "WARNING" or "WARN" or "WRN" => "WRN",
        "ERROR" or "ERR" => "ERR",
        "FATAL" or "FTL" => "FTL",
        "DEBUG" or "DBG" => "DBG",
        "VERBOSE" or "VRB" => "VRB",
        var s => s.Length >= 3 ? s[..3] : s
    };

    private static string FullLevel(string level) => ShortLevel(level) switch
    {
        "INF" => "Information",
        "WRN" => "Warning",
        "ERR" => "Error",
        "FTL" => "Fatal",
        "DBG" => "Debug",
        "VRB" => "Verbose",
        _ => level
    };

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
}

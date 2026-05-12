using Spectre.Console;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;

// =================== Configuração ===================
// Iterações elevadas deixam o trabalho realmente pesado (CPU-bound).
const int PBKDF2_ITERATIONS = 50_000;
const int HASH_BYTES = 32; // 32 = 256 bits
const string CSV_URL = "https://www.gov.br/receitafederal/dados/municipios.csv";
const string OUT_DIR_NAME = "mun_hash_por_uf";

string FormatTempo(long ms)
{
    var ts = TimeSpan.FromMilliseconds(ms);
    return $"{ts.Minutes}m {ts.Seconds}s {ts.Milliseconds}ms";
}

var swTotal = Stopwatch.StartNew();

string baseDir = Directory.GetCurrentDirectory();
string tempCsvPath = Path.Combine(baseDir, "municipios.csv");
string outRoot = Path.Combine(baseDir, OUT_DIR_NAME);

Console.WriteLine("Baixando CSV de municípios (Receita Federal) ...");
using (var wc = new WebClient())
{
    wc.Encoding = Encoding.UTF8; // ajuste para ISO-8859-1 se necessário
    wc.DownloadFile(CSV_URL, tempCsvPath);
}

var swRead = Stopwatch.StartNew();

Console.WriteLine("Lendo e parseando o CSV ...");
var linhas = File.ReadAllLines(tempCsvPath, Encoding.UTF8);
if (linhas.Length == 0)
{
    Console.WriteLine("Arquivo CSV vazio.");
    return;
}

int startIndex = 0;
if (linhas[0].IndexOf("IBGE", StringComparison.OrdinalIgnoreCase) >= 0 ||
    linhas[0].IndexOf("UF", StringComparison.OrdinalIgnoreCase) >= 0)
{
    startIndex = 1; // pula cabeçalho
}

var municipios = new List<Municipio>(linhas.Length - startIndex);

for (int i = startIndex; i < linhas.Length; i++)
{
    var linha = (linhas[i] ?? "").Trim();
    if (string.IsNullOrWhiteSpace(linha)) continue;

    var parts = linha.Split(';');
    if (parts.Length < 5) continue;

    municipios.Add(new Municipio
    {
        Tom = Util.San(parts[0]),
        Ibge = Util.San(parts[1]),
        NomeTom = Util.San(parts[2]),
        NomeIbge = Util.San(parts[3]),
        Uf = Util.San(parts[4]).ToUpperInvariant()
    });
}

Console.WriteLine($"Registros lidos: {municipios.Count}");

// Grupo por UF
var porUf = new Dictionary<string, List<Municipio>>(StringComparer.OrdinalIgnoreCase);
foreach (var m in municipios)
{
    if (!porUf.ContainsKey(m.Uf))
        porUf[m.Uf] = new List<Municipio>();
    porUf[m.Uf].Add(m);
}

// Ordena as UFs alfabeticamente e ignora a UF "EX"
var ufsOrdenadas = porUf.Keys
    .Where(uf => !string.Equals(uf, "EX", StringComparison.OrdinalIgnoreCase))
    .OrderBy(uf => uf, StringComparer.OrdinalIgnoreCase)
    .ToList();

// Gera saída
Directory.CreateDirectory(outRoot);
Console.WriteLine($"Calculando hash por município e gerando arquivos por UF, ProcessorCount: {Environment.ProcessorCount}");

var queue = new QueueCreator(Environment.ProcessorCount);
var ufResults = new ConcurrentDictionary<string, ConcurrentBag<(Municipio m, string hash)>>();
var allTasks = new List<Task>();

// Novo console com progresso dinamico
await AnsiConsole.Progress().Columns(new ProgressColumn[]
    {
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new SpinnerColumn(),
    })
    .StartAsync(async ctx =>
    {
        var progressMap = new Dictionary<string, ProgressTask>();
        foreach (var uf in ufsOrdenadas)
        {
            var listaUf = porUf[uf];

            listaUf.Sort((a, b) =>
                string.Compare(
                    a.NomePreferido,
                    b.NomePreferido,
                    StringComparison.OrdinalIgnoreCase));

            ufResults[uf] = new ConcurrentBag<(Municipio, string)>();

            progressMap[uf] = ctx.AddTask(
                $"\t[white]{uf}[/] [yellow]000 ms[/]",
                maxValue: listaUf.Count);

            foreach (var m in listaUf)
            {
                var tcs = new TaskCompletionSource();
                allTasks.Add(tcs.Task);

                queue.AddToQueue(async () =>
                {
                    try {
                        string password = m.ToConcatenatedString();
                        byte[] salt = Util.BuildSalt(m.Ibge);

                        string hashHex = Util.DeriveHashHex(
                            password,
                            salt,
                            PBKDF2_ITERATIONS,
                            HASH_BYTES);

                        ufResults[uf].Add((m, hashHex));
                        progressMap[uf].Increment(1);

                    }
                    finally {
                        tcs.SetResult();

                        if (progressMap[uf].Value%25==0 || progressMap[uf].Value==progressMap[uf].MaxValue)
                        {
                            progressMap[uf].Description =
                                $"\t[white]{uf}[/] [blue]{progressMap[uf].ElapsedTime?.TotalMilliseconds:F0} ms[/]";
                        }
                    }
                    await Task.CompletedTask;
                });
            }
        }
        await Task.WhenAll(allTasks);
    });

queue.Dispose();

swTotal.Stop();
swRead.Stop();
Console.WriteLine();
Console.WriteLine("===== RESUMO =====");
Console.WriteLine($"UFs geradas: {ufsOrdenadas.Count} Com {PBKDF2_ITERATIONS} Iterações");
Console.WriteLine($"Pasta de saída: {outRoot}");

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine($"\nTempo total: \t{FormatTempo(swTotal.ElapsedMilliseconds)} ({swTotal.Elapsed})");

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\nTempo leitura: \t{FormatTempo(swRead.ElapsedMilliseconds)} ({swRead.Elapsed})");

Console.ForegroundColor = ConsoleColor.Gray;
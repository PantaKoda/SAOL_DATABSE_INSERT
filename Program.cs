using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SAOL_DATABSE_INSERT.Data;
using SAOL_DATABSE_INSERT.JsonModels;
using SAOL_DATABSE_INSERT.Models; 
using System.Text.Json;
using EFCore.BulkExtensions; 
using HtmlAgilityPack; 


public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            { 
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                var connectionString = hostContext.Configuration.GetConnectionString("SaolDataDb");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.Error.WriteLine("FATAL ERROR: Connection string 'SaolDataDb' not found in appsettings.json or is empty.");
                    Environment.Exit(1); 
                }

                services.AddDbContext<saol_dataContext>(options =>
                    options.UseNpgsql(connectionString)); 
                services.AddTransient<DataLoaderService>(); 
            })
            .Build();

        var dataLoader = host.Services.GetRequiredService<DataLoaderService>();
        var config = host.Services.GetRequiredService<IConfiguration>();

        try
        {
            Console.WriteLine("Starting data loading process...");

            string adjectivesPath = config.GetValue<string>("DataFilePaths:Adjectives") ?? string.Empty;
            string verbsPath = config.GetValue<string>("DataFilePaths:Verbs") ?? string.Empty;
            string nounsPath = config.GetValue<string>("DataFilePaths:Nouns") ?? string.Empty;
            string adverbsPath = config.GetValue<string>("DataFilePaths:Adverbs") ?? string.Empty;

            if (string.IsNullOrEmpty(adjectivesPath) ||
                string.IsNullOrEmpty(verbsPath) ||
                string.IsNullOrEmpty(nounsPath) ||
                string.IsNullOrEmpty(adverbsPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: One or more data file paths are missing or empty in appsettings.json under 'DataFilePaths'.");
                Console.ResetColor();
                return; 
            }

            Console.WriteLine($"Adjectives path: {adjectivesPath}");
            Console.WriteLine($"Verbs path: {verbsPath}");
            Console.WriteLine($"Nouns path: {nounsPath}");
            Console.WriteLine($"Adverbs path: {adverbsPath}");

            await dataLoader.LoadAllDataAsync(adjectivesPath, verbsPath, nounsPath, adverbsPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Data loading process completed successfully!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred during the data loading process: {ex.Message}");
            Console.WriteLine("--- Full Stack Trace ---");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
            Environment.ExitCode = 1; 
        }
    }
}

public class DataLoaderService
{
    private readonly saol_dataContext _context;
    private readonly HtmlDocument _htmlDoc; 

    public DataLoaderService(saol_dataContext context)
    {
        _context = context;
        _htmlDoc = new HtmlDocument(); 
        HtmlNode.ElementsFlags.Remove("form"); 
    }

    private string CleanString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }
        _htmlDoc.LoadHtml(input);
        string dehtml = _htmlDoc.DocumentNode.InnerText; 

        
        return dehtml.Replace("\u00A0", " ").Trim();
    }

    private async Task<List<TJsonModel>?> LoadJsonFileAsync<TJsonModel>(string filePath)
    {
        Console.WriteLine($"Attempting to load JSON from: {filePath}");
        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: File not found at {filePath}");
            Console.ResetColor();
            return null; 
        }
        try
        {
            var jsonString = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: File at {filePath} is empty or contains only whitespace.");
                Console.ResetColor();
                return new List<TJsonModel>(); 
            }
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            return JsonSerializer.Deserialize<List<TJsonModel>>(jsonString, options);
        }
        catch (JsonException jsonEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error deserializing JSON from {filePath}: {jsonEx.Message} (Path: {jsonEx.Path}, Line: {jsonEx.LineNumber}, BytePos: {jsonEx.BytePositionInLine})");
            Console.ResetColor();
            throw; 
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
            Console.ResetColor();
            throw; 
        }
    }

    public async Task LoadAllDataAsync(string adjectivesPath, string verbsPath, string nounsPath, string adverbsPath)
    {
        var bulkConfig = new BulkConfig
        {
            SetOutputIdentity = true,
            PreserveInsertOrder = true,
            BatchSize = 5000, 
        };

        using var transaction = await _context.Database.BeginTransactionAsync();
        Console.WriteLine("Database transaction started.");
        try
        {
            await LoadAdjectivesAsync(adjectivesPath, bulkConfig);
            await LoadVerbsAsync(verbsPath, bulkConfig);
            await LoadNounsAsync(nounsPath, bulkConfig);
            await LoadAdverbsAsync(adverbsPath, bulkConfig);

            await transaction.CommitAsync();
            Console.WriteLine("Database transaction committed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during bulk loading, attempting to roll back transaction...");
            await transaction.RollbackAsync();
            Console.WriteLine("Database transaction rolled back.");
            throw; 
        }
    }

    private async Task LoadAdjectivesAsync(string filePath, BulkConfig entryBulkConfig)
    {
        var jsonAdjectives = await LoadJsonFileAsync<JsonAdjective>(filePath);
        if (jsonAdjectives == null || !jsonAdjectives.Any())
        {
            Console.WriteLine("No adjectives to load or file error for adjectives.");
            return;
        }
        Console.WriteLine($"Loaded {jsonAdjectives.Count} adjectives from JSON. Processing...");

        var adjectiveEntries = new List<adjective_entry>();
        var entriesWithOriginals = new List<(adjective_entry Entry, JsonAdjective Original)>();

        foreach (var jsonAdj in jsonAdjectives)
        {
            if (jsonAdj == null || string.IsNullOrWhiteSpace(jsonAdj.ClassName))
            {
                // Console.WriteLine("Skipping adjective with missing class name.");
                continue;
            }
            var entry = new adjective_entry { _class = CleanString(jsonAdj.ClassName) };
            adjectiveEntries.Add(entry);
            entriesWithOriginals.Add((entry, jsonAdj));
        }

        if (adjectiveEntries.Any())
        {
            await _context.BulkInsertAsync(adjectiveEntries, entryBulkConfig);
            Console.WriteLine($"Bulk inserted {adjectiveEntries.Count} adjective entries. IDs should be populated.");
        }
        else
        {
            Console.WriteLine("No valid adjective entries to insert.");
        }

        var adjectiveForms = new List<adjective_form>();
        foreach (var (entry, originalJson) in entriesWithOriginals)
        {
            if (entry.id == 0 && adjectiveEntries.Any()) 
            {
                Console.WriteLine($"Warning: Adjective entry for class '{entry._class}' did not get an ID after bulk insert. Skipping its forms.");
                continue;
            }
            if (originalJson.Forms != null)
            {
                foreach (var kvp in originalJson.Forms)
                {
                    string degree = CleanString(kvp.Key);
                    if (string.IsNullOrEmpty(degree)) continue; 

                    foreach (var formVal in kvp.Value)
                    {
                        string cleanedForm = CleanString(formVal);
                        if (string.IsNullOrEmpty(cleanedForm)) continue; 

                        adjectiveForms.Add(new adjective_form
                        {
                            entry_id = entry.id,
                            degree = degree,
                            form = cleanedForm
                        });
                    }
                }
            }
        }

        if (adjectiveForms.Any())
        {
            await _context.BulkInsertAsync(adjectiveForms, new BulkConfig { BatchSize = entryBulkConfig.BatchSize });
            Console.WriteLine($"Bulk inserted {adjectiveForms.Count} adjective forms.");
        }
        else
        {
            Console.WriteLine("No adjective forms to insert.");
        }
        Console.WriteLine("Adjectives loading complete.");
    }

    private async Task LoadVerbsAsync(string filePath, BulkConfig entryBulkConfig)
    {
        var jsonVerbs = await LoadJsonFileAsync<JsonVerb>(filePath);
        if (jsonVerbs == null || !jsonVerbs.Any())
        {
            Console.WriteLine("No verbs to load or file error for verbs.");
            return;
        }
        Console.WriteLine($"Loaded {jsonVerbs.Count} verbs from JSON. Processing...");

        var verbEntries = new List<verb_entry>();
        var entriesWithOriginals = new List<(verb_entry Entry, JsonVerb Original)>();

        foreach (var jsonVerb in jsonVerbs)
        {
            if (jsonVerb == null || string.IsNullOrWhiteSpace(jsonVerb.ClassName)) continue;
            var entry = new verb_entry { _class = CleanString(jsonVerb.ClassName) };
            verbEntries.Add(entry);
            entriesWithOriginals.Add((entry, jsonVerb));
        }

        if (verbEntries.Any())
        {
            await _context.BulkInsertAsync(verbEntries, entryBulkConfig);
            Console.WriteLine($"Bulk inserted {verbEntries.Count} verb entries.");
        }
        else
        {
            Console.WriteLine("No valid verb entries to insert.");
        }

        var verbForms = new List<verb_form>();
        foreach (var (entry, originalJson) in entriesWithOriginals)
        {
            if (entry.id == 0 && verbEntries.Any())
            {
                Console.WriteLine($"Warning: Verb entry for class '{entry._class}' did not get an ID. Skipping its forms.");
                continue;
            }
            if (originalJson.Forms != null)
            {
                foreach (var kvp in originalJson.Forms)
                {
                    string section = CleanString(kvp.Key);
                    if (string.IsNullOrEmpty(section)) continue;

                    foreach (var formVal in kvp.Value)
                    {
                        string cleanedForm = CleanString(formVal);
                        if (string.IsNullOrEmpty(cleanedForm)) continue;

                        verbForms.Add(new verb_form
                        {
                            entry_id = entry.id,
                            section = section,
                            form = cleanedForm
                        });
                    }
                }
            }
        }
        if (verbForms.Any())
        {
            await _context.BulkInsertAsync(verbForms, new BulkConfig { BatchSize = entryBulkConfig.BatchSize });
            Console.WriteLine($"Bulk inserted {verbForms.Count} verb forms.");
        }
        else
        {
            Console.WriteLine("No verb forms to insert.");
        }
        Console.WriteLine("Verbs loading complete.");
    }

    private async Task LoadNounsAsync(string filePath, BulkConfig entryBulkConfig)
    {
        var jsonNouns = await LoadJsonFileAsync<JsonNoun>(filePath);
        if (jsonNouns == null || !jsonNouns.Any())
        {
            Console.WriteLine("No nouns to load or file error for nouns.");
            return;
        }
        Console.WriteLine($"Loaded {jsonNouns.Count} nouns from JSON. Processing...");

        var nounEntries = new List<noun_entry>();
        var entriesWithOriginals = new List<(noun_entry Entry, JsonNoun Original)>();

        foreach (var jsonNoun in jsonNouns)
        {
            if (jsonNoun == null || string.IsNullOrWhiteSpace(jsonNoun.ClassName)) continue;
            var entry = new noun_entry { _class = CleanString(jsonNoun.ClassName) };
            nounEntries.Add(entry);
            entriesWithOriginals.Add((entry, jsonNoun));
        }

        if (nounEntries.Any())
        {
            await _context.BulkInsertAsync(nounEntries, entryBulkConfig);
            Console.WriteLine($"Bulk inserted {nounEntries.Count} noun entries.");
        }
        else
        {
            Console.WriteLine("No valid noun entries to insert.");
        }

        var nounForms = new List<noun_form>();
        foreach (var (entry, originalJson) in entriesWithOriginals)
        {
            if (entry.id == 0 && nounEntries.Any())
            {
                Console.WriteLine($"Warning: Noun entry for class '{entry._class}' did not get an ID. Skipping its forms.");
                continue;
            }
            if (originalJson.Forms != null)
            {
                foreach (var kvp in originalJson.Forms)
                {
                    string number = CleanString(kvp.Key);
                    if (string.IsNullOrEmpty(number)) continue;

                    foreach (var formVal in kvp.Value)
                    {
                        string cleanedForm = CleanString(formVal);
                        if (string.IsNullOrEmpty(cleanedForm)) continue;

                        nounForms.Add(new noun_form
                        {
                            entry_id = entry.id,
                            number = number,
                            form = cleanedForm
                        });
                    }
                }
            }
        }
        if (nounForms.Any())
        {
            await _context.BulkInsertAsync(nounForms, new BulkConfig { BatchSize = entryBulkConfig.BatchSize });
            Console.WriteLine($"Bulk inserted {nounForms.Count} noun forms.");
        }
        else
        {
            Console.WriteLine("No noun forms to insert.");
        }
        Console.WriteLine("Nouns loading complete.");
    }

    private async Task LoadAdverbsAsync(string filePath, BulkConfig entryBulkConfig)
    {
        var jsonAdverbs = await LoadJsonFileAsync<JsonAdverb>(filePath);
        if (jsonAdverbs == null || !jsonAdverbs.Any())
        {
            Console.WriteLine("No adverbs to load or file error for adverbs.");
            return;
        }
        Console.WriteLine($"Loaded {jsonAdverbs.Count} adverbs from JSON. Processing...");

        var adverbEntries = new List<adverb_entry>();
        var entriesWithOriginals = new List<(adverb_entry Entry, JsonAdverb Original)>();

        foreach (var jsonAdverb in jsonAdverbs)
        {
            if (jsonAdverb == null || string.IsNullOrWhiteSpace(jsonAdverb.ClassName)) continue;
            var entry = new adverb_entry { _class = CleanString(jsonAdverb.ClassName) };
            adverbEntries.Add(entry);
            entriesWithOriginals.Add((entry, jsonAdverb));
        }

        if (adverbEntries.Any())
        {
            await _context.BulkInsertAsync(adverbEntries, entryBulkConfig);
            Console.WriteLine($"Bulk inserted {adverbEntries.Count} adverb entries.");
        }
        else
        {
            Console.WriteLine("No valid adverb entries to insert.");
        }

        var adverbForms = new List<adverb_form>();
        foreach (var (entry, originalJson) in entriesWithOriginals)
        {
            if (entry.id == 0 && adverbEntries.Any())
            {
                Console.WriteLine($"Warning: Adverb entry for class '{entry._class}' did not get an ID. Skipping its forms.");
                continue;
            }
            if (originalJson.Forms != null) 
            {
                foreach (var formVal in originalJson.Forms)
                {
                    string cleanedForm = CleanString(formVal);
                    if (string.IsNullOrEmpty(cleanedForm)) continue;

                    adverbForms.Add(new adverb_form
                    {
                        entry_id = entry.id,
                        form = cleanedForm
                    });
                }
            }
        }
        if (adverbForms.Any())
        {
            await _context.BulkInsertAsync(adverbForms, new BulkConfig { BatchSize = entryBulkConfig.BatchSize });
            Console.WriteLine($"Bulk inserted {adverbForms.Count} adverb forms.");
        }
        else
        {
            Console.WriteLine("No adverb forms to insert.");
        }
        Console.WriteLine("Adverbs loading complete.");
    }
}
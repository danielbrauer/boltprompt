using System.Drawing;
using System.Reflection;
using System.Text.Json;
using LanguageModels;

namespace boltprompt;

internal class Configuration
{
    [DescriptionForLanguageModel("The maximum number of suggestions to show.")]
    public int NumSuggestions { get; set; } = 10;
    [DescriptionForLanguageModel("The background color for suggestions (In rrggbb hex format)")]
    public string SuggestionBackgroundColor { get; set; } = "d0d0d0";
    [DescriptionForLanguageModel("The background color for the selected suggestion (In rrggbb hex format).")]
    public string SelectedSuggestionBackgroundColor { get; set; } = "30c0ff";
    [DescriptionForLanguageModel("The text color for suggestions (In rrggbb hex format).")]
    public string SuggestionTextColor { get; set; } = "303030";
    [DescriptionForLanguageModel("The text color for the current proposed auto-complete selection in the command line (In rrggbb hex format).")]
    public string AutocompleteTextColor { get; set; } = "a0a0a0";
    [DescriptionForLanguageModel("The prefix text to show before the command line prompt.")]
    public string PromptPrefix { get; set; } = Prompt.ComposePromptPrefixScheme([
        (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{user_name}"),
        (BufferedConsole.ConsoleColor.Gray6, BufferedConsole.ConsoleColor.Gray20, "{working_directory_short_path}{prompt_symbol}")
    ]);
    [DescriptionForLanguageModel("Your OpenAI API key to use for AI-based suggestions.")]
    public string? OpenAiApiKey { get; set; } = null;
    
    private static Configuration Load()
    {
        if (Paths.Configuration.FileExists())
            return JsonSerializer.Deserialize<Configuration>(Paths.Configuration.ReadAllText()) ?? new(); 

        return new();
    }

    private void Write()
    {
        Paths.Configuration.WriteAllText(JsonSerializer.Serialize(this));
    }

    public static void ListProperties()
    {
        foreach (var pi in typeof(Configuration).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            Console.WriteLine($"{pi.Name}::{pi.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(DescriptionForLanguageModel))?.ConstructorArguments.FirstOrDefault().Value}");
    }
    
    public string Get(string propertyName) => 
        typeof(Configuration).GetProperty(propertyName)?.GetValue(this)?.ToString() ?? $"Property {propertyName} not found.";

    public void Set(string propertyName, string value)
    {
        var prop = typeof(Configuration).GetProperty(propertyName);
        if (prop == null)
            throw new InvalidDataException($"Invalid config property name: {propertyName}");

        object propValue;
        if (prop.PropertyType == typeof(string))
            propValue = value;
        else if (prop.PropertyType == typeof(int))
            propValue = int.Parse(value);
        else if (prop.PropertyType == typeof(BufferedConsole.ConsoleColor))
            propValue = (BufferedConsole.ConsoleColor)int.Parse(value);
        else
            propValue = Convert.ChangeType(value, prop.PropertyType);
        prop.SetValue(this, propValue);
        Write();
    }

    public static Configuration Instance { get; } = Load();

    public static void ListPropertyValues(string propertyName, string? prefix)
    {
        var prop = typeof(Configuration).GetProperty(propertyName);
        if (prop == null)
            throw new InvalidDataException($"Invalid config property name: {propertyName}");
        if (prop.Name == nameof(PromptPrefix))
        {
            void PrintPromptPrefix(
                (BufferedConsole.ConsoleColor bg, BufferedConsole.ConsoleColor fg, string label)[] parts)
            {
                var promptPrefixScheme = Prompt.ComposePromptPrefixScheme(parts);
                Console.WriteLine($"\"{prefix}\"::{Prompt.GetPromptPrefix(promptPrefixScheme)}");
            }

            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{working_directory_name}{prompt_symbol}")
            ]);
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{working_directory_short_path}{prompt_symbol}")
            ]);
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{working_directory_path}{prompt_symbol}")
            ]);

            
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{user_name}"),
                (BufferedConsole.ConsoleColor.Gray6, BufferedConsole.ConsoleColor.Gray20, "{working_directory_name}{prompt_symbol}")
            ]);
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{user_name}"),
                (BufferedConsole.ConsoleColor.Gray6, BufferedConsole.ConsoleColor.Gray20, "{working_directory_short_path}{prompt_symbol}")
            ]);
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{user_name}"),
                (BufferedConsole.ConsoleColor.Gray6, BufferedConsole.ConsoleColor.Gray20, "{working_directory_path}{prompt_symbol}")
            ]);
            
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{host_name}"),
                (BufferedConsole.ConsoleColor.Gray6, BufferedConsole.ConsoleColor.Gray20, "{user_name}"),
                (BufferedConsole.ConsoleColor.Gray10, BufferedConsole.ConsoleColor.Gray20, "{working_directory_name}{prompt_symbol}")
            ]);
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{host_name}"),
                (BufferedConsole.ConsoleColor.Gray6, BufferedConsole.ConsoleColor.Gray20, "{user_name}"),
                (BufferedConsole.ConsoleColor.Gray10, BufferedConsole.ConsoleColor.Gray20, "{working_directory_short_path}{prompt_symbol}")
            ]);
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Black, BufferedConsole.ConsoleColor.Gray20, "{host_name}"),
                (BufferedConsole.ConsoleColor.Gray6, BufferedConsole.ConsoleColor.Gray20, "{user_name}"),
                (BufferedConsole.ConsoleColor.Gray10, BufferedConsole.ConsoleColor.Gray20, "{working_directory_path}{prompt_symbol}")
            ]);
            
            PrintPromptPrefix([
                (BufferedConsole.ConsoleColor.Red, BufferedConsole.ConsoleColor.Gray20, "{timestamp}{prompt_symbol}")
            ]);

        } 
        else if (prop.PropertyType == typeof(string) && prop.Name.EndsWith("Color"))
        {
            if (prefix != null)
                Console.WriteLine(prefix);
            void PrintColor(string html)
            {
                var prefixColor = html;
                if (prefix != null)
                    prefixColor = $"{prefix}{html[prefix.Length..]}";
                Console.WriteLine($"{prefixColor}::\u001b[38;5;{(int)BufferedConsole.ColorForHtml(prefixColor)}m{GetClosestColorName(prefixColor)}");
            }

            string GetClosestColorName(string htmlColor)
            {
                double GetColorDifference(Color c1, Color c2) => Math.Sqrt(
                    Math.Pow(c1.R - c2.R, 2) +
                    Math.Pow(c1.G - c2.G, 2) +
                    Math.Pow(c1.B - c2.B, 2));
        
                var closestColorName = "";
                var smallestDifference = double.MaxValue;

                var rgb = BufferedConsole.ParseHtmlColor(htmlColor);
                var color = Color.FromArgb(255, rgb.r, rgb.g, rgb.b);
                foreach (KnownColor knownColor in Enum.GetValues(typeof(KnownColor)))
                {
                    var known = Color.FromKnownColor(knownColor);
                    if (known.IsSystemColor) 
                        continue;
                    var difference = GetColorDifference(color, known);
                    if (!(difference < smallestDifference)) continue;
                    smallestDifference = difference;
                    closestColorName = known.Name;
                }

                return closestColorName;
            }

            string[] colors =  
            [
                "000000",
                "FFFFFF",
                "FF0000",
                "008000",
                "0000FF",
                "FFFF00",
                "00FFFF",
                "FF00FF",
                "808080",
                "C0C0C0",
                "800000",
                "808000",
                "000080",
                "800080",
                "008080",
                "00FF00",
                "FFA500",
                "FFC0CB",
                "A52A2A",
                "FFD700"
            ];
            if (prefix != null)
                colors = colors.Select(c => $"{prefix}{c[prefix.Length..]}").Distinct().ToArray();
            foreach (var c in colors) 
                PrintColor(c);
        }
    }
}
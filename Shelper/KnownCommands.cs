using NiceIO;

namespace Shelper;

public static class KnownCommands
{
    private static readonly Dictionary<string, Task<CommandInfo>> AllKnownCommands;

    public delegate void UpdateCommandInfo(CommandInfo ci);

    public static event UpdateCommandInfo? CommandInfoLoaded;
    private static async Task<CommandInfo> CreateAndCacheCommandInfo(string command)
    {
        NPath commandDir = "Commands";
        commandDir = commandDir.MakeAbsolute();
        var path = commandDir.Combine($"{command}.json");
        var ci = await GptCommandInfoSupplier.GetCommandInfoForCommand(command);
        path.WriteAllText(ci.Serialize());
        CommandInfoLoaded?.Invoke(ci);
        return ci;
    }
    
    private static async Task<CommandInfo> LoadCachedCommandInfo(NPath path)
    {
        var json = await File.ReadAllTextAsync(path.ToString());
        var ci = CommandInfo.Deserialize(json);
        if (ci == null)
            throw new InvalidDataException($"Could not load command info json for {path}");
        CommandInfoLoaded?.Invoke(ci);
        return ci;
    }
    
    public static CommandInfo? GetCommand(string command, bool createInfoIfNotAvailable, out bool isPending)
    {
        isPending = false;
        if (AllKnownCommands.TryGetValue(command, out var ci))
        {
            if (ci.IsCompleted) 
                return ci.Result;
            isPending = true;
            return null;
        }

        if (!createInfoIfNotAvailable) return null;
        
        AllKnownCommands[command] = CreateAndCacheCommandInfo(command);
        return null;
    }
    
    static KnownCommands()
    {
        AllKnownCommands = new Dictionary<string, Task<CommandInfo>>
        {
            ["ls"] = Task.FromResult(CommandInfo.Ls)
        };
        NPath commandDir = "Commands";
        commandDir = commandDir.MakeAbsolute();
        if (commandDir.DirectoryExists())
        {
            foreach (var file in commandDir.Files("*.json", true))
                AllKnownCommands[file.FileNameWithoutExtension] = LoadCachedCommandInfo(file);
        }
        else
        {
            commandDir.CreateDirectory();
            foreach (var cmd in AllKnownCommands)
            {
                var path = commandDir.Combine($"{cmd.Key}.json");
                path.WriteAllText(cmd.Value.Result.Serialize());
            }
        }
    }
}
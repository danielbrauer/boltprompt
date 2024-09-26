using System.Reflection;
using NiceIO;

namespace boltprompt;

public static class Prompt
{
    private static int _promptLength;
    private static int _scrollOffset;
    private static int _commandLength;
    private static int _commandLineCursorPosition;
    public static void SetCursorPosition(int commandLineCursorPosition)
    {
        var pos = BufferedConsole.GetCursorPosition();
        _commandLineCursorPosition = commandLineCursorPosition;
        _scrollOffset = Math.Clamp(_scrollOffset, commandLineCursorPosition - Console.WindowWidth + _promptLength + 1, commandLineCursorPosition);
        BufferedConsole.SetCursorPosition(_commandLineCursorPosition + _promptLength - _scrollOffset, pos.Top);
        BufferedConsole.Flush();
    }

    static string CurrentDirectoryNameForPrompt(NPath path) => path == NPath.HomeDirectory ? "~" : path.FileName;
    
    public static void RenderPrompt(string? commandline = null, string? selectedSuggestion = null)
    {
        BufferedConsole.Update();
        var pos = BufferedConsole.GetCursorPosition();
        BufferedConsole.BackgroundColor = BufferedConsole.ConsoleColor.Black;
        BufferedConsole.ForegroundColor = BufferedConsole.ConsoleColor.White;
        BufferedConsole.SetCursorPosition(0, pos.Top);
        var debug = Assembly.GetEntryAssembly()?.Location.ToNPath().Parent.Parent.FileName == "Debug";
        var promptchar = debug ? "🪲" : "⚡️";
        var promptText = $"{CurrentDirectoryNameForPrompt(NPath.CurrentDirectory)}{promptchar}";
        BufferedConsole.Write(promptText);
        BufferedConsole.ResetColor();
        BufferedConsole.ForegroundColor = BufferedConsole.ConsoleColor.Black;
        BufferedConsole.Write("\uE0B0 ");
        BufferedConsole.ResetColor();
        _promptLength = promptText.Length + 2;
        if (commandline == null) return;
        var commandLineLastWord = Suggestor.SplitCommandIntoWords(commandline).LastOrDefault("");
        var selectedSuggestionSuffix = "";
        
        if (selectedSuggestion != null && commandLineLastWord.Length < selectedSuggestion.Length)
            selectedSuggestionSuffix = selectedSuggestion[commandLineLastWord.Length..];

        _commandLength = commandline.Length + selectedSuggestionSuffix.Length;
        
        var remainingSpace = Console.WindowWidth - _promptLength - _commandLength;
        var charactersToSkip = _scrollOffset;
        
        if (charactersToSkip > 0)
        {
            BufferedConsole.Write("⋯");
            charactersToSkip++;
        }

        var parts = Suggestor.ParseCommandLine(commandline);
        foreach (var part in parts)
        {
            BufferedConsole.Bold = part.Type switch
            {
                Suggestor.CommandLinePart.PartType.Command => true,
                Suggestor.CommandLinePart.PartType.Argument => false,
                Suggestor.CommandLinePart.PartType.Operator => true,
                Suggestor.CommandLinePart.PartType.Whitespace => false,
                _ => throw new ArgumentOutOfRangeException()
            };
            BufferedConsole.Underline = part.ArgumentType switch
            {
                CommandInfo.ArgumentType.Directory => true,
                CommandInfo.ArgumentType.File => true,
                CommandInfo.ArgumentType.FileSystemEntry => true,
                _ => false
            };
            if (charactersToSkip < part.Text.Length)
                BufferedConsole.Write(part.Text[charactersToSkip..]);
            charactersToSkip -= part.Text.Length;
            if (charactersToSkip < 0)
                charactersToSkip = 0;
        }
        
        BufferedConsole.ForegroundColor = BufferedConsole.ConsoleColor.Gray15;
        BufferedConsole.Write(selectedSuggestionSuffix[charactersToSkip..]);

        if (remainingSpace + _scrollOffset < 0)
        {
            BufferedConsole.SetCursorPosition(Console.WindowWidth - 1, pos.Top);
            BufferedConsole.Write("⋯");
        }


        BufferedConsole.ResetColor();

        SuggestionConsoleViewer.ClearLineFromCursor();
        SetCursorPosition(_commandLineCursorPosition);
    }
}
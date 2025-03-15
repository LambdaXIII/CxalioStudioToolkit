using MediaKiller;
using Spectre.Console;
using Spectre.Console.Cli;


Console.CancelKeyPress += (sender, e) =>
{
    AnsiConsole.MarkupLine("[red]Ctrl+C[/] detected. Quitting...");
    XEnv.Instance.WannaQuit = true;
    e.Cancel = true;
};

var app = new CommandApp<MediaKillerCommand>();
return app.Run(args);



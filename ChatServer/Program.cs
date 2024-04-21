using System.Net;
using System.Net.Sockets;
using ChatServer;
using ChatServer.Core;
using ChatServer.Models;
using CommandLine;
using CommandLine.Text;

var parserResult = new Parser(with =>
{
    with.AutoHelp = false;
    with.HelpWriter = Console.Out;
}).ParseArguments<CommandLineOptions>(args);

if (args.Contains("-h"))
{
    Console.WriteLine(HelpText.AutoBuild(parserResult, h => h, e => e));
    return;
}

var commandLineOptions = parserResult.Value;

if (parserResult.Errors.Any() || !IPAddress.TryParse(commandLineOptions.IpAddress, out var ipAddress))
{
    Console.Error.WriteLine("Invalid command line options");
    Environment.ExitCode = 1;
    return;
}

CancellationTokenSource cancellationTokenSource = new();

Console.CancelKeyPress += (sender, args) =>
{
    cancellationTokenSource.Cancel();
    cancellationTokenSource.Dispose();
    args.Cancel = true;
};

using var server = new ServerListener(ipAddress, commandLineOptions.Port, commandLineOptions.UdpConfirmationAttempts,
    commandLineOptions.UdpConfirmationTimeout);
await server.StartListeningAsync(cancellationTokenSource.Token);

if (server.IsInternalError)
{
    Environment.ExitCode = 1;
}

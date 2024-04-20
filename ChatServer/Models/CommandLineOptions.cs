using CommandLine;

namespace ChatServer.Models;

public class CommandLineOptions
{
    [Option('l', Required = false, HelpText = "Server IP or hostname", Default = "0.0.0.0")]
    public string IpAddress { get; set; } = null!;

    [Option('p', Required = false, HelpText = "Server port", Default = (ushort)4567)]
    public ushort Port { get; set; }

    [Option('d', Required = false, HelpText = "UDP confirmation timeout", Default = (ushort)250)]
    public ushort UdpConfirmationTimeout { get; set; }

    [Option('r', Required = false, HelpText = "UDP confirmation attempts", Default = (byte)3)]
    public byte UdpConfirmationAttempts { get; set; }
}
using ChatServer.Models;
using ChatServer.SocketClients;
using ChatServer.Utilities;

namespace ChatServer;

public class MessageProcessor : IMessageProcessor
{
    private const string ServerName = "Server";
    private const string DefaultChannelName = "Default";

    private readonly IAuthenticationService authenticationService;
    private readonly IChannelManager channelManager;
    private readonly WorkflowGraph workflowGraph = new WorkflowGraph();
    private readonly IIpkClient ipkClient;
    private readonly User user;

    public MessageProcessor(IAuthenticationService authenticationService, IChannelManager channelManager,
        IIpkClient ipkClient, User user)
    {
        this.authenticationService = authenticationService;
        this.channelManager = channelManager;
        this.ipkClient = ipkClient;
        this.user = user;
    }

    public async Task ProcessMessage(ResponseResult request, User user, CancellationToken cancellationToken = default)
    {
        if (request.ProcessingResult == ResponseProcessingResult.AlreadyProcessed ||
            request.Message.MessageType == MessageType.Confirm)
        {
            return;
        }

        if (request.ProcessingResult == ResponseProcessingResult.ParsingError ||
            request.Message.MessageType == MessageType.Unknown)
        {
            await ipkClient.SendError("Invalid message type", cancellationToken);
        }

        var message = request.Message;
        // if (!workflowGraph.IsAllowedMessageType(message.MessageType))
        // {
        //     await ipkClient.SendError("Invalid message type for this state", cancellationToken);
        //     return;
        // }

        // Wrong approach
        workflowGraph.NextState(message.MessageType);

        switch (message.MessageType)
        {
            case MessageType.Auth:
                await ProcessAuthMessage(message);
                break;
            case MessageType.Msg:
                await ProcessMsgMessage(message);
                break;
            case MessageType.Join:
                await ProcessJoinMessage(message);
                break;
            case MessageType.Bye:
                await ProcessByeMessage();
                break;
            case MessageType.Err:
                await ProcessErrMessage(message);
                break;
        }
    }

    public bool IsEndState => workflowGraph.IsEndState;

    private async Task ProcessErrMessage(Message message)
    {
        await ipkClient.Leave();
        await ProcessByeMessage();
    }

    private async Task ProcessByeMessage()
    {
        await ChannelExit();
    }

    private async Task ChannelExit()
    {
        await ProcessMsgMessage(new Message()
        {
            MessageType = MessageType.Msg,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, ServerName },
                {
                    MessageArguments.MessageContent,
                    $"{user.DisplayName} has left the {user.Channel.Name}"
                }
            }
        });
        channelManager.RemoveUserFromChannel(user, user.Channel.Name);
        user.Channel = null;
    }

    private async Task ChannelJoin(string channelId)
    {
        channelManager.AddUserToChannel(user, channelId);
        user.Channel = channelManager.GetChannel(channelId);

        await channelManager.SendToChannel(channelId, null, new Message()
        {
            MessageType = MessageType.Msg,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, ServerName },
                {
                    MessageArguments.MessageContent,
                    $"{user.DisplayName} has joined the {channelId}"
                }
            }
        });
    }

    private async Task ProcessJoinMessage(Message message)
    {
        if (user.Channel != null)
        {
            await ChannelExit();
        }

        var channelId = (string)message.Arguments[MessageArguments.ChannelId];

        await ipkClient.SendReply(true, "Channel joined successfully", message);

        await ChannelJoin(channelId);
    }

    private async Task ProcessMsgMessage(Message message)
    {
        await channelManager.SendToChannel(user.Channel!.Name, user.Username!, message);
    }

    private async Task ProcessAuthMessage(Message message)
    {
        var success = authenticationService.Authenticate((string)message.Arguments[MessageArguments.UserName],
            (string)message.Arguments[MessageArguments.Secret]);

        string messageContent;

        if (success)
        {
            messageContent = "Authentication successful";
        }
        else
        {
            messageContent = "Authentication failed";
        }

        await ipkClient.SendReply(success, messageContent, message);


        if (success)
        {
            user.DisplayName = (string)message.Arguments[MessageArguments.DisplayName];
            user.Username = (string)message.Arguments[MessageArguments.UserName];

            await ChannelJoin(DefaultChannelName);
        }
    }
}
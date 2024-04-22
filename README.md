# IPK Project 2(IOTA: Chat server)

A chat server, that allows clients to communicate with each other over udp or tcp protocols.

## Usage 

### Compilation

```bash
make
```
Executable ```ipk24chat-server``` will be created in the root of the project
### Commandline options

* ```-l``` - Server listening IP address for welcome sockets (default is 0.0.0.0);
* ```-p``` - Server listening port for welcome sockets (default is 4567);
* ```-d``` - UDP confirmation timeout (default is 250);
* ```-r``` - Maximum number of UDP retransmissions (default is 3);
* ```-h``` - Prints program help output.

### Example of usage

```bash
./ipk24chat-server -l 127.0.0.1 -p 4000 -d 300
```

Starts a server, that listens on the address 127.0.0.1 with the port 400 and timeout for udp confimation 300 ms

To stop the server all you have to do is to press ctrl-c

## Implementation

The projected was implemented in .NET Core 8.

### Program flow

At first command line options are parsed to get `CommandLineOptions` object, properties of which are later supplied to the constructor of `ServerListener`. Then `ServerListener::StartListeningAsync()` method is called, that starts listening for incoming first tcp and udp messages in the parallel tasks. In case of udp the udp implementation of `IIpkClient` interface is created in the beginning of listening and after the first message listening port for that client is randomized to accept other new clients on the provided listening port. The tcp variant of `IIpkClient` is created while accepting incoming tcp request. The first received message from a client is later sent to `ProcessRequest` method, where in case of invalid message ERR message with following BYE is sent back. In case of valid message, `UserClient` object is created for interracting with the client. The retrieved message is later sent to the created `UserClient` object for Its processing. Then the `ServerListener` object listens for the other first messages and the process repeats itself. 

<br/>

In the created `UserClient` object, 3 tasks are created for listening, processing and sending messages from another client or server purposes. When the message is accepted from the client in the listening tasks, It's later sent to the processing task via `BlockingCollection`. Meanwhile in the processing task the message is retrieved from `BlockingCollection` and sent to `MessageProcessor::ProcessMessage`, where based on the message type and the interraction state(processing is perfomed by `WorkflowGraph` object), the respective action is committed. In case of AUTH message object that implements `IAuthenticationService` checks for the valid credentials, `ChannelManager` object adds user to the default channel and sends message to clients, that are in the mentioned channel, about the user joining the channel. If the JOIN message is got, user is removed from the last channel, its each member is informed about the user leaving, user is then added to the channel with the channel id provided in the message and all the channel's memebers are informed about the user entering the channel. If the message is invalid or is not supposed to be sent in the current state, ERR with following BYE is sent. After sent or received BYE, all the client related resources together with tasks are disposed. 

### Class diagram

![](./classdiagram.png)

## Testing

The testing phase was perfomed using already implemented client from the first IPK project. The project was tested both in Windows and Linux enviroments 

Here's the input and output of both tcp and udp client:

![](clients_output.png)

The tcp client is in right window and its input was:
```bash
/auth kyle kyle kyle
sup
/join new_channel
sup here
```

And the udp client is in left window and the input was:
```bash
/auth tom tom tom
hey
/join new_channel
hey here
<ctrl-c pressed>
```

And here are the respective outputs from server logs and wireshark:

![](wireshark_and_server_output.png)

## Bibliography

[2]: Dolejška, D. NESFIT/IPK-Projects-2024 -  IPK-Projects-2024 - FIT - VUT Brno - git [online]. 2024. [cit. 2024-04-01]. Available from: https://git.fit.vutbr.cz/NESFIT/IPK-Projects-2024

[2] - Dolejška, D. Server for a chat server using IPK24-CHAT protocol [online]. February 2024. [cited 2024-04-20]. Available at: https://git.fit.vutbr.cz/NESFIT/IPK-Projects-2024/src/branch/master/Project%202/iota

[3]: [Microsoft](https://www.microsoft.com/). \.NET Core [online]. Available fromhttps://learn.microsoft.com/en-us/dotnet

[4]: [Microsoft](https://www.microsoft.com/). TCP overview[online]. 04/17/2024. Available from https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes

[4]: [Microsoft](https://www.microsoft.com/). UdpClient Class[online]. 04/17/2024. Available from https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.udpclient?view=net-8.0
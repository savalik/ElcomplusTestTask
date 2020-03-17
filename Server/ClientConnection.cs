using NetworkProtocol;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Server.MainForm;

namespace Server
{
    internal class ClientConnection
    {
        private readonly TcpClient _client;
        private readonly Logger _logger;
        private readonly LoggingDelegate _loggingDelegate;
        private readonly UserConnectedDelegate _userConnectedDelegate;
        private readonly UserDisconnectedDelegate _userDisconnectedDelegate;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<Message>> _notSentMessages;
        private readonly ConcurrentDictionary<string, bool> _users;
        
        private bool _clientDisconnected;
        private string _nickname = "unknown";
        public ClientConnection(
            TcpClient tcpClient, 
            Logger logger, 
            LoggingDelegate loggingDelegate,
            UserConnectedDelegate userConnectedDelegate,
            UserDisconnectedDelegate userDisconnectedDelegate,
            ConcurrentDictionary<string, ConcurrentQueue<Message>> notSentMessages,
            ConcurrentDictionary<string, bool> users)
        {
            _client = tcpClient;
            _logger = logger;
            _loggingDelegate = loggingDelegate;
            _userConnectedDelegate = userConnectedDelegate;
            _userDisconnectedDelegate = userDisconnectedDelegate;
            _notSentMessages = notSentMessages;
            _users = users;
        }

        public void Process()
        {
            var stream = _client.GetStream();

            Task.Run(() => StartReceiveMessages(stream));

            Task.Run(() => StartSendingMessages(stream));
        }

        private void StartSendingMessages(NetworkStream stream)
        {
            while (!_clientDisconnected)
            {
                _notSentMessages.TryGetValue(_nickname, out var queue);

                if(queue != null && queue.TryDequeue(out var message))
                {
                    if (SendMessage(stream, message))
                    {
                        _notSentMessages[_nickname] = queue;
                    }
                }

                Thread.Sleep(1000);

                if(_users.Count > 0)
                {
                    var users = _users.Select(x => x.Key).ToArray();
                    
                    SendMessage(stream, new UserList(users));
                }
            }
        }

        private bool SendMessage(Stream stream, IData message)
        {
            if (_clientDisconnected || stream == null )
                return false;

            try
            {
                var jsonString = JsonConvert.SerializeObject(new Data() { Item = message });
                var data = Encoding.Unicode.GetBytes(jsonString);
                stream.Write(data, 0, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"Sending message failed. {ex.Message}");
                return false;
            }
        }

        private void StartReceiveMessages(NetworkStream stream)
        {
            try
            {
                var data = new byte[512];
                while (!_clientDisconnected)
                {
                    var builder = new StringBuilder();
                    do
                    {
                        var bytes = stream.Read(data, 0, data.Length);

                        var jsonString = Encoding.Unicode.GetString(data, 0, bytes);

                        var response = JsonConvert.DeserializeObject<Data>(
                            value: jsonString,
                            converters: new ItemConverter());

                        if (response != null && response.Item is Message message)
                        {
                            builder.Append($"From {message.From} to {message.To}: {message.Text}");

                            var newQueue = new ConcurrentQueue<Message>(new List<Message>() { message });
                            
                            _notSentMessages.AddOrUpdate(
                                key: message.To,
                                addValue: newQueue, 
                                updateValueFactory: (key, oldValue) => {
                                    oldValue.Enqueue(message);
                                    return oldValue;
                                });
                        }
                        
                        if(response != null && response.Item is Event connectionEvent)
                        {
                            switch(connectionEvent.Type)
                            {
                                case EventType.Connect:
                                    HandleConnect(connectionEvent.Nickname, stream);
                                    break;
                                case EventType.Disconnect:
                                    HandleDisconnect(connectionEvent.Nickname, $"{connectionEvent.Nickname} user disconnected.");
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                    while (stream.DataAvailable);

                    LogInfo(builder.ToString());
                }
            }
            catch (Exception ex)
            {
                HandleDisconnect(_nickname, $"{_nickname} disconnected. {ex.Message}");
            }
            finally
            {
                stream?.Close();
                _client?.Close();
            }
        }

        ///<exception cref="Exception"></exception>
        private void HandleConnect(string nickname, NetworkStream stream)
        {
            if(_users.ContainsKey(nickname))
            {
                SendErrorMessage(nickname, stream);
                throw new Exception("The client tried to use a taken name.");
            }

            _users.GetOrAdd(nickname, true);
            LogInfo($"{nickname} connected.");
            _nickname = nickname;
            _userConnectedDelegate.Invoke(nickname);
        }

        private void SendErrorMessage(string nickname, NetworkStream stream)
        {
            var errorMessage = new Message("SERVER", nickname, $"{nickname} is already in use.");

            SendMessage(stream, errorMessage);
        }

        private void HandleDisconnect(string nickname, string logMessage)
        {
            LogInfo(logMessage);
            _clientDisconnected = true;
            _userDisconnectedDelegate.Invoke(nickname);
            _users.TryRemove(nickname, out _);
        }

        private void LogInfo(string message)
        {
            _loggingDelegate.Invoke(message);
            _logger.Log(message);
        }
    }
}

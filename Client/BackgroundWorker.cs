using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Client.MainForm;

namespace Client
{
    public class BackgroundWorker : IDisposable
    {
        private bool _cancellationRequested;
        private bool _isConnected;

        private string _nickname;
        private ICollection<string> _currentUsers = new List<string>();

        private TcpClient _client;
        private NetworkStream _stream;
        private FillChatDelegate _fillChatDelegate;
        private UpdateUserListDelegate _updateUserListDelegate;

        public void Dispose()
        {
            _cancellationRequested = true;

            SendMessage(new NetworkProtocol.Event(NetworkProtocol.EventType.Disconnect, _nickname));

            _client?.Close();
        }

        ///<exception cref="SocketException"></exception>
        public void Connect(
            string hostname, 
            int port, 
            string nickname, 
            FillChatDelegate fillChatDelegate, 
            UpdateUserListDelegate updateUserListDelegate)
        {
            if (!_isConnected)
            {
                _nickname = nickname;
                _fillChatDelegate = fillChatDelegate;
                _updateUserListDelegate = updateUserListDelegate;

                _client = new TcpClient(hostname, port);
                _stream = _client.GetStream();
                _isConnected = true;

                SendMessage(new NetworkProtocol.Event(NetworkProtocol.EventType.Connect, _nickname));

                Task.Run(StartReceiveMessages);
            }
        }

        public bool SendMessage(NetworkProtocol.IData message)
        {
            try
            {
                if (!_isConnected)
                {
                    return false;
                }
                
                var jsonString = JsonConvert.SerializeObject(
                    value: new NetworkProtocol.Data() { Item = message },
                    formatting: Formatting.None);

                var data = Encoding.Unicode.GetBytes(jsonString);
                _stream.Write(data, 0, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private void StartReceiveMessages()
        {
            try
            {
                var data = new byte[512];
                while (!_cancellationRequested)
                {
                    var builder = new StringBuilder();
                    do
                    {
                        var bytes = _stream.Read(data, 0, data.Length);

                        var jsonString = Encoding.Unicode.GetString(data, 0, bytes);

                        var response = JsonConvert.DeserializeObject<NetworkProtocol.Data>(
                            value: jsonString,
                            converters: new NetworkProtocol.ItemConverter());

                        if(response != null && response.Item is NetworkProtocol.Message message)
                        {
                            builder.Append($"{message.From}: {message.Text}");
                            _fillChatDelegate.Invoke(builder.ToString());
                        }

                        if (response != null
                            && response.Item is NetworkProtocol.UserList userList 
                            && _currentUsers.Intersect(userList.Users).Count() != userList.Users.Length)
                        {
                            _updateUserListDelegate.Invoke(userList.Users);
                            _currentUsers = userList.Users;
                        }
                    }
                    while (_stream.DataAvailable);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}

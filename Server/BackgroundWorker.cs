using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Server.MainForm;

namespace Server
{
    public class BackgroundWorker : IDisposable
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<NetworkProtocol.Message>> _notSentMessages
            = new ConcurrentDictionary<string, ConcurrentQueue<NetworkProtocol.Message>>();
        private readonly ConcurrentDictionary<string, bool> _users =
            new ConcurrentDictionary<string, bool>();
        private readonly Logger _logger;

        private bool _cancellationRequested;

        private LoggingDelegate _loggingDelegate;
        private UserConnectedDelegate _userConnectedDelegate;
        private UserDisconnectedDelegate _userDisconnectedDelegate;
        private IPAddress _address;
        private int _port;

        public BackgroundWorker(Logger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            _cancellationRequested = true;
        }

        public void Start(
            IPAddress address, 
            int port, 
            LoggingDelegate loggingDelegate, 
            UserConnectedDelegate userConnectedDelegate, 
            UserDisconnectedDelegate userDisconnectedDelegate)
        {
            _address = address;
            _port = port;
            _loggingDelegate = loggingDelegate;
            _userConnectedDelegate = userConnectedDelegate;
            _userDisconnectedDelegate = userDisconnectedDelegate;
            Task.Run(StartInternal);
        }

        private async Task StartInternal()
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(_address, _port);
                listener.Start();
                _logger.Log("Waiting for connections...");

                while (!_cancellationRequested)
                {
                    var client = await Task.Run(() => listener.AcceptTcpClient());

                    var clientConnection = new ClientConnection(
                        tcpClient: client,
                        logger: _logger,
                        loggingDelegate: _loggingDelegate,
                        userConnectedDelegate: _userConnectedDelegate,
                        userDisconnectedDelegate: _userDisconnectedDelegate,
                        notSentMessages: _notSentMessages, users: _users);

                    var clientThread = new Thread(clientConnection.Process);
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error in {nameof(BackgroundWorker)}: {ex.Message}\n");
                _logger.Log(ex.StackTrace);
            }
            finally
            {
                listener?.Stop();
            }
        }
    }
}

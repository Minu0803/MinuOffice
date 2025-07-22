using ServerMessenger.Common;
using ServerMessenger.Managers;
using ServerMessenger.Network;
using ServerMessenger.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerMessenger.Network
{
    public class SocketServerManager
    {
        private Socket _listenerSocket;
        private Thread _acceptThread;
        private List<ClientSession> _clientSessions;
        private bool _isRunning;

        private RoomManager _roomManager;
        private DataService _dataService;
        private Action<ClientSession> _onClientConnected;
        private Action<string> _onLog;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        public SocketServerManager(Action<string> onLog)
        {
            _dataService = new DataService();
            _roomManager = new RoomManager(_dataService);
            _clientSessions = new List<ClientSession>();
            _onLog = onLog;
            
        }


        public void StartServer(Action<ClientSession> onClientConnected)
        {

            if (_isRunning)
                return;

            _onClientConnected = onClientConnected;

            _dataService.Open();
            _dataService.GetAllUsers();
            _roomManager.LoadRoomsFromDatabase();

            try
            {
                _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Constants.ServerPort));
                _listenerSocket.Listen(1000);

                _isRunning = true;
                _acceptThread = new Thread(AcceptClients) { IsBackground = true }; 
                _acceptThread.Start();

                _onLog?.Invoke($"[서버 시작]");
            }
            catch (Exception ex)
            {
                _onLog?.Invoke($"[서버 시작 오류] {ex.Message}");
            }
        }

        public void StopServer()
        {
            if (!_isRunning)
                return;

            _dataService.Close();

            try
            {
                _isRunning = false;

                try
                {
                    _listenerSocket?.Close();
                }
                catch (Exception ex)
                {
                    _onLog?.Invoke($"[소켓 닫기 예외] {ex.Message}");
                }

                if (_acceptThread != null && _acceptThread.IsAlive)
                    _acceptThread.Join();

                lock (_clientSessions)
                {
                    foreach (var session in _clientSessions)
                    {
                        session.Disconnect();
                    }
                    _clientSessions.Clear();
                }

                _onLog?.Invoke("[서버 중지 완료]");
            }
            catch (Exception ex)
            {
                _onLog?.Invoke($"[서버 중지 오류] {ex.Message}");
            }
        }

        private void AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    Socket clientSocket = _listenerSocket.Accept();
                    _onLog?.Invoke($"[클라이언트 접속] {clientSocket.RemoteEndPoint}");

                    ClientSession session = new ClientSession(clientSocket, this, _roomManager, _onLog, _onClientConnected, _dataService);

                    lock (_clientSessions)
                    {
                        _clientSessions.Add(session);
                    }

                    _onClientConnected?.Invoke(session);
                    session.StartReceiveLoop();
                }
                catch (Exception ex)
                {
                   Console.WriteLine($"[AcceptClients예외 발생] {ex.Message}");
                }
            }
        }

        public ClientSession FindClientSessionByUserId(string userId)
        {
            lock (_clientSessions)
            {
                return _clientSessions.Find(s => s.UserId == userId);
            }
        }

        public void RemoveClientSession(ClientSession session)
        {
            lock (_clientSessions)
            {
                _clientSessions.Remove(session);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

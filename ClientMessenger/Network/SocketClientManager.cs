using ClientMessenger.Common;
using ClientMessenger.DTO;
using ClientMessenger.Models;
using ClientMessenger.ViewModels;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ClientMessenger.Network
{
    public class SocketClientManager
    {
        private Socket _clientSocket;        
        private bool _isConnected;

        public bool IsConnected
        {
            get
            {
                return _isConnected && _clientSocket != null && _clientSocket.Connected;
            }
        }
        public MySession CurrentSession { get; set; }
        public Action<LoginResponseDto> OnLoginSuccess { get; set; }

        public bool Connect()
        {
            if (_isConnected && _clientSocket != null && _clientSocket.Connected)
                return true;

            try
            {
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.Connect("127.0.0.1", Constants.ServerPort);
                _isConnected = true;

                CurrentSession = new MySession(_clientSocket);
                if (OnLoginSuccess != null)
                {
                    CurrentSession.OnLoginSuccess = OnLoginSuccess;
                }

                CurrentSession.StartReceiveLoop();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Connect 예외] {ex.Message}");
                _isConnected = false;
                return false;
            }
        }


        public void Disconnect()
        {
            try
            {
                if (CurrentSession != null)
                {
                    CurrentSession.StopReceiveLoop();
                }

                _isConnected = false;
                
                _clientSocket.Close();
                _clientSocket = null;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Disconnect 예외] {ex.Message}");
            }
        }                
        
    }
}

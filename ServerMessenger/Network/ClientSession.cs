using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerMessenger.Common;
using System.Runtime.InteropServices;

namespace ServerMessenger.Network
{
    public class ClientSession
    {
        private Socket _socket;
        private Thread _receiveThread;
        private ManualResetEvent _exitEvent = new ManualResetEvent(false);

        public string UserId { get; private set; }
        public string Nickname { get; private set; }
        public string Email { get; private set; }

        public ClientSession(Socket socket)
        {
            _socket = socket;
        }

        public void Start()
        {
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();
        }

        public void Stop()
        {
            _exitEvent.Set();           

            try
            {
                _socket?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket Close 예외] {ex.Message}");
            }

            _receiveThread?.Join();
        }

        private void ReceiveLoop()
        {
            try
            {
                while (!_exitEvent.WaitOne(0))
                {
                    // 1. 헤더 수신
                    byte[] headerBytes = ReadN(Marshal.SizeOf(typeof(PacketHeader)));
                    if (headerBytes == null)
                        break;

                    PacketHeader header = PacketUtils.DeserializeHeader(headerBytes);

                    if (!header.IsValidSignature())
                    {
                        Console.WriteLine("[헤더 오류] 시그니처 불일치");
                        break;
                    }

                    // 2. 바디 수신
                    byte[] bodyBytes = ReadN(header.Length);
                    if (bodyBytes == null)
                        break;

                    string bodyText = Encoding.UTF8.GetString(bodyBytes);
                    HandlePacket((PacketType)header.Type, bodyText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[수신 루프 오류] {ex.Message}");
            }
        }

        private byte[] ReadN(int size)
        {
            byte[] buffer = new byte[size];
            int offset = 0;

            try
            {
                while (offset < size)
                {
                    int read = _socket.Receive(buffer, offset, size - offset, SocketFlags.None);
                    if (read == 0)
                    {
                        Console.WriteLine("[수신 종료] 클라이언트 소켓이 닫혔습니다.");
                        return null;
                    }

                    offset += read;
                }

                return buffer;
            }
            catch (SocketException se)
            {
                Console.WriteLine($"[ReadN 소켓 오류] {se.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReadN 오류] {ex.Message}");
                return null;
            }
        }

        public void Send(PacketType type, byte[] bodyBytes)
        {
            try
            {
                PacketHeader header = new PacketHeader(type, bodyBytes.Length);
                byte[] headerBytes = PacketUtils.SerializeHeader(header);

                byte[] packet = new byte[headerBytes.Length + bodyBytes.Length];
                Buffer.BlockCopy(headerBytes, 0, packet, 0, headerBytes.Length);
                Buffer.BlockCopy(bodyBytes, 0, packet, headerBytes.Length, bodyBytes.Length);

                _socket.Send(packet);
            }
            catch (SocketException se)
            {
                Console.WriteLine($"[Send 소켓 오류] {se.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Send 오류] {ex.Message}");
            }
        }

        private void HandlePacket(PacketType type, string body) // 미완성
        {
            switch (type)
            {
                case PacketType.LoginRequest:
                    Console.WriteLine($"[로그인 요청] {body}");
                    break;

                case PacketType.SignUpRequest:
                    Console.WriteLine($"[회원가입 요청] {body}");
                    break;

                case PacketType.TextMessage:
                    Console.WriteLine($"[텍스트 메시지] {body}");
                    break;

                default:
                    Console.WriteLine($"[알 수 없는 패킷 타입] {type}");
                    break;
            }
        }
    }
}

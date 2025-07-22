using ClientMessenger.Common;
using ClientMessenger.DTO;
using ClientMessenger.Models;
using ClientMessenger.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace ClientMessenger.Network
{
    public class MySession
    {
        private Socket _socket;
        private Thread _receiveThread;
        private bool _isReceiving;
        private byte[] _receiveBuffer;
                
        public Action<LoginResponseDto> OnLoginSuccess { get; set; }

        public string UserId { get; set; }
        public string Nickname { get; set; }


        public MySession(Socket socket)
        {
            _socket = socket;
            _receiveBuffer = new byte[Constants.DefaultBufferSize];
        }

        public void StartReceiveLoop()
        {
            _isReceiving = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
        }

        private void ReceiveLoop() 
        {
            try
            {
                while (_isReceiving)
                {
                    // 헤더 수신
                    byte[] headerBytes = ReadN(Marshal.SizeOf(typeof(PacketHeader)));
                    if (headerBytes == null)
                        break;

                    PacketHeader header = DeserializeHeader(headerBytes);

                    if (!header.IsValidSignature())
                    {
                        _socket.Close();

                        break;
                    }

                    // 바디 수신
                    byte[] bodyBytes = ReadN(header.Length);
                    if (bodyBytes == null)
                        break;

                    HandlePacket(header, bodyBytes);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReceiveLoop 예외] {ex.Message}");
            }
        }
        public void StopReceiveLoop()
        {
            _isReceiving = false;
        }

        private byte[] ReadN(int size)
        {

            if (size >= _receiveBuffer.Length)
            {
                _receiveBuffer = new byte[size];
            }

            int offset = 0;

            try
            {
                while (offset < size)
                {
                    int read = _socket.Receive(_receiveBuffer, offset, size - offset, SocketFlags.None);
                    if (read == 0)
                    {
                        
                        return null;
                    }
                    offset += read;
                }

                return _receiveBuffer;
            }
            catch (Exception ex)
            {
                
                return null;
            }
        }


        /// <summary>
        /// 텍스트 전용 Send 메서드
        /// </summary>
        public void Send(PacketType type, byte[] jsonBytes)
        {
            try
            {
                int jsonLength = jsonBytes.Length;
                int totalLength = jsonLength;

                var header = new PacketHeader(type, totalLength, jsonLength);
                byte[] headerBytes = SerializeHeader(header);

                byte[] packet = new byte[headerBytes.Length + totalLength];
                Buffer.BlockCopy(headerBytes, 0, packet, 0, headerBytes.Length);
                Buffer.BlockCopy(jsonBytes, 0, packet, headerBytes.Length, jsonLength);

                _socket.Send(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Send 예외] {ex.Message}");
            }
        }


        /// <summary>
        /// 파일 및 이미지용 Send 메서드
        /// </summary>
        public void Send(PacketType type, byte[] jsonBytes, byte[] binaryBytes)
        {
            try
            {
                int jsonLength = jsonBytes.Length; // Json 메타데이터 길이 계산
                int binaryLength = binaryBytes.Length; // 파일 데이터 길이 계산 
                int totalLength = jsonLength + binaryLength; // 전체 바디 길이

                var header = new PacketHeader(type, totalLength, jsonLength); // (패킷 타입, 전체 바디 길이, Json 길이)
                byte[] headerBytes = SerializeHeader(header);

                byte[] packet = new byte[headerBytes.Length + totalLength];
                Buffer.BlockCopy(headerBytes, 0, packet, 0, headerBytes.Length);
                Buffer.BlockCopy(jsonBytes, 0, packet, headerBytes.Length, jsonLength);
                Buffer.BlockCopy(binaryBytes, 0, packet, headerBytes.Length + jsonLength, binaryLength); // Json 다음 위치에 파일 데이터 삽입

                _socket.Send(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Send 예외] {ex.Message}");
            }
        }


        private void HandlePacket(PacketHeader header, byte[] body)
        {
            PacketType type = (PacketType)header.Type;

            switch (type)
            {                
                case PacketType.LoginResponse:
                    HandleLoginResponse(body);
                    break;

                case PacketType.CheckIdDuplicateResponse:
                    CheckIdDuplicateResponse(body);
                    break;

                case PacketType.SignUpResponse:
                    HandleSignUpResponse(body);
                    break;

                case PacketType.CreateRoomResponse:
                    HandleCreateRoomResponse(body);
                    break;

                case PacketType.LoadMessagesResponse:
                    HandleLoadMessagesResponse(body);
                    break;  
                case PacketType.TextMessage:
                    HandleTextMessage(body);
                    break;

                case PacketType.DeleteMessageResponse:
                    HandleDeleteMessageResponse(body);
                    break;

                case PacketType.ImageMessage:
                    HandleImageMessage(header, body);
                    break;

                case PacketType.FileMessage:
                    HandleFileMessage(header, body);
                    break;

                case PacketType.FileSaveResponse:
                    HandleFileSaveResponse(header, body);

                    break;

                default:
                    
                    break;
            }
        }
        

        private void HandleLoginResponse(byte[] body)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body);
                var response = JsonConvert.DeserializeObject<LoginResponseDto>(json);

                OnLoginSuccess?.Invoke(response); // LoginViewModel 에 알림
            }
            catch (Exception ex)
            {
                MessageBox.Show("로그인 응답 처리 중 오류가 발생했습니다.\n" + ex.Message, "처리 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CheckIdDuplicateResponse(byte[] body)
        {
            string json = Encoding.UTF8.GetString(body);
            try
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (response != null && response.TryGetValue("IsDuplicate", out var isDuplicateObj))
                {
                    bool isDuplicate = Convert.ToBoolean(isDuplicateObj);
                    IdDuplicate?.Invoke(isDuplicate); // SignUpViewModel 에 알림
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"예외 발생: {ex.Message}");
            }
        }

        private void HandleSignUpResponse(byte[] body)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body);
                var response = JsonConvert.DeserializeObject<SignUpResponseDto>(json);
                
                SignUpCompleted?.Invoke(response.IsSuccess); // SignUpViewModel 에 알림
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleSignUpResponse 예외] {ex.Message}");
                SignUpCompleted?.Invoke(false);
            }
        }

        private void HandleCreateRoomResponse(byte[] body)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body);
                var response = JsonConvert.DeserializeObject<CreateRoomResponseDto>(json);

                OnRoomCreated?.Invoke(response); // UserListViewModel에게 알림
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleCreateRoomResponse 예외] {ex.Message}");
            }
        }

        private void HandleLoadMessagesResponse(byte[] body)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body);
                var messages = JsonConvert.DeserializeObject<List<MessageModel>>(json);

                if (messages == null || messages.Count == 0)
                    return;
                
                OnMessagesLoaded?.Invoke(messages); // ChattingRoomViewModel 에 알림
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleLoadMessagesResponse 예외] {ex.Message}");
            }
        }

        private void HandleTextMessage(byte[] body)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body);
                var message = JsonConvert.DeserializeObject<MessageModel>(json);

                if (message == null)
                    return;

                // 내 메시지 여부 판단
                message.IsMine = (message.UserId == this.UserId);
                
                OnTextMessageReceived?.Invoke(message); // ChattingRoomViewModel 에 알림
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleTextMessage 예외] {ex.Message}");
            }
        }

        private void HandleDeleteMessageResponse(byte[] body)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body);
                var deleteInfo = JsonConvert.DeserializeObject<DeleteMessageResponseDto>(json);

                if (deleteInfo == null)
                    return;

                OnMessageDeleted?.Invoke(deleteInfo); // ChattingRoomViewModel 에 알림
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleDeleteMessageResponse 예외] {ex.Message}");
            }
        }


        private void HandleImageMessage(PacketHeader header, byte[] body)
        {
            
        }

        private void HandleFileMessage(PacketHeader header, byte[] body)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body);
                var message = JsonConvert.DeserializeObject<MessageModel>(json);

                OnFileMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                // 로깅 또는 에러 표시
                Console.WriteLine($"파일 메시지 처리 중 오류: {ex.Message}");
            }
        }

        private void HandleFileSaveResponse(PacketHeader header, byte[] body)
        {
            try
            {
                // 메타 + 바이너리 분리
                int jsonLength = header.JsonLength;
                byte[] jsonBytes = new byte[jsonLength];
                byte[] binaryBytes = new byte[header.Length - jsonLength];

                Buffer.BlockCopy(body, 0, jsonBytes, 0, jsonLength);
                Buffer.BlockCopy(body, jsonLength, binaryBytes, 0, binaryBytes.Length);

                // 메타 
                string json = Encoding.UTF8.GetString(jsonBytes);
                var meta = JsonConvert.DeserializeObject<FileDownloadResponseDto>(json);

                OnFileChunkReceived?.Invoke(meta, binaryBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 청크 처리 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 구조체를 바이트로 변경
        /// </summary>
        public byte[] SerializeHeader(PacketHeader header)
        {
            int size = Marshal.SizeOf(header); // 구조체 크기 계산
            byte[] buffer = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size); // 힙에 사이즈만큼  메모리 공간 생성 (.NET이 관리하는 메모리가 아니라 네이티브 힙에 공간 생성) -> 보통은 GC
            try
            {
                Marshal.StructureToPtr(header, ptr, true); // 구조체를 메모리에 복사
                Marshal.Copy(ptr, buffer, 0, size); // 복사된 메모리 내용을 byte로 복사
            }
            finally
            {
                Marshal.FreeHGlobal(ptr); // 메모리 해제
            }
            return buffer;
        }

        /// <summary>
        /// 바이트를 구조체로 변경
        /// </summary>
        public PacketHeader DeserializeHeader(byte[] buffer)
        {
            PacketHeader header = new PacketHeader();
            int size = Marshal.SizeOf(header);
            IntPtr ptr = Marshal.AllocHGlobal(size); // 힙에 사이즈만큼  메모리 공간 생성
            try
            {
                Marshal.Copy(buffer, 0, ptr, size);
                header = (PacketHeader)Marshal.PtrToStructure(ptr, typeof(PacketHeader)); // return 타입이 object라서 직접 캐스팅해서 명시
            }
            finally
            {
                Marshal.FreeHGlobal(ptr); // 메모리 해제
            }
            return header;
        }

        // 이벤트 정의
        public event SignUpCompletedHandler SignUpCompleted; // 회원가입 완료 이벤트
        public delegate void SignUpCompletedHandler(bool isSuccess);

        public event DuplicateCheckedHandler IdDuplicate; // 아이디 중복 확인 이벤트
        public delegate void DuplicateCheckedHandler(bool isDuplicate);

        public event RoomCreatedHandler OnRoomCreated; // 채팅방 생성 이벤트
        public delegate void RoomCreatedHandler(CreateRoomResponseDto room);

        public event MessagesLoadedHandler OnMessagesLoaded; // 채팅내역 불러오기 이벤트
        public delegate void MessagesLoadedHandler(List<MessageModel> messages);

        public delegate void TextMessageReceivedHandler(MessageModel message);
        public event TextMessageReceivedHandler OnTextMessageReceived;

        public delegate void MessageDeletedHandler(DeleteMessageResponseDto deleteInfo);
        public event MessageDeletedHandler OnMessageDeleted;

        public delegate void FileMessageReceivedHandler(MessageModel fileMessage);
        public event FileMessageReceivedHandler OnFileMessageReceived;

        public delegate void FileChunkReceivedHandler(FileDownloadResponseDto meta, byte[] chunkData);
        public event FileChunkReceivedHandler OnFileChunkReceived;

    }
}

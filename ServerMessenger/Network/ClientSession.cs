using Mysqlx.Crud;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using ServerMessenger.Common;
using ServerMessenger.DTO;
using ServerMessenger.Managers;
using ServerMessenger.Models;
using ServerMessenger.Services;
using ServerMessenger.Util;
using ServerMessenger.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Windows;

namespace ServerMessenger.Network
{
    public class ClientSession
    {
        private Socket _socket;
        private Thread _receiveThread;
        private RoomManager _roomManager;
        private DataService _dataService;
        private SocketServerManager _socketManager;
        private Action<string> _onLog;
        private Action<ClientSession> _onClientConnected;              
        private byte[] _receiveBuffer;
        private bool _isReceiving;

        // C:\Users\사용자\Documents\MinuOffice\UploadedFiles
        private string _fileSaveDirectory = @"C:\MinuMessenger\UploadedFiles";

        private string _userId;
        private string _nickname;
        private string _email;

        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
            }
        }

        public string Nickname
        {
            get => _nickname;
            set
            {
                _nickname = value;
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
            }
        }

        public ClientSession(Socket socket, SocketServerManager socketManager, RoomManager roomManager, Action<string> onLog, Action<ClientSession> onClientConnected, DataService dataService)
        {
            _socket = socket;
            _roomManager = roomManager;
            _dataService = dataService;
            _socketManager = socketManager;
            _onLog = onLog;
            _onClientConnected = onClientConnected;            
            _receiveBuffer = new byte[Constants.DefaultBufferSize];
        }

        public void StartReceiveLoop()
        {
            _isReceiving = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
        }

        public void Disconnect()
        {
            _isReceiving = false;
            try
            {
                _socket.Close();
            }
            catch (Exception ex)
            {
                _onLog?.Invoke($"[Socket Close 예외] {ex.Message}");
            }

            _receiveThread?.Join();
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
                        _onLog?.Invoke("[헤더 오류] 시그니처 불일치");
                        _socketManager.RemoveClientSession(this);
                        Disconnect();
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
                _onLog?.Invoke($"[수신 루프 오류] {ex.Message}");
            }
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
                        _onLog?.Invoke("[수신 종료] 클라이언트 소켓이 닫혔습니다.");
                        return null;
                    }
                    offset += read;
                }

                return _receiveBuffer;
            }
            catch (Exception ex)
            {
                _onLog?.Invoke($"[ReadN 오류] {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 텍스트용 Send 메서드 
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
                _onLog?.Invoke($"[메시지 전송 오류] {ex.Message}");
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
                case PacketType.CheckIdDuplicateRequest:
                    {
                        CheckIdDuplicateRequest(body, header.JsonLength);
                        break;
                    }

                case PacketType.SignUpRequest:
                    {
                        SignUpRequest(body, header.JsonLength);
                        break;
                    }

                case PacketType.LoginRequest:
                    {
                        LoginRequest(body, header.JsonLength);
                        break;
                    }

                case PacketType.LoadMessagesRequest: 
                    {
                        LoadMessagesRequest(body, header.JsonLength);
                        break;
                    }

                case PacketType.CreateRoomRequest:
                    {
                        CreateRoomRequest(body, header.JsonLength);
                        break;
                    }

                case PacketType.TextMessage:
                    {
                        TextMessageRequest(body, header.JsonLength);
                        break;
                    }

                case PacketType.ImageMessage:
                    {
                        ImageMessageRequest(header, body);
                        break;
                    }

                case PacketType.FileMessage:
                    {
                        FileMessageRequest(header, body);
                        break;
                    }

                case PacketType.FileSaveRequest: // 파일이랑 이미지 둘 다 
                    {
                        FileSaveRequest(body);
                        break;
                    }

                case PacketType.DeleteMessageRequest:
                    {
                        DeleteMessageRequest(body, header.JsonLength);
                        break;
                    }

                case PacketType.DisconnectRequest:
                    {
                        DisconnectRequest(body, header.JsonLength);
                        break;
                    }

                default:
                    _onLog?.Invoke($"[알 수 없는 패킷 타입] {type}");
                    break;
            }
        }

        private void CheckIdDuplicateRequest(byte[] body, int len)
        {
            string json = Encoding.UTF8.GetString(body, 0, len);
            var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            string userId = obj["UserId"];

            bool isDuplicate = _dataService.IsDuplicateId(userId);
            var response = new
            {                
                IsDuplicate = isDuplicate, // true면 중복, false면 사용 가능                
            };

            string jsonResponse = JsonConvert.SerializeObject(response);
            Send(PacketType.CheckIdDuplicateResponse, Encoding.UTF8.GetBytes(jsonResponse));
        }
        private void SignUpRequest(byte[] body, int len)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body, 0, len);
                var requestDto = JsonConvert.DeserializeObject<SignUpRequestDto>(json);

                var user = new UserModel
                {
                    UserId = requestDto.UserId,
                    Password = requestDto.Password,
                    Email = requestDto.Email,
                    Nickname = requestDto.Nickname,                     

                };
                
              
                var responseDto = new SignUpResponseDto();                               
                
                bool saved = _dataService.SaveUser(user);
                if (saved)
                {
                    responseDto.IsSuccess = true;                    
                }
                else
                {
                    responseDto.IsSuccess = false;                    
                }
                
                string jsonResponse = JsonConvert.SerializeObject(responseDto);
                Send(PacketType.SignUpResponse, Encoding.UTF8.GetBytes(jsonResponse));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignUpRequest 예외] {ex.Message}");

                var errorResponse = new SignUpResponseDto
                {
                    IsSuccess = false,                    
                };

                string jsonError = JsonConvert.SerializeObject(errorResponse);
                Send(PacketType.SignUpResponse, Encoding.UTF8.GetBytes(jsonError));
            }
        }

        private void LoginRequest(byte[] body, int len)
        {
            string json = Encoding.UTF8.GetString(body, 0, len);
            var requestDto = JsonConvert.DeserializeObject<LoginRequestDto>(json);

            string userId = requestDto.UserId;
            string password = requestDto.Password;

            var user = _dataService.GetUser(userId);
            var response = new LoginResponseDto();  

            if (user == null || !user.CheckPassword(password))
            {
                response.IsSuccess = false;
            }
            else
            {
                UserId = userId;
                Nickname = user.Nickname;
                Email = user.Email;

                response.IsSuccess = true;
                response.UserId = userId;
                response.Nickname = user.Nickname;

                // 참여중인 채팅방
                response.ChatList = _dataService.GetChatRoomsForUser(userId);

                // 전체 유저 목록 (자기 자신 제외)
                response.UserList = _dataService.GetAllUsers()
                    .Where(u => u.UserId != userId)
                    .ToList();

                _onClientConnected?.Invoke(this);
            }

            string jsonResponse = JsonConvert.SerializeObject(response);
            Send(PacketType.LoginResponse, Encoding.UTF8.GetBytes(jsonResponse));
        }

        private void LoadMessagesRequest(byte[] body, int len)
        {
            string json = Encoding.UTF8.GetString(body, 0, len);
            var request = JsonConvert.DeserializeObject<LoadMessagesRequestDto>(json);

            int roomId = request.RoomId;
            int offset = request.Offset;
            int limit = request.Limit;

            var messages = _dataService.GetMessages(roomId, offset, limit).ToList();


            string jsonResponse = JsonConvert.SerializeObject(messages);
            Send(PacketType.LoadMessagesResponse, Encoding.UTF8.GetBytes(jsonResponse));
        }
        private void CreateRoomRequest(byte[] body, int len)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body, 0, len);
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                var userIdsRaw = obj["UserIds"].ToString();
                var userIds = JsonConvert.DeserializeObject<List<string>>(userIdsRaw);

                var roomManager = _roomManager;
                var socketManager = _socketManager;

                List<ClientSession> participants = new List<ClientSession>();

                foreach (string id in userIds)
                {
                    // var users = this._dataService.GetAllUsers();
                    var targetSession = socketManager.FindClientSessionByUserId(id);
                    if (targetSession != null)
                    {
                        participants.Add(targetSession);
                    }
                }

                // 요청 보낸 클라이언트 자신도 포함
                if (!participants.Contains(this))
                    participants.Add(this);

                var room = roomManager.CreateRoom(participants);

                var responseDto = new CreateRoomResponseDto();

                if (room != null)
                {
                    responseDto.IsSuccess = true;
                    responseDto.RoomId = room.RoomId;
                    responseDto.RoomName = room.RoomName;
                    responseDto.ParticipantIds = room.Participants;
                }
                else // 방 생성 실패
                {
                    responseDto.IsSuccess = false;
                    responseDto.RoomId = -1;
                    responseDto.RoomName = null;
                    responseDto.ParticipantIds = new List<string>();
                }

                string jsonResponse = JsonConvert.SerializeObject(responseDto);
                Send(PacketType.CreateRoomResponse, Encoding.UTF8.GetBytes(jsonResponse));
            }
            catch (Exception ex)
            {
                var errorDto = new CreateRoomResponseDto
                {
                    IsSuccess = false,
                    RoomId = -1,
                    RoomName = null,
                    ParticipantIds = new List<string>()
                };

                string errorJson = JsonConvert.SerializeObject(errorDto);
                Send(PacketType.CreateRoomResponse, Encoding.UTF8.GetBytes(errorJson));
            }
        }

        private void TextMessageRequest(byte[] body, int len)
        {
            string json = Encoding.UTF8.GetString(body, 0, len);
            var msg = JsonConvert.DeserializeObject<MessageModel>(json);

            msg.Type = "text";
            msg.Timestamp = DateTime.Now;            
            msg.IsDeleted = false;

            _roomManager.SaveMessage(msg.RoomId, msg); // DB 저장

            var room = _roomManager.GetRoom(msg.RoomId);
            if (room != null)
            {
                string jsonToSend = JsonConvert.SerializeObject(msg);
                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonToSend);

                foreach (var userId in room.Participants)
                {
                    if (userId == msg.UserId)
                        continue; // 자기 자신 제외

                    var targetSession = _socketManager.FindClientSessionByUserId(userId); // 온라인 세션 찾기
                    if (targetSession != null)
                    {
                        targetSession.Send(PacketType.TextMessage, bodyBytes);
                    }
                }
            }
        }
        private void ImageMessageRequest(PacketHeader header, byte[] body)
        {
            // 헤더에서 Json 길이를 가져와 배열로 분리
            int jsonLength = header.JsonLength;
            byte[] jsonBytes = new byte[jsonLength]; // 메타 정보
            Buffer.BlockCopy(body, 0, jsonBytes, 0, jsonLength); // body에서 jsonLength 만큼을 jsonBytes에 복사 (메타 데이터 추출)

            // 이미지 청크 데이터 추출
            byte[] chunkBytes = new byte[body.Length - jsonLength];
            Buffer.BlockCopy(body, jsonLength, chunkBytes, 0, chunkBytes.Length); // 이미지 데이터 추출

            var json = Encoding.UTF8.GetString(jsonBytes);
            var meta = JsonConvert.DeserializeObject<FileTransferChunkDto>(json);

            int roomId = meta.RoomId;
            string userId = meta.SenderId;
            string fileName = meta.FileName;
            int totalChunks = meta.TotalChunks;
            int chunkIndex = meta.ChunkIndex;
            bool isFinal = meta.IsFinal;

            // 임시 디렉토리에 청크 저장
            string tempDir = Path.Combine(_fileSaveDirectory, "Temp", roomId.ToString(), fileName); // Temp/방ID/파일이름 으로 경로 생성
            Directory.CreateDirectory(tempDir);
            string chunkPath = Path.Combine(tempDir, $"{chunkIndex}.part"); // 파일의 각 청크를 순서대로 .part 파일로 저장
            File.WriteAllBytes(chunkPath, chunkBytes);

            _onLog?.Invoke($"[청크 수신] {fileName} (chunk {chunkIndex + 1}/{totalChunks})"); // 몇 번째 청크를 받았는지

            // 마지막 청크 수신 시 이미지 파일 결합
            if (isFinal)
            {
                string finalDir = Path.Combine(_fileSaveDirectory, roomId.ToString());
                Directory.CreateDirectory(finalDir);

                string savedPath = Path.Combine(finalDir, fileName);

                using (var output = new FileStream(savedPath, FileMode.Create)) // 파일이 이미 존재하면 해당 파일을 덮어씀
                {
                    for (int i = 0; i < totalChunks; i++)
                    {
                        string partPath = Path.Combine(tempDir, $"{i}.part");
                        byte[] partBytes = File.ReadAllBytes(partPath);
                        output.Write(partBytes, 0, partBytes.Length);
                    }
                }

                Directory.Delete(tempDir, true);

                // 썸네일 생성
                string thumbnailName = $"thumb_{fileName}"; // 썸네일용 이름 생성
                string thumbnailPath = Path.Combine(finalDir, thumbnailName); // 썸네일 저장 경로

                try
                {
                    using (System.Drawing.Image original = System.Drawing.Image.FromFile(savedPath))

                    //위에서 로드한 original 이미지에서 200x200 크기의 썸네일을 생성
                    using (System.Drawing.Image thumb = original.GetThumbnailImage(200, 200, () => false, IntPtr.Zero)) // 중간에 취소할지여부, 사용자 정의데이터
                    {
                        thumb.Save(thumbnailPath);
                    }
                }
                catch (Exception ex)
                {
                    _onLog?.Invoke($"[썸네일 생성 실패] {ex.Message}");
                    thumbnailPath = null;
                }

                // DB에 메시지 저장
                var msg = new MessageModel
                {
                    RoomId = roomId,
                    UserId = userId,
                    Timestamp = DateTime.Now,
                    Type = "image",
                    FileName = fileName,
                    FileSize = new FileInfo(savedPath).Length.ToString(),
                    FileUrl = savedPath,
                    ThumbnailUrl = thumbnailPath,
                    IsDeleted = false
                };

                _roomManager.SaveMessage(roomId, msg);

                string jsonToSend = JsonConvert.SerializeObject(msg);
                byte[] jsonBody = Encoding.UTF8.GetBytes(jsonToSend);

                // 방 참여자들에게 이미지 전송
                var room = _roomManager.GetRoom(roomId);
                if (room != null)
                {
                    foreach (var targetId in room.Participants)
                    {
                        if (targetId == userId) continue; // 전송자 제외

                        var session = _socketManager.FindClientSessionByUserId(targetId);
                        if (session != null)
                        {
                            session.Send(PacketType.ImageMessage, jsonBody);
                        }
                    }
                }
                _onLog?.Invoke($"[이미지 저장 및 전송 완료] {fileName} → {savedPath}");
            }

        }
        private void FileMessageRequest(PacketHeader header, byte[] body)
        {
            // 헤더에서 Json 길이를 가져와 배열로 분리
            int jsonLength = header.JsonLength;
            byte[] jsonBytes = new byte[jsonLength];
            Buffer.BlockCopy(body, 0, jsonBytes, 0, jsonLength);

            // 파일 청크 추출
            byte[] chunkBytes = new byte[body.Length - jsonLength];
            Buffer.BlockCopy(body, jsonLength, chunkBytes, 0, chunkBytes.Length);

            var json = Encoding.UTF8.GetString(jsonBytes);
            var meta = JsonConvert.DeserializeObject<FileTransferChunkDto>(json);

            int roomId = meta.RoomId;
            string userId = meta.SenderId;
            string fileName = meta.FileName;
            int totalChunks = meta.TotalChunks;
            int chunkIndex = meta.ChunkIndex;
            bool isFinal = meta.IsFinal;

            // 임시 디렉토리에 청크 저장
            string tempDir = Path.Combine(_fileSaveDirectory, "Temp", roomId.ToString(), fileName);
            Directory.CreateDirectory(tempDir);
            string chunkPath = Path.Combine(tempDir, $"{chunkIndex}.part");
            File.WriteAllBytes(chunkPath, chunkBytes);

            _onLog?.Invoke($"[파일 청크 수신] {fileName} (chunk {chunkIndex + 1}/{totalChunks})");

            // 마지막 청크인 경우 파일 결합
            if (isFinal)
            {
                string finalDir = Path.Combine(_fileSaveDirectory, roomId.ToString());
                Directory.CreateDirectory(finalDir);

                string savedPath = Path.Combine(finalDir, fileName); // 기존 파일 있으면 덮어쓰기

                using (var output = new FileStream(savedPath, FileMode.Create))
                {
                    for (int i = 0; i < totalChunks; i++)
                    {
                        string partPath = Path.Combine(tempDir, $"{i}.part");
                        byte[] partBytes = File.ReadAllBytes(partPath);
                        output.Write(partBytes, 0, partBytes.Length);
                    }
                }

                Directory.Delete(tempDir, true);

                // DB 저장
                var msg = new MessageModel
                {
                    RoomId = roomId,
                    UserId = userId,
                    Timestamp = DateTime.Now,
                    Type = "file",
                    FileName = fileName,
                    FileSize = new FileInfo(savedPath).Length.ToString(),
                    FileUrl = savedPath,
                    IsDeleted = false
                };

                _roomManager.SaveMessage(roomId, msg);

                string jsonToSend = JsonConvert.SerializeObject(msg);
                byte[] jsonBody = Encoding.UTF8.GetBytes(jsonToSend);

                var room = _roomManager.GetRoom(roomId);
                if (room != null)
                {
                    foreach (var targetId in room.Participants)
                    {
                        if (targetId == userId) continue;

                        var session = _socketManager.FindClientSessionByUserId(targetId);
                        if (session != null)
                        {
                            session.Send(PacketType.FileMessage, jsonBody);
                        }
                    }
                }
                _onLog?.Invoke($"[파일 저장 및 전송 완료] {fileName} → {savedPath}");
            }

        }
        private void FileSaveRequest(byte[] body)
        {
            try
            {
                var json = Encoding.UTF8.GetString(body);
                var request = JsonConvert.DeserializeObject<FileDownloadRequestDto>(json);

                int roomId = request.RoomId;
                string fileName = request.FileName;

                // 저장된 파일의 경로
                string filePath = Path.Combine(_fileSaveDirectory, roomId.ToString(), fileName);

                if (!File.Exists(filePath))
                {
                    _onLog?.Invoke($"[파일 없음] 요청된 파일이 존재하지 않음: {filePath}");
                }

                byte[] fileBytes = File.ReadAllBytes(filePath);// 파일 전체를 읽음
                int chunkSize = 1024 * 1024; // 1MB
                int totalChunks = (int)Math.Ceiling((double)fileBytes.Length / chunkSize); // 몇 개의 청크가 필요한지 계산

                _onLog?.Invoke($"[파일 저장 요청 수신] {fileName} → {UserId}, 총 {totalChunks}개 청크");

                for (int i = 0; i < totalChunks; i++)
                {
                    int currentChunkSize = Math.Min(chunkSize, fileBytes.Length - (i * chunkSize));
                    byte[] chunkBytes = new byte[currentChunkSize];
                    Buffer.BlockCopy(fileBytes, i * chunkSize, chunkBytes, 0, currentChunkSize);

                    var meta = new FileDownloadResponseDto
                    {
                        RoomId = roomId,
                        SenderId = UserId,
                        FileName = fileName,
                        TotalChunks = totalChunks,
                        ChunkIndex = i,
                        IsFinal = (i == totalChunks - 1)
                    };

                    string metaJson = JsonConvert.SerializeObject(meta);
                    byte[] metaBytes = Encoding.UTF8.GetBytes(metaJson);

                    Send(PacketType.FileSaveResponse, metaBytes, chunkBytes);
                }

                _onLog?.Invoke($"[파일 저장 응답 완료] {fileName} → {UserId}");
            }
            catch (Exception ex)
            {
                _onLog?.Invoke($"[FileSaveRequest 처리 오류] {ex.Message}");
            }
        }
        
        private void DeleteMessageRequest(byte[] body, int len)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body, 0, len);
                var request = JsonConvert.DeserializeObject<DeleteMessageRequestDto>(json);

                bool success = _roomManager.DeleteMessage(request.RoomId, request.MessageId);

                if (!success)
                {
                    _onLog?.Invoke($"[메시지 삭제 실패] 메시지 ID: {request.MessageId}");
                    return;
                }

                _onLog?.Invoke($"[메시지 삭제 처리 완료] 메시지 ID: {request.MessageId}");

                var room = _roomManager.GetRoom(request.RoomId);
                if (room != null)
                {
                    foreach (var targetId in room.Participants)
                    {
                        var session = _socketManager.FindClientSessionByUserId(targetId);
                        if (session != null)
                        {
                            bool isSender = (targetId == request.RequesterId);

                            var response = new DeleteMessageResponseDto
                            {
                                RoomId = request.RoomId,
                                MessageId = request.MessageId,
                                IsDeleted = true,
                                IsForAll = isSender // 본인일 경우 전체에게 적용할지
                            };

                            string jsonToSend = JsonConvert.SerializeObject(response);
                            byte[] jsonBody = Encoding.UTF8.GetBytes(jsonToSend);

                            session.Send(PacketType.DeleteMessageResponse, jsonBody);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _onLog?.Invoke($"[DeleteMessageRequest 처리 오류] {ex.Message}");
            }
        }


        private void DisconnectRequest(byte[] body, int len)
        {
            string json = Encoding.UTF8.GetString(body, 0, len);
            var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            string userId = obj["UserId"];

            _onLog?.Invoke($"[연결 종료 요청] UserId: {userId}");

            // 세션 목록에서 제거
            _socketManager.RemoveClientSession(this);

            // 연결 해제
            Disconnect();
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

    }
}

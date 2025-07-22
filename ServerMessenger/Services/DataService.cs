using MySql.Data.MySqlClient;
using ServerMessenger.DTO;
using ServerMessenger.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;

namespace ServerMessenger.Services
{
    public class DataService
    {
        private MySqlConnection _connection;
        private string _connectionString;

        public DataService()
        {
            string ip = GetLocalIPAddress();
            if (string.IsNullOrEmpty(ip))
            {
                Console.WriteLine("IP를 가져오지 못했습니다.");
                return;
            }
                _connectionString = $"Server={ip};Database=MinuOffice;Uid=user;Pwd=user;Charset=utf8mb4;";
        }

        // 현재 컴퓨터의 IPv4 주소를 반환
        private string GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList) 
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4 주소 반환
                {
                    return ip.ToString(); 
                }
            }
            return null;
        }

        public void Open()
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                _connection.Open();
                Console.WriteLine("[DB 연결 성공]");
            }
            catch (Exception ex) // 연결 실패 시 예외 처리 추가
            {
                Console.WriteLine($"[DB 연결 실패] {ex.Message}");
            }
        }
        public void Close()
        {
            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Closed)
                {

                    _connection.Dispose(); // 연결 해제
                    Console.WriteLine("[DB 연결 종료]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 연결 종료 오류] {ex.Message}");
            }
        }

        public UserModel GetUser(string userId)
        {
            string query = "SELECT user_id, password, nickname, email FROM users WHERE user_id = @UserId";
            MySqlCommand cmd = null;
            MySqlDataReader reader = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                reader = cmd.ExecuteReader(); // 쿼리 결과를 한 줄씩 순서대로 읽을 수 있게 해주는 객체를 반환

                if (reader.Read()) // 결과가 있으면 true + 첫 번째 줄로 이동
                { 
                    return new UserModel
                    {
                        UserId = reader.GetString("user_id"),     
                        Password = reader.GetString("password"), 
                        Nickname = reader.GetString("nickname"),
                        Email = reader.GetString("email")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - GetUser] {ex.Message}");
            }
            finally
            {
                reader.Dispose(); 
                cmd.Dispose();                 
            }

            return null;
        }
        public List<UserModel> GetAllUsers()
        {
            List<UserModel> users = new List<UserModel>();
            string query = "SELECT user_id, nickname, email FROM users";

            MySqlCommand cmd = null;
            MySqlDataReader reader = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                reader = cmd.ExecuteReader(); // 쿼리 결과를 한 줄씩 순서대로 읽을 수 있게 해주는 객체를 반환

                while (reader.Read())
                {
                    users.Add(new UserModel
                    {
                        UserId = reader.GetString("user_id"),
                        Nickname = reader.GetString("nickname"),
                        Email = reader.GetString("email")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - GetAllUsers] {ex.Message}");
            }
            finally
            {
                reader.Dispose();
                cmd.Dispose();
            }

            return users;
        }

        // ID 중복 검사
        public bool IsDuplicateId(string userId)
        {
            string query = "SELECT COUNT(*) FROM users WHERE user_id = @UserId";

            MySqlCommand cmd = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                int count = Convert.ToInt32(cmd.ExecuteScalar());  // 첫번째 행의 첫번째 열의 값만 가져옴
                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - IsDuplicateId] {ex.Message}");
                return true;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        // 사용자 저장 (회원가입)
        public bool SaveUser(UserModel user)
        {
            string query = @"INSERT INTO users (user_id, password, email, nickname)
                     VALUES (@UserId, @Password, @Email, @Nickname)";

            MySqlCommand cmd = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserId", user.UserId);
                cmd.Parameters.AddWithValue("@Password", user.Password); // 해싱된 값 사용
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Nickname", user.Nickname);

                return cmd.ExecuteNonQuery() == 1; // 성공 여부 반환
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - SaveUser] {ex.Message}");
                return false;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        // 채팅방 생성
        public RoomModel CreateRoom(string roomName)
        {
            string query = "INSERT INTO rooms (room_name) VALUES (@RoomName); SELECT LAST_INSERT_ID();";

            MySqlCommand cmd = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@RoomName", roomName);

                int roomId = Convert.ToInt32(cmd.ExecuteScalar());  // 생성한 방의 RoomId를 가져옴 (첫번째 행의 첫번째 열의 값)만 가져옴

                return new RoomModel
                {
                    RoomId = roomId,
                    RoomName = roomName
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - CreateRoom] {ex.Message}");
                return null;
            }
            finally
            {
                cmd.Dispose();
            }
        }     

        // 채팅방 참가자 추가
        public bool AddParticipant(int roomId, string userId)
        {
            string query = "INSERT INTO room_participants (room_id, user_id) VALUES (@RoomId, @UserId)";

            MySqlCommand cmd = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@RoomId", roomId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                return cmd.ExecuteNonQuery() == 1; // 성공 여부 반환
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - AddParticipant] {ex.Message}");
                return false;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        // 사용자가 보낸 채팅 메시지를 DB에 저장
        public bool SaveMessage(MessageModel message)
        {
            string query = "INSERT INTO messages (user_id, room_id, timestamp, type, content, file_name, file_size, file_url, is_deleted) " +
                           "VALUES (@UserId, @RoomId, @Timestamp, @Type, @Content, @FileName, @FileSize, @FileUrl, @IsDeleted)";

            MySqlCommand cmd = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserId", message.UserId);
                cmd.Parameters.AddWithValue("@RoomId", message.RoomId);
                cmd.Parameters.AddWithValue("@Timestamp", message.Timestamp);
                cmd.Parameters.AddWithValue("@Type", message.Type);
                cmd.Parameters.AddWithValue("@Content", string.IsNullOrEmpty(message.Content) ? (object)DBNull.Value : message.Content);
                cmd.Parameters.AddWithValue("@FileName", string.IsNullOrEmpty(message.FileName) ? (object)DBNull.Value : message.FileName);
                cmd.Parameters.AddWithValue("@FileSize", message.FileSize == null ? (object)DBNull.Value : message.FileSize);
                cmd.Parameters.AddWithValue("@FileUrl", string.IsNullOrEmpty(message.FileUrl) ? (object)DBNull.Value : message.FileUrl);

                cmd.Parameters.AddWithValue("@IsDeleted", message.IsDeleted);

                return cmd.ExecuteNonQuery() == 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - SaveMessage] {ex.Message}");
                return false;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        // 메시지 조회 (방별)
        public List<MessageModel> GetMessages(int roomId, int skip, int take)
        {
            List<MessageModel> messages = new List<MessageModel>();
            string query = "SELECT message_id, room_id, user_id, content, timestamp FROM messages WHERE room_id = @RoomId ORDER BY timestamp LIMIT @Take OFFSET @Skip";

            MySqlCommand cmd = null;
            MySqlDataReader reader = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@RoomId", roomId);
                cmd.Parameters.AddWithValue("@Take", take);
                cmd.Parameters.AddWithValue("@Skip", skip);

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    messages.Add(new MessageModel
                    {
                        MessageId = reader.GetInt32("message_id"),
                        RoomId = reader.GetInt32("room_id"),
                        UserId = reader.GetString("user_id"),
                        Content = reader.GetString("content"),
                        Timestamp = reader.GetDateTime("timestamp")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - GetMessages] {ex.Message}");
            }
            finally
            {
                reader.Dispose();
                cmd.Dispose();
            }

            return messages;
        }

        // 메시지 삭제
        public bool MarkMessageAsDeleted(int messageId)
        {
            string query = "UPDATE messages SET is_deleted = 1 WHERE message_id = @MessageId";

            MySqlCommand cmd = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@MessageId", messageId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - MarkMessageAsDeleted] {ex.Message}");
                return false;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        // 사용자가 참여한 채팅방 목록 조회
        public List<RoomDto> GetChatRoomsForUser(string userId)
        {
            var chatRooms = new List<RoomDto>();

            string queryRooms = @"SELECT r.room_id, r.room_name FROM room_participants rp 
                          JOIN rooms r ON rp.room_id = r.room_id 
                          WHERE rp.user_id = @userId";

            MySqlCommand cmd = null;
            MySqlDataReader reader = null;

            try
            {
                cmd = new MySqlCommand(queryRooms, _connection);
                cmd.Parameters.AddWithValue("@userId", userId);

                reader = cmd.ExecuteReader();
                var roomInfoList = new List<(int RoomId, string RoomName)>();

                while (reader.Read())
                {
                    int roomId = reader.GetInt32("room_id");
                    string roomName = reader.GetString("room_name");

                    roomInfoList.Add((roomId, roomName));
                }

                reader.Close(); 

                foreach (var (roomId, roomName) in roomInfoList)
                {
                    var lastMsg = GetLastMessageForRoom(roomId);
                    chatRooms.Add(new RoomDto
                    {
                        RoomId = roomId,
                        RoomName = roomName,
                        LastMessage = lastMsg["Message"]?.ToString(),
                        Timestamp = lastMsg["Timestamp"]?.ToString()

                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - GetChatRoomsForUser] {ex.Message}");
            }
            finally
            {
                reader.Dispose();
                cmd.Dispose();
            }

            return chatRooms;
        }

        // 최신 메시지 조회 (timestamp 내림차순 정렬 후 1개)
        private Dictionary<string, object> GetLastMessageForRoom(int roomId)
        {
            string query = @"SELECT content, timestamp FROM messages WHERE room_id = @roomId ORDER BY timestamp DESC LIMIT 1";

            MySqlCommand cmd = null;
            MySqlDataReader reader = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@roomId", roomId);

                reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Dictionary<string, object>
                    {
                        { "Message", reader["content"]?.ToString() ?? "" }, // 메시지가 없을 경우 빈 문자열
                        { "Timestamp", reader["timestamp"]?.ToString() ?? "" } // 타임스탬프가 없을 경우 빈 문자열
                    };
                }
                return new Dictionary<string, object>
            {
                { "Message", "" },
                { "Timestamp", "" }
            };
            }

            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - GetLastMessageForRoom] {ex.Message}");
                return new Dictionary<string, object>
            {
                { "Message", "" },
                { "Timestamp", "" }
            };
            }

          
        }

        public RoomModel GetRoom(int roomId)
        {
            string query = "SELECT room_id, room_name FROM rooms WHERE room_id = @RoomId";

            MySqlCommand cmd = null;
            MySqlDataReader reader = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@RoomId", roomId);

                reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new RoomModel
                    {
                        RoomId = reader.GetInt32("room_id"),
                        RoomName = reader.GetString("room_name"),
                        Participants = new List<string>() // 이후에 따로 채움
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - GetRoom] {ex.Message}");
            }
            finally
            {
                reader.Dispose();
                cmd.Dispose();
            }

            return null;
        }


        public List<RoomModel> GetAllRooms()
        {
            List<RoomModel> rooms = new List<RoomModel>();
            string query = "SELECT room_id, room_name FROM rooms";

            MySqlCommand cmd = null;
            MySqlDataReader reader = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    RoomModel room = new RoomModel
                    {
                        RoomId = Convert.ToInt32(reader["room_id"]),
                        RoomName = reader["room_name"]?.ToString(),
                        Participants = new List<string>() // 이후 참가자 정보는 따로 채움
                    };

                    rooms.Add(room);
                }

                return rooms;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - GetAllRooms] {ex.Message}");
                return rooms;
            }
            finally
            {
                reader.Dispose();
                cmd.Dispose();
            }
        }

        public List<string> GetParticipants(int roomId)
        {
            string query = "SELECT user_id FROM room_participants WHERE room_id = @RoomId";
            List<string> participants = new List<string>();

            MySqlCommand cmd = null;
            MySqlDataReader reader = null;

            try
            {
                cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@RoomId", roomId);

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    participants.Add(reader["user_id"]?.ToString() ?? "");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB 오류 - GetParticipants] {ex.Message}");
            }
            finally
            {
                reader.Dispose();
                cmd.Dispose();
            }

            return participants;
        }



    }
}

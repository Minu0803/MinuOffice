using Newtonsoft.Json;
using ServerMessenger.Common;
using ServerMessenger.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace ServerMessenger.Services
{
    public class FileDataService
    {
        public FileDataService()
        {
            Directory.CreateDirectory(Constants.UserDataPath);
            Directory.CreateDirectory(Constants.RoomDataPath);
            Directory.CreateDirectory(Constants.MessageDataPath);
        }

        // ========== 유저 정보 ==========
        public void SaveUser(string userId, string password, string nickname, string email)
        {
            var user = new Dictionary<string, string>
            {
                { "UserId", userId },
                { "Password", password },
                { "Nickname", nickname },
                { "Email", email }
            };

            string path = Path.Combine(Constants.UserDataPath, userId + ".json");
            File.WriteAllText(path, JsonConvert.SerializeObject(user, Formatting.Indented));
        }

        public Dictionary<string, string> LoadUser(string userId)
        {
            string path = Path.Combine(Constants.UserDataPath, userId + ".json");
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        public void DeleteUser(string userId)
        {
            string path = Path.Combine(Constants.UserDataPath, userId + ".json");
            if (File.Exists(path)) File.Delete(path);
        }

        // ========== 채팅방 ==========
        public void SaveRoom(RoomModel room)
        {
            string path = Path.Combine(Constants.RoomDataPath, room.RoomId + ".json");
            File.WriteAllText(path, JsonConvert.SerializeObject(room, Formatting.Indented));
        }

        public RoomModel LoadRoom(string roomId)
        {
            string path = Path.Combine(Constants.RoomDataPath, roomId + ".json");
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<RoomModel>(json);
        }

        public void DeleteRoom(string roomId)
        {
            string path = Path.Combine(Constants.RoomDataPath, roomId + ".json");
            if (File.Exists(path)) File.Delete(path);
        }

        // ========== 메시지 ==========
        public void SaveMessage(string roomId, MessageModel message)
        {
            string path = Path.Combine(Constants.MessageDataPath, roomId + ".json");
            List<MessageModel> messages = new List<MessageModel>();

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var loaded = JsonConvert.DeserializeObject<List<MessageModel>>(json);
                if (loaded != null)
                    messages = loaded;
            }

            messages.Add(message);
            File.WriteAllText(path, JsonConvert.SerializeObject(messages, Formatting.Indented));
        }

        public List<MessageModel> LoadMessages(string roomId)
        {
            string path = Path.Combine(Constants.MessageDataPath, roomId + ".json");
            if (!File.Exists(path)) return new List<MessageModel>();

            string json = File.ReadAllText(path);
            var result = JsonConvert.DeserializeObject<List<MessageModel>>(json);
            return result ?? new List<MessageModel>();
        }

        // ========== 파일 저장 ==========
        public void SaveUploadedFile(string roomId, string fileName, byte[] data)
        {
            string roomFolder = Path.Combine(Constants.RoomDataPath, roomId);
            Directory.CreateDirectory(roomFolder);

            string filePath = Path.Combine(roomFolder, fileName);
            File.WriteAllBytes(filePath, data);
        }
    }
}

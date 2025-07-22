using ServerMessenger.Common;
using ServerMessenger.Models;
using ServerMessenger.Network;
using ServerMessenger.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace ServerMessenger.Managers
{
    public class RoomManager
    {
        private Dictionary<int, List<ClientSession>> _roomSessions;
        private Dictionary<int, RoomModel> _roomModels;       // 방 전체 정보
        private DataService _dataService;
        private object _lock;

        public RoomManager(DataService dataService)
        {
            _roomSessions = new Dictionary<int, List<ClientSession>>();
            _roomModels = new Dictionary<int, RoomModel>();
            _dataService = dataService;
            _lock = new object();
        }

        // 방 생성 후 RoomModel 반환
        public RoomModel CreateRoom(List<ClientSession> participants)
        {
            if (participants == null || participants.Count == 0)
                return null;

            // 참여자 닉네임으로 roomName 자동 생성
            var nicknames = participants.Select(p => p.Nickname).ToList();
            string roomName = string.Join(", ", nicknames);

            // DB에 방 생성 요청
            RoomModel room = _dataService.CreateRoom(roomName);
            if (room == null)
                return null;

            // 메모리상 roomId에 참여자 리스트 등록
            _roomSessions[room.RoomId] = new List<ClientSession>();
            _roomModels[room.RoomId] = room; 

            foreach (var session in participants)
            {
                _dataService.AddParticipant(room.RoomId, session.UserId); // DB
                _roomSessions[room.RoomId].Add(session);
                room.Participants.Add(session.UserId);   // Model
            }

            return room;
        }

        public void SaveMessage(int roomId, MessageModel message)
        {
            lock (_lock)
            {
                // DB 저장
                bool isSaved = _dataService.SaveMessage(message);

                if (!isSaved)
                {
                    Console.WriteLine("[메시지 저장 실패] DB 저장 중 오류 발생");
                    return;
                }               
            }
        }

        public RoomModel GetRoom(int roomId)
        {
            if (_roomModels.ContainsKey(roomId))
                return _roomModels[roomId];

            // 메모리에 없으면 DB에서 조회
            var dbRoom = _dataService.GetRoom(roomId); // 방 이름, ID 등
            var participantIds = _dataService.GetParticipants(roomId); // userId 목록

            if (dbRoom != null)
            {
                dbRoom.Participants = participantIds;
                _roomModels[roomId] = dbRoom; 
                return dbRoom;
            }

            return null;
        }

        // 서버 시작 시 호출
        public void LoadRoomsFromDatabase()
        {
            var allRooms = _dataService.GetAllRooms();
            foreach (var room in allRooms)
            {
                // 참여자 목록 DB에서 가져오기
                var participantIds = _dataService.GetParticipants(room.RoomId);

                // RoomModel에 참여자 등록
                room.Participants = participantIds;

                // 메모리에 등록
                _roomModels[room.RoomId] = room;
            }
        }

        public bool DeleteMessage(int roomId, int messageId)
        {
            return _dataService.MarkMessageAsDeleted(messageId);
        }

    }
}

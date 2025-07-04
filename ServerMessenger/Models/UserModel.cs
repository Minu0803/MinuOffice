using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ServerMessenger.Models
{
    public class UserModel
    {
        public string UserId { get; set; }       
        public string Nickname { get; set; }     // 채팅에서 표시할 이름
        public string Email { get; set; } 

        [JsonIgnore] // JSON 저장/응답 시 제외
        private string _password;               

        // 비밀번호 설정
        public void SetPassword(string password)
        {
            _password = password;
        }

        // 비밀번호 확인
        public bool CheckPassword(string password)
        {
            return _password == password;
        }
    }
}



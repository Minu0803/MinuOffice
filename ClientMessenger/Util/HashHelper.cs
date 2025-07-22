using System.Text;
using System.Security.Cryptography;

namespace ClientMessenger.Util
{
    public static class HashHelper
    {
        public static string ComputeSha256Hash(string rawPassword)
        {
            using (SHA256 sha256 = SHA256.Create())  // SHA256 객체 생성
            {
                byte[] bytes = Encoding.UTF8.GetBytes(rawPassword); // 입력된 패스워드를 바이트 배열로 
                byte[] hash = sha256.ComputeHash(bytes); // SHA256 해시를 계산하여 바이트 배열로 반환 -> 내부적으로 32개의 바이트 값이 나옴 ex)0xAF, 0xA3, 0x2B, 0x10..

                StringBuilder builder = new StringBuilder(); // 결과 해시 값을 16진수 문자열로 변환하기 위한 StringBuilder 생성
                foreach (byte b in hash) // 해시 바이트 배열을 순회하면서 각각을 2자리 16진수 문자열로 변환하여 추가
                    builder.Append(b.ToString("x2")); // 16진수로 변환. ex) 0xAF → "af"

                return builder.ToString(); // 최종 해시 문자열 반환  
            }
        }
    }

}

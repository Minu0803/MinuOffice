using ClientMessenger.Common;
using ClientMessenger.DTO;
using ClientMessenger.Network;
using ClientMessenger.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClientMessenger.ViewModels
{
    public class SignUpViewModel : INotifyPropertyChanged
    {
        private string _userId;
        private string _password;
        private string _passwordConfirm;
        private string _email;
        private string _nickname;
        private SocketClientManager _socketClientManager;
        private bool _isIdChecked; // 사용자가 중복 확인 버튼을 눌렀는지
        private bool _isIdAvailable; // 서버가 사용 가능한 ID라고 응답했는지        


        private RelayCommand _checkDuplicateIdCommand;
        private RelayCommand _signUpCommand;
        private RelayCommand _goToLoginCommand;
        public Action CloseAction { get; set; }

        public string UserId
        {
            get { return _userId; }
            set 
            { 
                _userId = value; 
                OnPropertyChanged(); 
            }
        }

        public string Password
        {
            get { return _password;}
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string PasswordConfirm
        {
            get { return _passwordConfirm; }
            set
            {
                _passwordConfirm = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string Nickname
        {
            get { return _nickname; }
            set
            {
                _nickname = value;
                OnPropertyChanged();
            }
        }

        public ICommand CheckDuplicateIdCommand
        {
            get
            {
                if (_checkDuplicateIdCommand == null)
                {
                    _checkDuplicateIdCommand = new RelayCommand(ExecuteCheckDuplicateId);
                }
                return _checkDuplicateIdCommand;
            }
        }

        public ICommand SignUpCommand
        {
            get
            {
                if (_signUpCommand == null)
                {
                    this._signUpCommand = new RelayCommand(new Action<object>(x =>
                    {
                        ExecuteSignUp();

                    }), CanExecuteSignUp);
                }
                return _signUpCommand;
            }
        }

        public ICommand GoToLoginCommand
        {
            get
            {
                if (_goToLoginCommand == null)
                {
                    _goToLoginCommand = new RelayCommand(ExecuteGoToLogin);
                }
                return _goToLoginCommand;
            }
        }
        public SignUpViewModel(SocketClientManager socketClientManager)
        {
            _socketClientManager = socketClientManager;            
        }

        private void ExecuteCheckDuplicateId(object obj)
        {            

            if (string.IsNullOrWhiteSpace(UserId))
            {
                MessageBox.Show("아이디를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Connect() 호출 이후에 세션이 생성되므로 이 때 이벤트 등록
            if (_socketClientManager.CurrentSession != null)
            {
                _socketClientManager.CurrentSession.IdDuplicate -= OnIdDuplicateChecked; // 중복 방지
                _socketClientManager.CurrentSession.IdDuplicate += OnIdDuplicateChecked;
                _socketClientManager.CurrentSession.SignUpCompleted -= OnSignUpCompleted;
                _socketClientManager.CurrentSession.SignUpCompleted += OnSignUpCompleted;
            }
            JObject jObj = new JObject();
            jObj["UserId"] = UserId;

            string serialJson = Newtonsoft.Json.JsonConvert.SerializeObject(jObj);

            _socketClientManager.CurrentSession.Send(PacketType.CheckIdDuplicateRequest, Encoding.UTF8.GetBytes(serialJson));
        }

        private void OnIdDuplicateChecked(bool isDuplicate)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => // 다른 스레드에서 _isIdChecked 및 _isIdAvailable 를 수정할일이 없어서 락 걸지않음
            {
                _isIdChecked = true;

                if (isDuplicate)
                {
                    _isIdAvailable = false;
                    MessageBox.Show("이미 사용 중인 ID입니다.", "중복 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    _isIdAvailable = true;
                    MessageBox.Show("사용 가능한 ID입니다.", "중복 확인", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }));
        }      

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            /*
             * 정규식 설명:
             * ^                  : 문자열의 시작
             * (?=.*[A-Z])        : 최소 하나의 대문자가 포함되어야 함
             * (?=.*[a-z])        : 최소 하나의 소문자가 포함되어야 함
             * (?=.*[^A-Za-z])    : 최소 하나의 특수문자 또는 숫자 포함 (문자가 아닌 것)
             * .{6,}              : 전체 길이는 최소 6자 이상
             * $                  : 문자열의 끝
             */
            string pattern = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*[^A-Za-z]).{6,}$";

            // 패턴과 비밀번호를 비교
            return Regex.IsMatch(password, pattern);
        }

        private bool CanExecuteSignUp(object obj)
        {
            return !string.IsNullOrWhiteSpace(UserId)
                && !string.IsNullOrWhiteSpace(Password)
                && !string.IsNullOrWhiteSpace(PasswordConfirm)
                && !string.IsNullOrWhiteSpace(Email)
                && !string.IsNullOrWhiteSpace(Nickname);
        }

        private void ExecuteSignUp()
        {                                        

            // 중복 확인 버튼을 눌렀고, 사용가능하고, 중복 확인 후 아이디를 바꾸지 않았는지 확인
            if (!_isIdChecked || !_isIdAvailable)
            {
                MessageBox.Show("아이디 중복 확인을 해주세요.", "확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Password != PasswordConfirm)
            {
                MessageBox.Show("비밀번호가 일치하지 않습니다.", "확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidPassword(Password))
            {
                MessageBox.Show("비밀번호는 6자 이상이며, 대문자, 소문자, 숫자 또는 특수문자를 포함해야 합니다.", "비밀번호 규칙", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var requestDto = new SignUpRequestDto
            {
                UserId = UserId,
                Password = HashHelper.ComputeSha256Hash(Password),
                Email = Email,
                Nickname = Nickname
            };

            string json = JsonConvert.SerializeObject(requestDto);
            byte[] body = Encoding.UTF8.GetBytes(json);

            _socketClientManager.CurrentSession.Send(PacketType.SignUpRequest, body);

        }

        private void OnSignUpCompleted(bool isSuccess)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isSuccess)
                {
                    MessageBox.Show("회원가입이 완료되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseAction?.Invoke();
                }
                else
                {
                    MessageBox.Show("회원가입에 실패했습니다.\n이미 등록된 정보일 수 있습니다.", "실패", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }


        private void ExecuteGoToLogin(object obj)
        {            
            CloseAction?.Invoke();
        }

        /// <summary>
        /// 이벤트 해제
        /// </summary>
        public void Cleanup()
        {
            if (_socketClientManager.CurrentSession != null)
            {
                _socketClientManager.CurrentSession.IdDuplicate -= OnIdDuplicateChecked;
                _socketClientManager.CurrentSession.SignUpCompleted -= OnSignUpCompleted;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

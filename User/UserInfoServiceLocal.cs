using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Playcus.Saves;
using Playcus.Utils;
using UnityEngine;

namespace Playcus
{
    /// <summary>
    /// Provide generic information about user.
    /// </summary>
    [ServiceBind(typeof(IUserInfoService))]
    public class UserInfoServiceLocal : Service, IUserInfoService
    {
        // DEPENDENCIES
        [InjectService] private ISaveService _saveManager;

        // CONFIG
        [SerializeField] private string _debugDeepLink;
        [SerializeField] private string _playerName;

        // CONSTANTS
        private const string PF_KEY_USER_ID = "PDLSDK_USER_ID";
        
        // PROPERTIES
        public bool IsFirstLaunch => _saveVO.SessionCount == 0;

        private DateTime? _installDate = null;
        public DateTime InstallDate
        {
            get
            {
                if (_installDate != null)
                {
                    var ticks = _installDate?.Ticks ?? 0;
                    return new DateTime(ticks);
                }
                
                DateTime parsedDate = DateTime.Now;
                if (!string.IsNullOrEmpty(_saveVO.InstallDate))
                {
                    try
                    {
                        DateTimeFormat.ParseSavedDate(_saveVO.InstallDate, out parsedDate);
                    }
                    catch (Exception e)
                    {
                        parsedDate = DateTime.Now;
                        Console.WriteLine(e);
                    }
                }

                _installDate = parsedDate;
                return parsedDate;
            }
        }

        public DateTime PreviousLaunchDate => _previousLaunchDate;

        public int SessionCount => _saveVO.SessionCount;

        public int PlayingDaysCount => _saveVO.PlayingDaysCount;

        public string DeepLink { get; set; }

        public string PlayerName
        {
            get => _playerName;
            set
            {
                if (_playerName == value) return;
                _playerName = value;
                _saveVO.Name = value;
                _saveManager.Save();
            }
        }
        
        /// <summary>
        /// Gets the user ID.
        /// </summary>
        public string UserID {
            get {
                string v = PlayerPrefs.GetString(PF_KEY_USER_ID, string.Empty);
                if (string.IsNullOrEmpty(v))
                {
                    _isNewPlayer = true;
                    v = GenerateUserID();
                    PlayerPrefs.SetString(PF_KEY_USER_ID, v);
                }
                return v;
            }
        }

        private bool _isNewPlayer;
        public bool IsNewPlayer
        {
            get
            {
                var userId = UserID;
                return _isNewPlayer;
            }
        }

        // PRIVATE
        private UserInfoSaveVO _saveVO = new UserInfoSaveVO();
        private const string SAVE_KEY = "UserInfoServiceLocal";

        private DateTime _previousLaunchDate;

        // Start is called before the first frame update
        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            try
            {
                await _saveManager.RegisterAndLoadAsync(SAVE_KEY, _saveVO);

                _playerName = string.IsNullOrEmpty(_saveVO.Name) ? _playerName : _saveVO.Name;

                if (string.IsNullOrEmpty(_saveVO.InstallDate))
                {
                    // First launch
                    _saveVO.InstallDate = DateTime.Now.Ticks.ToString();
                    _saveVO.PreviousLaunchDate = DateTime.Now.Ticks.ToString();
                    _saveVO.PlayingDaysCount = 1;
                }
                else
                {
                    _saveVO.SessionCount++;
                }
            
                // Bake previous launch date and override in save
                _previousLaunchDate = DateTime.Now;

                TryFixSavedDate();

                DateTimeFormat.ParseSavedDate(_saveVO.PreviousLaunchDate, out _previousLaunchDate);
            
                if (_previousLaunchDate.DayOfYear != DateTime.Now.DayOfYear)
                {
                    _saveVO.PlayingDaysCount++;
                }

                _saveVO.PreviousLaunchDate = DateTime.Now.Ticks.ToString();
            
                // Debug deep links in editor
                if (Application.isEditor && !string.IsNullOrEmpty(_debugDeepLink) && string.IsNullOrEmpty(DeepLink))
                {
                    DeepLink = _debugDeepLink;
                }

                ServiceLoadingComplete();
            }
            catch (OperationCanceledException e)
            {

                Debug.LogError($"{GetType().Name} loading cancelled with exception: {e}",this.gameObject);
                   
                State = ServiceState.Failed;

                // ServiceLoadingComplete();
            }
        }

        
        private void TryFixSavedDate()
        {
            if (long.TryParse(_saveVO.InstallDate, out var res))
            {
                return;
            }
            string installDateString = _saveVO.InstallDate;
            string previousLaunchDateString = _saveVO.PreviousLaunchDate;
            bool dateSwapped = false;
            bool previousSwapped = false;
            DateTime installDate;
            dateSwapped = DateTimeFormat.ParseSavedDate(installDateString, out installDate);
            if (dateSwapped)
            {
                installDateString = DateTimeFormat.TryFixDateString(installDateString);
                previousLaunchDateString = DateTimeFormat.TryFixDateString(previousLaunchDateString);
            }

            DateTime previousLaunchDate;
            previousSwapped = DateTimeFormat.ParseSavedDate(previousLaunchDateString, out previousLaunchDate);
            if (previousSwapped)
            {
                if (dateSwapped)
                {
                    _saveVO.InstallDate = DateTime.Now.Ticks.ToString();
                    _saveVO.PreviousLaunchDate = DateTime.Now.Ticks.ToString();
                    return;
                }
                else
                {
                    installDateString = DateTimeFormat.TryFixDateString(installDateString);
                    if (DateTimeFormat.ParseSavedDate(installDateString, out installDate))
                    {
                        _saveVO.InstallDate = DateTime.Now.Ticks.ToString();
                        _saveVO.PreviousLaunchDate = DateTime.Now.Ticks.ToString();
                        return;
                    }
                }
            }


            if (previousLaunchDate < installDate)
            {
                if (dateSwapped || previousSwapped)
                {
                    _saveVO.InstallDate  = DateTime.Now.Ticks.ToString();
                    _saveVO.PreviousLaunchDate = DateTime.Now.Ticks.ToString();
                }
                else
                {
                    installDateString  = DateTimeFormat.TryFixDateString(installDateString); 
                    previousLaunchDateString  = DateTimeFormat.TryFixDateString(previousLaunchDateString);

                    if (!DateTimeFormat.ParseSavedDate(installDateString, out installDate) &&
                        !DateTimeFormat.ParseSavedDate(previousLaunchDateString, out previousLaunchDate))
                    {
                        if (installDate <= previousLaunchDate)
                        {
                            if (installDate != System.DateTime.MinValue &&
                                previousLaunchDate != System.DateTime.MinValue)
                            {
                                _saveVO.InstallDate = installDate.Ticks.ToString();
                                _saveVO.PreviousLaunchDate = previousLaunchDate.Ticks.ToString();
                            }
                            return;
                        }
                    }
                    _saveVO.InstallDate  = DateTime.Now.Ticks.ToString();
                    _saveVO.PreviousLaunchDate = DateTime.Now.Ticks.ToString();
                }
            }
            else
            {
                if (installDate != System.DateTime.MinValue &&
                    previousLaunchDate != System.DateTime.MinValue)
                {
                    _saveVO.InstallDate = installDate.Ticks.ToString();
                    _saveVO.PreviousLaunchDate = previousLaunchDate.Ticks.ToString();
                }
            }

        }
        
        private string GenerateUserID() {
            return Guid.NewGuid().ToString();
        }
    }

}
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Playcus.Ads.Mock
{
    [RequireComponent(typeof(Canvas))]
    public class MockAdsUI : MonoBehaviour
    {
        [SerializeField] private Text _label;

        [SerializeField] private Button _closeButton;

        [SerializeField] private float _showDelay = 5f;

        public bool simulateFailure;
        
        private Action _onCloseCallback;
        private Action _onFailure;

        void Awake()
        {
            _closeButton.gameObject.SetActive(false);
        }

        public void StartShow(string label,Action onFailure, Action onShow, Action onClose)
        {
            _onCloseCallback = onClose;
            _onFailure = onFailure;
            _label.text = label;
            StartCoroutine(ShowRoutine(onShow));
        }

        private IEnumerator ShowRoutine(Action onShow)
        {
            onShow?.Invoke();
            yield return new WaitForSeconds(_showDelay);

            if (simulateFailure)
            {
                _onFailure?.Invoke();
                Destroy(gameObject);
                yield break;
            }
            _closeButton.gameObject.SetActive(true);

            _closeButton.onClick.AddListener(OnCloseAdsClick);
        }

        private void OnCloseAdsClick()
        {
            _onCloseCallback?.Invoke();
            _closeButton.onClick.RemoveAllListeners();
            Destroy(gameObject);
        }
    }
}
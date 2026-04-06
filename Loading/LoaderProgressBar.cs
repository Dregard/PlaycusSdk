using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Playcus.Analytics;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Playcus.Loading
{
    /// <summary>
    /// How long Loader need be wait before can continue loading
    /// Documentation https://docs.google.com/document/d/1A6Ce52Xh3iLBiVgYKnjTHNCm4g21k5jln3PdMnZE0To/edit#
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LoaderProgressBar : MonoBehaviour
    {
        [HelpBox(@"Display service loading progress by fillAmount of image bar", HelpBoxMessageType.Info)]
        [SerializeField] private bool _readme;
        private Image _progressBar;

        [Range(0.001f,0.1f)]
        [SerializeField] private float _fakePreloadFactor = 0.1f;

        // Use this for initialization
        private void Start()
        {
            _progressBar = GetComponent<Image>();
            ServiceLocator.Get<Loader>().ChunkLoadedEvent += OnNextStepLoaded;
            _progressBar.fillAmount = 0f;

            StartCoroutine(FakeLoadProgress());
        }

        public void OnNextStepLoaded(int step, int maxstep)
        {
            if (_progressBar != null)
            {
                float realProgress = (float)(Mathf.Min(step, maxstep)) / (float)maxstep;
                _progressBar.fillAmount = Mathf.Max(realProgress, _progressBar.fillAmount);
            }
        }

        private IEnumerator FakeLoadProgress()
        {
            while (_progressBar.fillAmount < 1f)
            {
                _progressBar.fillAmount += _fakePreloadFactor * Time.unscaledDeltaTime;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
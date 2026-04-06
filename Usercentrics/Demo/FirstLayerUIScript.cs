using System.Text;
using Unity.Usercentrics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Custom First Layer UI example.
/// This is a basic UI made with Unity that shows some labels
/// and some buttons so the user can interact with them.
/// </summary>

namespace Unity.Usercentrics
{
    public class FirstLayerUIScript : MonoBehaviour
    {
        [SerializeField] private Text Title = null;
        [SerializeField] private Text Description = null;
        [SerializeField] private Text FirstLayerNoteResurface = null;

        [SerializeField] private Button AcceptAllButton = null;
        [SerializeField] private Button DenyAllButton = null;

        public UnityAction OnFinishCallback = null;

        void Start()
        {
            Debug.Log("[USERCENTRICS][DEBUG] start 1st layer script");
            SetContent();

            AcceptAllButton.onClick.AddListener(() => { AcceptAll(); });
            DenyAllButton.onClick.AddListener(() => { DenyAll(); });
        }

        public void SetContent()
        {
            FirstLayerSettings firstLayerSettings = Usercentrics.Instance.GetFirstLayerSettings();

            Title.text = firstLayerSettings.title;
            Description.text = firstLayerSettings.description;
            FirstLayerNoteResurface.text = firstLayerSettings.resurfaceNote;
        }

        private void AcceptAll()
        {
            Usercentrics.Instance.AcceptAll();
            OnFinishCallback?.Invoke();
        }

        private void DenyAll()
        {
            Usercentrics.Instance.DenyAll();
            OnFinishCallback?.Invoke();
        }
    }
}

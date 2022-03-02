using UnityEngine;
using UnityEngine.UI;
using static EngineCommon.Assertions;

namespace EngineCommon
{
    [RequireComponent(typeof(Button))]
    public class TogglePopup : MonoBehaviour
    {
        [SerializeField] private Canvas _popup;
        
        void Awake()
        {
            var button = GetComponent<Button>();
            assert(button != null);
            button.onClick.AddListener(OnTogglePopup);
        }

        private void OnTogglePopup()
        {
            _popup.enabled = !_popup.enabled;
        }
    }
}

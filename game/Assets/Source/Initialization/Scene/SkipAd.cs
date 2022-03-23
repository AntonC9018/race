using System.Threading.Tasks;
using UnityEngine;

namespace Race.SceneTransition
{
    public class SkipAd : MonoBehaviour
    {
        [SerializeField] private GameObject _ad;
        
        // int is dummy
        private TaskCompletionSource<int> _taskCompletion;

        public Task Restart()
        {
            _ad.SetActive(true);
            _taskCompletion = new TaskCompletionSource<int>();
            return _taskCompletion.Task;
        }

        public void OnButtonClicked()
        {
            if (_taskCompletion is not null)
            {
                _ad.SetActive(false);
                _taskCompletion.SetResult(1);
            }
        }
    }
}

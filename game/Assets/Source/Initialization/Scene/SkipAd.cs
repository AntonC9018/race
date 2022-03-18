using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Race.SceneTransition
{
    public class SkipAd : MonoBehaviour
    {
        [SerializeField] private GameObject ad;

        public void OnButtonClicked()
        {
            ad.SetActive(false);
        }
    }
}

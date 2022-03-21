using System;
using System.Collections.Generic;
using UnityEngine;

namespace Race.Gameplay
{
    public interface IDelay
    {
        void Delay(Action action);      
    }

    public class SameDurationDelay : MonoBehaviour, IDelay
    {
        [InspectorName("Delay (sec)")]
        [SerializeField] private float _delay;

        private Queue<(Action callback, float addedTime)> _items;

        void Awake()
        {
            _items = new();
        }

        public void Delay(Action action)
        {
            _items.Enqueue((action, Time.time));
        }

        public void Update()
        {
            while (_items.TryPeek(out var t))
            {
                var (callback, addedTime) = t;

                // Take advantage of the fact that the queue is sorted.
                if (addedTime + _delay > Time.time)
                    break;
                    
                _items.Dequeue();
                callback();
            }
        }
    }
}
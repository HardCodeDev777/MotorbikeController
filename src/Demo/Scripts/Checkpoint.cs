using UnityEngine;

namespace HardCodeDev.Examples
{
    using HardCodeDev.MotorbikeController;

    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Transform _nextCheckpoint;
        [SerializeField] private bool _isLastCheckpoint;
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                var controller = other.gameObject.GetComponent<MotorbikeController>();
                if (!_isLastCheckpoint) controller.aiSettings.currentTarget = _nextCheckpoint;
                gameObject.GetComponent<MeshRenderer>().enabled = false;
                _nextCheckpoint.gameObject.GetComponent<MeshRenderer>().enabled = true;
            }
            if (other.gameObject.CompareTag("Bot"))
            {
                var controller = other.gameObject.GetComponent<MotorbikeController>();
                controller.aiSettings.currentTarget = _nextCheckpoint;
            }
        }
    }
}
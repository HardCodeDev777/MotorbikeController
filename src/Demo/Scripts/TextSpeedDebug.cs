using UnityEngine;

namespace HardCodeDev.Examples
{
    using HardCodeDev.MotorbikeController;

    public class TextSpeedDebug : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _speedText;

        private void Update() => _speedText.text = $"{MotorbikeController.Speed}";
    }
}
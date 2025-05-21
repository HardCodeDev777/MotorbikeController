using UnityEngine;

namespace HardCodeDev.Examples
{
    using HardCodeDev.MotorbikeController;

    public class TextSpeedDebug : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text speedText;

        private void Update() => speedText.text = $"{MotorbikeController.Speed}";
    }
}
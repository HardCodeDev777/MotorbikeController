using UnityEngine;
using System.Collections;
using System;

namespace HardCodeDev.MotorbikeController
{
    public class MotorbikeController : MonoBehaviour
    {
        #region Settings and configs
        private readonly struct PhysicsConfig
        {
            public const float MStoKMH = 3.6f, Watt = 735.499f, Omega = 100f;
        }

        private readonly struct StiffnessSettings
        {
            public readonly float frontForwardStiffness, frontSidewaysStiffness, backForwardStiffness, backSidewaysStiffness;

            public StiffnessSettings(float frontForwardStiffness, float frontSidewaysStiffness, float backForwardStiffness, float backSidewaysStiffness)
            {
                this.frontForwardStiffness = frontForwardStiffness;
                this.frontSidewaysStiffness = frontSidewaysStiffness;
                this.backForwardStiffness = backForwardStiffness;
                this.backSidewaysStiffness = backSidewaysStiffness;
            }
        }

        [Serializable]
        private struct WheelieRuntime
        {
            public float wheelieTime;

            [NonSerialized]
            public float wheelieBreakTime;
            [NonSerialized]
            public bool isWheelieng;
        }

        private readonly struct WheelieConfig
        {
            public readonly float wheelieResetTime, wheelieDelay, rbOriginalMass, rbWheelieMass, motorWheelie;
            public readonly Vector3 wheelieCenterOfMass;
            public WheelieConfig(float wheelieResetTime, float wheelieDelay, float rbOriginalMass, float rbWheelieMass, float motorWheelie, Vector3 wheelieCenterOfMass)
            {
                this.wheelieResetTime = wheelieResetTime;
                this.wheelieDelay = wheelieDelay;
                this.rbOriginalMass = rbOriginalMass;
                this.rbWheelieMass = rbWheelieMass;
                this.motorWheelie = motorWheelie;
                this.wheelieCenterOfMass = wheelieCenterOfMass;
            }
        }

        [Serializable]
        private struct StabilizationSettings
        {
            public float stabilizationAngle, stabilizationPower;

            [NonSerialized]
            public float realStablizationAngle;
        }

        [Serializable]
        private class WheelsSettings
        {
            public float steerAngle;
            public WheelCollider frontCollider, backCollider;
            public Transform frontWheel, backWheel;
        }

        [Serializable]
        private struct SpeedAndBrakeSettings
        {
            public float maxSpeed, breakPower, horsePower;
        }
        #endregion

        [SerializeField] private WheelsSettings _wheelsSettings = new();
        [SerializeField] private SpeedAndBrakeSettings _speedAndBrakeSettings = new();
        [SerializeField] private StabilizationSettings _stabilization = new();
        [SerializeField]
        private WheelieRuntime _wheelieRuntime = new()
        {
            wheelieBreakTime = 0f,
        };

        private WheelieConfig _wheelieConfig;
        private StiffnessSettings _stiffness;
        private Rigidbody _rb;

        public static float Speed { get; private set; }


        #region Setup

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            InitStructures();
            _stabilization.realStablizationAngle = _stabilization.stabilizationAngle;
        }

        private void InitStructures()
        {
            _stiffness = new(_wheelsSettings.frontCollider.forwardFriction.stiffness, _wheelsSettings.frontCollider.sidewaysFriction.stiffness, _wheelsSettings.backCollider.forwardFriction.stiffness, _wheelsSettings.backCollider.sidewaysFriction.stiffness);

            _wheelieConfig = new(wheelieResetTime: 2f, wheelieDelay: 0.5f, rbOriginalMass: _rb.mass, rbWheelieMass: 100f, motorWheelie: 15f, wheelieCenterOfMass: new(0, 0, -4f));
        }
        #endregion

        #region Core updates
        private void Update()
        {
            CalculateSpeed();
            UpdateWheel(_wheelsSettings.frontWheel, _wheelsSettings.frontCollider);
            UpdateWheel(_wheelsSettings.backWheel, _wheelsSettings.backCollider);
            WheelieControl();
            Stabilization();
            StiffnessControl();

            Debug.Log($"Time for wheelie: {_wheelieRuntime.wheelieBreakTime}");
            Debug.Log($"Back: {_wheelsSettings.backCollider.rpm}");
            Debug.Log($"Front: {_wheelsSettings.frontCollider.rpm}");
        }

        private void FixedUpdate()
        {
            MotorInput();
            RotationInput();
            Brake();
            SpeedControl();
        }
        #endregion

        #region Basic
        private void MotorInput()
        {
            var torque = (_speedAndBrakeSettings.horsePower * PhysicsConfig.Watt) / PhysicsConfig.Omega;
            if (_wheelieRuntime.isWheelieng) _wheelsSettings.backCollider.motorTorque = Input.GetAxis("Vertical") * torque * _wheelieConfig.motorWheelie;
            else _wheelsSettings.backCollider.motorTorque = Input.GetAxis("Vertical") * torque;
        }

        private void RotationInput() => _wheelsSettings.frontCollider.steerAngle = Input.GetAxis("Horizontal") * _wheelsSettings.steerAngle;

        private void UpdateWheel(Transform wheel, in WheelCollider wheelCollider)
        {
            wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rotation);
            wheel.position = pos;
            wheel.rotation = rotation * Quaternion.Euler(0, 0, 90);
        }

        private void Brake()
        {
            var isBraking = Input.GetKey(KeyCode.Space);
            if (!_wheelieRuntime.isWheelieng) _wheelsSettings.backCollider.brakeTorque = isBraking ? _speedAndBrakeSettings.breakPower : 0;
            else _wheelsSettings.backCollider.brakeTorque = 0;
            _wheelsSettings.frontCollider.brakeTorque = isBraking ? _speedAndBrakeSettings.breakPower : 0;
        }

        private void SpeedControl()
        {
            if (Speed > _speedAndBrakeSettings.maxSpeed) _rb.linearVelocity = _rb.linearVelocity.normalized * (_speedAndBrakeSettings.maxSpeed / PhysicsConfig.MStoKMH);
        }
        #endregion

        #region Wheelie
        private void WheelieControl()
        {
            if (Input.GetKey(KeyCode.Space) && Input.GetKey(KeyCode.W)) _wheelieRuntime.wheelieBreakTime += 0.1f;
            else StartCoroutine(nameof(WaitToResetWheelieBreakTime));

            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (_wheelieRuntime.wheelieBreakTime >= _wheelieRuntime.wheelieTime) StartCoroutine(nameof(Wheelie));
            }
        }

        private IEnumerator Wheelie()
        {
            yield return new WaitForSeconds(_wheelieConfig.wheelieDelay);
            _wheelieRuntime.isWheelieng = true;
            _rb.centerOfMass = _wheelieConfig.wheelieCenterOfMass;
            _rb.mass = _wheelieConfig.rbWheelieMass;
            StartCoroutine(nameof(WaitToResetWheelie));
        }

        private IEnumerator WaitToResetWheelieBreakTime()
        {
            yield return new WaitForSeconds(_wheelieConfig.wheelieResetTime);
            _wheelieRuntime.wheelieBreakTime = 0f;
        }

        private IEnumerator WaitToResetWheelie()
        {
            yield return new WaitForSeconds(_wheelieConfig.wheelieResetTime);
            _rb.mass = 200f;
            _rb.centerOfMass = Vector3.zero;
            _wheelieRuntime.isWheelieng = false;
        }
        #endregion

        #region Extra helpful
        private void StiffnessControl()
        {
            if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))
            {
                var backForw = _wheelsSettings.backCollider.forwardFriction;
                var backSide = _wheelsSettings.backCollider.sidewaysFriction;
                var frontForw = _wheelsSettings.frontCollider.forwardFriction;
                var frontSide = _wheelsSettings.frontCollider.sidewaysFriction;

                backForw.stiffness = 1f;
                backSide.stiffness = 1f;
                frontForw.stiffness = 1f;
                frontSide.stiffness = 1f;

                _wheelsSettings.backCollider.forwardFriction = backForw;
                _wheelsSettings.backCollider.sidewaysFriction = backForw;
                _wheelsSettings.frontCollider.forwardFriction = backForw;
                _wheelsSettings.frontCollider.sidewaysFriction = backForw;
            }

            else
            {
                var backForw = _wheelsSettings.backCollider.forwardFriction;
                var backSide = _wheelsSettings.backCollider.sidewaysFriction;
                var frontForw = _wheelsSettings.frontCollider.forwardFriction;
                var frontSide = _wheelsSettings.frontCollider.sidewaysFriction;

                backForw.stiffness = _stiffness.backForwardStiffness;
                backSide.stiffness = _stiffness.backSidewaysStiffness;
                frontForw.stiffness = _stiffness.frontForwardStiffness;
                frontSide.stiffness = _stiffness.frontSidewaysStiffness;

                _wheelsSettings.backCollider.forwardFriction = backForw;
                _wheelsSettings.backCollider.sidewaysFriction = backSide;
                _wheelsSettings.frontCollider.forwardFriction = frontForw;
                _wheelsSettings.frontCollider.sidewaysFriction = frontSide;
            }
        }

        private void Stabilization()
        {
            var angle = Vector3.Angle(transform.up, Vector3.up);
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) _stabilization.realStablizationAngle = _stabilization.stabilizationAngle - 1;
            else _stabilization.realStablizationAngle = 0f;

            if (angle > _stabilization.realStablizationAngle)
            {
                var rawDirection = Vector3.Cross(transform.up, Vector3.up);
                var stabilizationPower = Mathf.Clamp01(angle / 15) * _stabilization.stabilizationPower;
                _rb.AddTorque(rawDirection * stabilizationPower);
            }
        }
        #endregion

        private void CalculateSpeed() => Speed = _rb.linearVelocity.magnitude * PhysicsConfig.MStoKMH;
    }
}
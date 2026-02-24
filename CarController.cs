using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider WheelCollider_FL;
    public WheelCollider WheelCollider_FR;
    public WheelCollider WheelCollider_RL;
    public WheelCollider WheelCollider_RR;

    [Header("Wheel Meshes")]
    public Transform FrontLeftWheel;
    public Transform FrontRightWheel;
    public Transform RearLeftWheel;
    public Transform RearRightWheel;

    [Header("Driving")]
    public float motorForce = 1200f;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 30f;
    public float maxSpeedKmh = 120f;
    public float downforce = 80f;

    [Header("Stability")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.6f, 0f);
    public float maxAirAngularSpeed = 3f;
    public float landingVerticalDamping = 0.2f;
    public float landingAssistTime = 0.15f;

    [Header("WheelCollider Tuning")]
    public float suspensionDistance = 0.18f;
    public float suspensionSpring = 25000f;
    public float suspensionDamper = 4500f;
    public float suspensionTargetPosition = 0.5f;
    public float wheelDampingRate = 1.2f;
    public float forwardFrictionStiffness = 1.4f;
    public float sidewaysFrictionStiffness = 2.0f;
    public float forceAppPointDistance = 0.05f;

    [Header("Wheel setup helper")]
    public bool autoAlignWheelColliders = true;
    public float wheelRadiusScale = 0.95f;

    float moveInput;
    float steerInput;
    bool brakeInput;
    bool wasGrounded;
    float landingAssistTimer;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        AlignAllWheelCollidersToMeshes();
        rb.centerOfMass += centerOfMassOffset;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        ConfigureWheelCollider(WheelCollider_FL);
        ConfigureWheelCollider(WheelCollider_FR);
        ConfigureWheelCollider(WheelCollider_RL);
        ConfigureWheelCollider(WheelCollider_RR);

        // Stabilniejsza fizyka WheelColliderów (mniej "wystrzałów").
        WheelCollider_FL.ConfigureVehicleSubsteps(5f, 12, 15);
        WheelCollider_FR.ConfigureVehicleSubsteps(5f, 12, 15);
        WheelCollider_RL.ConfigureVehicleSubsteps(5f, 12, 15);
        WheelCollider_RR.ConfigureVehicleSubsteps(5f, 12, 15);
    }

    void Update()
    {
        moveInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        brakeInput = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        bool grounded = IsGrounded();

        ApplySteering();
        ApplyMotorAndBrakes(grounded);
        ApplyDownforce(grounded);
        StabilizeLanding(grounded);
        SyncWheelMeshes();

        wasGrounded = grounded;
    }


    void OnValidate()
    {
        if (!autoAlignWheelColliders) return;

        AlignWheelColliderToMesh(WheelCollider_FL, FrontLeftWheel);
        AlignWheelColliderToMesh(WheelCollider_FR, FrontRightWheel);
        AlignWheelColliderToMesh(WheelCollider_RL, RearLeftWheel);
        AlignWheelColliderToMesh(WheelCollider_RR, RearRightWheel);
    }

    void AlignAllWheelCollidersToMeshes()
    {
        if (!autoAlignWheelColliders) return;

        AlignWheelColliderToMesh(WheelCollider_FL, FrontLeftWheel);
        AlignWheelColliderToMesh(WheelCollider_FR, FrontRightWheel);
        AlignWheelColliderToMesh(WheelCollider_RL, RearLeftWheel);
        AlignWheelColliderToMesh(WheelCollider_RR, RearRightWheel);
    }

    void AlignWheelColliderToMesh(WheelCollider wc, Transform wheelMesh)
    {
        if (wc == null || wheelMesh == null) return;

        wc.transform.position = wheelMesh.position;

        if (wheelMesh.TryGetComponent(out Renderer r))
        {
            Bounds b = r.bounds;
            float meshRadius = Mathf.Max(b.extents.y, b.extents.x, b.extents.z) * wheelRadiusScale;
            if (meshRadius > 0.05f)
            {
                wc.radius = meshRadius;
            }
        }
    }

    void ConfigureWheelCollider(WheelCollider wc)
    {
        if (wc == null) return;

        wc.suspensionDistance = suspensionDistance;

        JointSpring spring = wc.suspensionSpring;
        spring.spring = suspensionSpring;
        spring.damper = suspensionDamper;
        spring.targetPosition = suspensionTargetPosition;
        wc.suspensionSpring = spring;

        wc.wheelDampingRate = wheelDampingRate;
        wc.forceAppPointDistance = forceAppPointDistance;

        WheelFrictionCurve forward = wc.forwardFriction;
        forward.stiffness = forwardFrictionStiffness;
        wc.forwardFriction = forward;

        WheelFrictionCurve sideways = wc.sidewaysFriction;
        sideways.stiffness = sidewaysFrictionStiffness;
        wc.sidewaysFriction = sideways;
    }

    bool IsGrounded()
    {
        return WheelCollider_FL.isGrounded ||
               WheelCollider_FR.isGrounded ||
               WheelCollider_RL.isGrounded ||
               WheelCollider_RR.isGrounded;
    }

    void ApplySteering()
    {
        float steer = steerInput * maxSteerAngle;
        WheelCollider_FL.steerAngle = steer;
        WheelCollider_FR.steerAngle = steer;
    }

    void ApplyMotorAndBrakes(bool grounded)
    {
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        float torque = grounded && speedKmh < maxSpeedKmh ? moveInput * motorForce : 0f;
        WheelCollider_RL.motorTorque = torque;
        WheelCollider_RR.motorTorque = torque;

        float currentBrake = brakeInput ? brakeForce : 0f;
        WheelCollider_FL.brakeTorque = currentBrake;
        WheelCollider_FR.brakeTorque = currentBrake;
        WheelCollider_RL.brakeTorque = currentBrake;
        WheelCollider_RR.brakeTorque = currentBrake;
    }

    void ApplyDownforce(bool grounded)
    {
        if (grounded)
        {
            rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude, ForceMode.Force);
        }
    }

    void StabilizeLanding(bool grounded)
    {
        if (!grounded)
        {
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, maxAirAngularSpeed);
            landingAssistTimer = 0f;
            return;
        }

        if (!wasGrounded)
        {
            landingAssistTimer = landingAssistTime;

            if (rb.linearVelocity.y > 0f)
            {
                Vector3 firstTouchVelocity = rb.linearVelocity;
                firstTouchVelocity.y *= landingVerticalDamping;
                rb.linearVelocity = firstTouchVelocity;
            }
        }

        if (landingAssistTimer > 0f)
        {
            landingAssistTimer -= Time.fixedDeltaTime;

            Vector3 velocity = rb.linearVelocity;
            if (velocity.y > 0f)
            {
                velocity.y = 0f;
                rb.linearVelocity = velocity;
            }

            Vector3 av = rb.angularVelocity;
            rb.angularVelocity = new Vector3(av.x * 0.7f, av.y, av.z * 0.7f);
        }
    }

    void SyncWheelMeshes()
    {
        UpdateWheel(WheelCollider_FL, FrontLeftWheel);
        UpdateWheel(WheelCollider_FR, FrontRightWheel);
        UpdateWheel(WheelCollider_RL, RearLeftWheel);
        UpdateWheel(WheelCollider_RR, RearRightWheel);
    }

    void UpdateWheel(WheelCollider collider, Transform wheel)
    {
        if (wheel == null || collider == null) return;

        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheel.position = pos;
        wheel.rotation = rot;
    }
}

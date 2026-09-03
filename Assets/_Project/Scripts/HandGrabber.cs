using UnityEngine;

public class HandGrabber : MonoBehaviour
{
    public bool isRightHand = true;
    public ArmMouseIKRig2 controller;     // drag the Player (with ArmMouseIKRig2) here
    public float breakForce = 800f;
    public float breakTorque = 800f;

    FixedJoint joint;

    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;            // detect + drive, won’t mess character
    }

    void OnTriggerStay(Collider other)
    {
        if (joint) return;
        var rb = other.attachedRigidbody;
        if (!rb) return;

        bool pressed = isRightHand ? Input.GetMouseButton(1) : Input.GetMouseButton(0);
        if (!pressed) return;

        joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = rb;
        joint.enableCollision = true;
        joint.breakForce = breakForce;
        joint.breakTorque = breakTorque;

        if (controller) controller.SetGrabbed(isRightHand, true);
    }

    void Update()
    {
        bool released = isRightHand ? !Input.GetMouseButton(1) : !Input.GetMouseButton(0);
        if (released && joint)
        {
            Destroy(joint);
            joint = null;
            if (controller) controller.SetGrabbed(isRightHand, false);
        }
    }

    void OnJointBreak(float force)
    {
        joint = null;
        if (controller) controller.SetGrabbed(isRightHand, false);
    }
}

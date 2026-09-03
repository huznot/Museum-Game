// Add this component to your player temporarily to debug
using UnityEngine;

public class AggressiveVelocityController : MonoBehaviour
{
    private Rigidbody rb;
    public bool forceZeroVelocity = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    void FixedUpdate()
    {
        if (forceZeroVelocity)
        {
            // Aggressively force velocity to zero
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Keep Y for gravity
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    public void EnableForceZero()
    {
        forceZeroVelocity = true;
        Debug.Log("FORCE ZERO VELOCITY ENABLED");
    }
    
    public void DisableForceZero()
    {
        forceZeroVelocity = false;
        Debug.Log("FORCE ZERO VELOCITY DISABLED");
    }
}
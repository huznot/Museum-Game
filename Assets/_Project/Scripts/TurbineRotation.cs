using UnityEngine;

public class TurbineRotation : MonoBehaviour
{
    public float rotationSpeed = -15f; // degrees per second

    void Update()
    {
        // Rotate around the Y axis (change axis if needed)
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }
}

using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MirrorReflection : MonoBehaviour
{
    public Camera mainCamera;      // Drag your Main Camera here
    public Camera mirrorCamera;    // Drag your MirrorCamera here
    public RenderTexture mirrorTexture; // Drag your RenderTexture here

    private void Start()
    {
        if (mirrorCamera.targetTexture != mirrorTexture)
            mirrorCamera.targetTexture = mirrorTexture;

        // Assign the texture to the mirror’s material
        GetComponent<Renderer>().material.mainTexture = mirrorTexture;
    }

    private void LateUpdate()
    {
        if (!mainCamera || !mirrorCamera) return;

        // Mirror plane info
        Vector3 mirrorPos = transform.position;
        Vector3 mirrorNormal = transform.forward; // mirror's outward direction

        // Reflect main camera position across mirror plane
        Vector3 toCam = mainCamera.transform.position - mirrorPos;
        Vector3 reflectedPos = mainCamera.transform.position - 2f * Vector3.Dot(toCam, mirrorNormal) * mirrorNormal;

        // Reflect main camera forward direction
        Vector3 reflectedForward = Vector3.Reflect(mainCamera.transform.forward, mirrorNormal);
        Vector3 reflectedUp = Vector3.Reflect(mainCamera.transform.up, mirrorNormal);

        // Update mirror camera
        mirrorCamera.transform.position = reflectedPos;
        mirrorCamera.transform.rotation = Quaternion.LookRotation(reflectedForward, reflectedUp);
    }
}

using UnityEngine;
using Zenject;

public class WiresInputManager : IInputManager
{
    [InjectOptional] private RectTransform wiresRoot;
    [InjectOptional] private Transform dragPlane;
    [InjectOptional] private Camera camera;

    public Vector3 InputPosition
    {
        get
        {
            if (TryGetOverlayLocalPoint(out var localPoint))
            {
                return localPoint;
            }

            return GetWorldPointOnPlane();
        }
    }

    private bool TryGetOverlayLocalPoint(out Vector2 localPoint)
    {
        localPoint = default;
        if (wiresRoot == null) return false;

        var canvas = wiresRoot.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wiresRoot,
            Input.mousePosition,
            null,
            out localPoint);
    }

    private Vector3 GetWorldPointOnPlane()
    {
        var cam = camera != null ? camera : Camera.main;
        if (cam == null) return Vector3.zero;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var plane = dragPlane != null
            ? new Plane(dragPlane.forward, dragPlane.position)
            : new Plane(Vector3.forward, Vector3.zero);

        return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : Vector3.zero;
    }
}

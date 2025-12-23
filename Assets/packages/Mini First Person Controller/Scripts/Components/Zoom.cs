using UnityEngine;

[ExecuteInEditMode]
public class Zoom : MonoBehaviour
{
    new Camera camera;
    public float defaultFOV = 60;

    void Awake()
    {
        camera = GetComponent<Camera>();
        if (camera)
            defaultFOV = camera.fieldOfView;
    }

    void Update()
    {
        // --- SCROLL ZOOM COMPLETELY DISABLED ---
        // No mouse scroll changes, camera FOV stays at default
        if (camera != null)
            camera.fieldOfView = defaultFOV;
    }
}

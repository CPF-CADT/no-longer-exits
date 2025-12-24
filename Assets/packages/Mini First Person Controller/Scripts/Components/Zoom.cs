using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
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
        if (camera != null)
            camera.fieldOfView = defaultFOV;
    }
}

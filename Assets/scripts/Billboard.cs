using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        // Rotates the UI to face the camera
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up);
    }
}
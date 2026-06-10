using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    private Camera targetCamera;

    void Start()
    {
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;
        transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,
            targetCamera.transform.rotation * Vector3.up);
    }
}
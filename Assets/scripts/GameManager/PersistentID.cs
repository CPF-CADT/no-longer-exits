using UnityEngine;
using System;

// Assign this component to any scene object that should persist state
public class PersistentID : MonoBehaviour
{
    [Tooltip("Globally unique ID used for saving/loading state. Auto-generated if empty.")]
    public string id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }
    }
#endif
}

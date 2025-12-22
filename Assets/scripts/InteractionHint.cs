using UnityEngine;

public class InteractionHint : MonoBehaviour
{
    [Header("Hint Text")]
    [TextArea] public string hintText = "Press E to Interact";
    [TextArea] public string alternateText = "";
    public bool useAlternate;

    public string GetHintText()
    {
        return useAlternate && !string.IsNullOrEmpty(alternateText)
            ? alternateText
            : hintText;
    }
}

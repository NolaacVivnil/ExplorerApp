using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class NonTransparentClick : MonoBehaviour
{
    void Start()
    {
        // Sets the alpha threshold so clicks only register on visible pixels
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }
}
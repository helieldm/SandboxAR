using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public GameObject canvasImage;
    public GameObject text;
    private GameObject text_instance;
    public void DisplayPopup(Vector2 imgPos)
    {
        canvasImage.SetActive(true);
        canvasImage.transform.localPosition = imgPos;
        text_instance = text_instance ? text_instance : Instantiate(text, transform);
    }
    public void ClearPopup()
    {
        canvasImage.SetActive(false);
        if (text_instance != null) Destroy(text_instance);
    }
}

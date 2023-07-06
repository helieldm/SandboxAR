using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public GameObject text;
    public void DisplayPopup()
    {
        Instantiate(text, transform);
    }
}

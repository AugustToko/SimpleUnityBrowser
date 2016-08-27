﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BrowserUI : MonoBehaviour
{

    public Canvas MainCanvas;
    public InputField UrlField;
    public Image Background;
    public Button Back;
    public Button Forward;


    [HideInInspector]
    public bool KeepUIVisible = false;


   

    public void Show()
    {
        UrlField.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
        UrlField.placeholder.gameObject.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
        UrlField.textComponent.gameObject.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
        Back.gameObject.SetActive(true);
        Forward.gameObject.SetActive(true);
        Background.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
    }

    public void Hide()
    {
        if (!KeepUIVisible)
        { 
            if (!UrlField.isFocused)
            {
                UrlField.GetComponent<CanvasRenderer>().SetAlpha(0.0f);
                UrlField.placeholder.gameObject.GetComponent<CanvasRenderer>().SetAlpha(0.0f);
                UrlField.textComponent.gameObject.GetComponent<CanvasRenderer>().SetAlpha(0.0f);
                Back.gameObject.SetActive(false);
                Forward.gameObject.SetActive(false);
                Background.GetComponent<CanvasRenderer>().SetAlpha(0.0f);
            }
            else
            {
                Show();
            }
        }
    }




    void Update()
    {
        if (UrlField.isFocused&&!KeepUIVisible)
        {
            Show();
        }
    }


}

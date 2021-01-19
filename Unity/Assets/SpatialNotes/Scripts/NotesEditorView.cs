using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotesEditorView : MonoBehaviour
{
    public Button saveButton;
    public Button backButton;
    [SerializeField] private Text noteText;
    [SerializeField] private Text statusText;
    [SerializeField] private InputField noteInputField;
    [SerializeField] private CanvasGroup canvasGroup;

    void Start()
    {
        show(false);
    }

    public void show(bool show)
    {
        gameObject.SetActive(show);
        /*
        if (show)
        {
            canvasGroup.alpha = 1;
        }
        else
        {
            canvasGroup.alpha = 0;
        }
        */
    }

    public void setText(string text)
    {
        noteInputField.SetTextWithoutNotify(text);
    }

    public void setFoundNote(string text)
    {
        show(true);
        statusText.text = "Note found!";
        saveButton.gameObject.SetActive(false);
        setText(text);
    }

    public string getText()
    {
        return noteInputField.text;
    }
}

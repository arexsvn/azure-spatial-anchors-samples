using UnityEngine;
using UnityEngine.UI;

public class SpatialNotesUIView : MonoBehaviour
{
    public Button saveButton;
    public Button backButton;
    [SerializeField] private Text noteText;
    [SerializeField] private GameObject noteContainer;
    [SerializeField] private Text statusText;
    [SerializeField] private Text connectionText;
    [SerializeField] private InputField noteInputField;

    void Start()
    {
        showNoteUI(false);
    }

    public void showAll(bool show)
    {
        gameObject.SetActive(show);
    }

    public void showNoteUI(bool show, bool allowBack = false, bool allowSave = false)
    {
        noteContainer.SetActive(show);
        backButton.gameObject.SetActive(allowBack);
        saveButton.gameObject.SetActive(allowSave);
    }

    public void setNoteText(string text)
    {
        noteInputField.SetTextWithoutNotify(text);
    }

    public string getText()
    {
        return noteInputField.text;
    }

    public Text getStatusTextbox()
    {
        return statusText;
    }

    public void setStatusText(string text)
    {
        statusText.text = text;
    }

    public void setConnection(float strength)
    {
        strength = Mathf.Min(1f, strength);

        if (strength >= 1f)
        {
            connectionText.text = "Ready!";
        }
        else
        {
            connectionText.text = "Scanning...\n" + Mathf.Floor(strength * 100f) + "%";
        }

        
    }
}

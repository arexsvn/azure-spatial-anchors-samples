using UnityEngine;
using UnityEngine.UI;

public class SpatialNotesUIView : MonoBehaviour
{
    public Button saveButton;
    public Button backButton;
    public Button deleteButton;
    [SerializeField] private Text noteText;
    [SerializeField] private GameObject noteContainer;
    [SerializeField] private Text confirmButtonText;
    [SerializeField] private Text statusText;
    [SerializeField] private InputField noteInputField;
    [SerializeField] private Image connectionFill;
    [SerializeField] private Image connectionIcon;

    void Start()
    {
        showNoteUI(false);
    }

    public void showAll(bool show)
    {
        gameObject.SetActive(show);
    }

    public void showNoteUI(bool show, bool allowBack = false, bool allowSave = false, bool allowDelete = false)
    {
        noteContainer.SetActive(show);
        backButton.gameObject.SetActive(allowBack);
        saveButton.gameObject.SetActive(allowSave);
        deleteButton.gameObject.SetActive(allowDelete);
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

    public void setConfirmButtonText(string text)
    {
        confirmButtonText.text = text;
    }

    public void setConnection(float strength)
    {
        strength = Mathf.Min(1f, strength);

        connectionFill.fillAmount = strength;

        Color connectionFillColor = Color.red;

        if (strength >= 1f)
        {
            connectionIcon.color = Color.white;
            connectionFillColor = Color.green;
        }
        else if (strength >= .5f)
        {
            connectionIcon.color = Color.grey;
            connectionFillColor = Color.yellow;
        }
        else
        {
            connectionIcon.color = Color.black;
        }

        connectionFill.color = connectionFillColor;
    }
}

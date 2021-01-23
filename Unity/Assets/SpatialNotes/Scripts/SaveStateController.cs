using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class SaveStateController
{
    public SaveState CurrentSave { get; private set; }

    private string _saveSuffix = "_save.txt";
    private string _saveFolderPath = "saveStates/";
    private string _saveId = "123";

    public SaveStateController()
    {
        
    }

    public void createNewSave()
    {
        CurrentSave = new SaveState();
        CurrentSave.id = _saveId;
        CurrentSave.appVersion = Application.version;
        CurrentSave.notes = new List<NoteData>();
    }

    public void loadCurrentSave()
    {
        load(_saveId);

        // upgrade format of save state if necessary...
        if (!isSaveStateValid(CurrentSave) || CurrentSave.appVersion != Application.version)
        {
            createNewSave();
        }

        if (CurrentSave.notes.Count > 0)
        {
            Debug.Log("Save State Found with " + CurrentSave.notes.Count + " notes!");
        }
    }

    public void addNote(string anchorId, string noteText)
    {
        foreach (NoteData noteData in CurrentSave.notes)
        {
            if (noteData.anchorId == anchorId)
            {
                noteData.noteText = noteText;
                save();
                return;
            }
        }

        NoteData newNoteData = new NoteData(anchorId, noteText);
        CurrentSave.notes.Add(newNoteData);

        save();
    }

    public void removeNote(string anchorId)
    {
        NoteData noteToDelete = null;


        foreach (NoteData noteData in CurrentSave.notes)
        {
            if (noteData.anchorId == anchorId)
            {
                noteToDelete = noteData;
            }
        }

        if (noteToDelete != null)
        {
            CurrentSave.notes.Remove(noteToDelete);
            save();
        }
        else
        {
            Debug.LogError("Can't find anchorId " + anchorId + " to delete.");
        }
    }

    public string getNoteText(string anchorId)
    {
        foreach (NoteData noteData in CurrentSave.notes)
        {
            if (noteData.anchorId == anchorId)
            {
                return noteData.noteText;
            }
        }
        return null;
    }

    public List<string> getSavedAnchorIds()
    {
        List<string> anchorIds = new List<string>();
        foreach (NoteData noteData in CurrentSave.notes)
        {
            if (!string.IsNullOrEmpty(noteData.anchorId))
            {
                anchorIds.Add(noteData.anchorId);
            }
        }
        return anchorIds;
    }

    public void init()
    {
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, _saveFolderPath);
        if (!Directory.Exists(saveDirectoryPath))
        {
            Directory.CreateDirectory(saveDirectoryPath);
        }
        loadCurrentSave();
    }

    public void save()
    {
        //Debug.Log("Saving State...\nanchorid : " + CurrentSave.anchorId + "\nnoteText : " + CurrentSave.noteText);

        string saveFileName = Path.Combine(_saveFolderPath, CurrentSave.id + _saveSuffix);
        string currentSavePath = Path.Combine(Application.persistentDataPath, saveFileName);
        string jsonString = JsonUtility.ToJson(CurrentSave);

        using (StreamWriter streamWriter = File.CreateText(currentSavePath))
        {
            streamWriter.Write(jsonString);
        }
    }

    public SaveState load(string id)
    {
        string saveFileName = Path.Combine(_saveFolderPath, id + _saveSuffix);
        string currentSavePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(currentSavePath))
        {
            return null;
        }

        using (StreamReader streamReader = File.OpenText(currentSavePath))
        {
            string jsonString = streamReader.ReadToEnd();
            CurrentSave = JsonUtility.FromJson<SaveState>(jsonString);
            return CurrentSave;
        }
    }

    public void delete(string id)
    {
        string saveFileName = Path.Combine(_saveFolderPath, id + _saveSuffix);
        string savePathToDelete = Path.Combine(Application.persistentDataPath, saveFileName);

        if (File.Exists(savePathToDelete))
        {
            File.Delete(savePathToDelete);
        }
    }

    private bool isSaveStateValid(SaveState saveState)
    {
        return saveState != null && saveState.notes != null;
    }
}

[Serializable]
public class SaveState
{
    public string id;
    public string appVersion;
    public List<NoteData> notes;
}

[Serializable]
public class NoteData
{
    public string anchorId;
    public string noteText;

    public NoteData(string anchorId, string noteText)
    {
        this.anchorId = anchorId;
        this.noteText = noteText;
    }
}

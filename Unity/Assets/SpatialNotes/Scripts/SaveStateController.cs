using UnityEngine;
using System;
using System.IO;

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
    }

    public bool loadCurrentSave()
    {
        bool newState = true;

        load(_saveId);
        newState = !isSaveStateValid(CurrentSave);

        // upgrade format of save state if necessary...
        if (CurrentSave == null || CurrentSave.appVersion != Application.version)
        {
            createNewSave();
        }

        return newState;
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
        Debug.Log("Saving State...\nanchorid : " + CurrentSave.anchorId + "\nnoteText : " + CurrentSave.noteText);

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
        if (saveState != null)
        {
            Debug.Log("Save State Found!\nanchorid : " + saveState.anchorId + "\nnoteText : " + saveState.noteText);
        }

        return saveState != null && !string.IsNullOrEmpty(saveState.anchorId);
    }
}

[Serializable]
public class SaveState
{
    public string id;
    public string anchorId;
    public string noteText;
    public string appVersion;
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}

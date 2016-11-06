using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour {

    public static bool haveInitialized = false;

	// Use this for initialization
	void Start () {
        StartCoroutine(loadPlayerPrefs());
	}

    public IEnumerator loadPlayerPrefs() {
        // Wait for game instance being assigned and input methods scanned
        while (Game.instance == null && !Misc.haveScannedInputs) {
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // Controller default value
        List<string> availableInputNames = Misc.getInputMethodNames();
        Text controllerText = Misc.GetMenuValueTextForKey("Options:controller");
        if (controllerText != null) {
            controllerText.text = availableInputNames[0];
        }

        string storedKeysStr = PlayerPrefs.GetString("Menu:storedKeys");
       	if (storedKeysStr != null && storedKeysStr != "") {
            // Get all MenuValue items
            MenuValue[] menuValueObjects = Resources.FindObjectsOfTypeAll<MenuValue>();
            Dictionary<string, MenuValue> menuValueObjectsWithKeys = new Dictionary<string, MenuValue>();
            foreach (MenuValue menuValueObject in menuValueObjects) {
                if (!menuValueObjectsWithKeys.ContainsKey(menuValueObject.key)) {
                    menuValueObjectsWithKeys.Add(menuValueObject.key, menuValueObject);
                }
            }

	        string[] storedKeys = storedKeysStr.Split(',');
            foreach (string storedKey in storedKeys) {
                // Find MenuValue object and set the stored value in it
                if (menuValueObjectsWithKeys.ContainsKey(storedKey)) {
                    MenuValue menuValueObject = menuValueObjectsWithKeys[storedKey];
                    object value;
                    if (menuValueObject.formatType == MenuValue.FORMAT_TYPES.Float) {
                        value = PlayerPrefs.GetFloat(storedKey);
                    } else if (menuValueObject.formatType == MenuValue.FORMAT_TYPES.Integer) {
                        value = PlayerPrefs.GetInt(storedKey);
                    } else if (menuValueObject.formatType == MenuValue.FORMAT_TYPES.String) {
                        value = PlayerPrefs.GetString(storedKey);
                    } else {
                        continue;
                    }

                    // Sanity check for controller
                    if (storedKey == "Options:controller") {
                        if (!availableInputNames.Contains((string)value)) {
                            continue;
                        }
                    }

                    menuValueObject.setValue(value);
                }
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);

        Menu.haveInitialized = true;
    }
}

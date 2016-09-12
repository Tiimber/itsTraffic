using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour {

	// Use this for initialization
	void Start () {
		loadPlayerPrefs();
	}

    public void loadPlayerPrefs() {
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
                    menuValueObject.setValue(value);
                }
            }
        }
    }
}

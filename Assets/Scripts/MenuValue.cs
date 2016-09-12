using UnityEngine;
using UnityEngine.UI;

public class MenuValue : MonoBehaviour {

    public enum FORMAT_TYPES {
        String,
        Float,
        Integer
    };

    public enum INPUT_TYPES {
        Slider,
        Text
    };

    public FORMAT_TYPES formatType;
    public INPUT_TYPES inputType;

    public string key;

    public void setValue(object value) {
        switch (formatType) {
            case FORMAT_TYPES.String:
                setStringValue((string) value);
            	break;
            case FORMAT_TYPES.Float:
                setFloatValue((float) value);
    	        break;
            case FORMAT_TYPES.Integer:
                setIntValue((int) value);
	            break;
        }
    }

    private void setStringValue(string value) {
        if (inputType == INPUT_TYPES.Text) {
            Text textObject = GetComponent<Text>();
            textObject.text = value;
        }
        // TODO - More?
    }

    private void setFloatValue(float value) {
        switch (inputType) {
            case INPUT_TYPES.Slider:
                Slider slider = GetComponent<Slider> ();
                slider.value = value;
                break;
        }
    }

    private void setIntValue(int value) {
        if (inputType == INPUT_TYPES.Slider) {
            setFloatValue((float)value);
        }
        // TODO - More?
    }
}

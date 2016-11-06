public class Joystick {
    private static string mouseAndKeyboardText = "Keyboard & Mouse";
    private static string controllerUnavailableText = "Controller unavailable";

    private int index;
    private string name;

    public Joystick(int index, string name) {
        this.index = index;
        this.name = name;

        setupController();
    }

    private void setupController() {
        switch (name) {
            case "Microsoft Xbox One Wired Controller":
                // TODO - Standard button setups?
                break;

        }
    }

    public string getName() {
        return name;
    }

    public static string GetMouseAndKeyboardName() {
        return mouseAndKeyboardText;
    }

    public static string GetUnavailableName() {
        return controllerUnavailableText;
    }

}

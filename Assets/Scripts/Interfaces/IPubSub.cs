using System.Collections;

public interface IPubSub {

	PROPAGATION onMessage (string message, object data);

}

public enum PROPAGATION {
	DEFAULT,
	STOP_IMMEDIATELY,
	STOP_AFTER_SAME_TYPE,
	CONTINUE_WITH_OTHER_TYPES
};

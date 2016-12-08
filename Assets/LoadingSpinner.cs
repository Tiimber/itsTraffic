using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSpinner : MonoBehaviour, IPubSub {

    private static string SUBSCRIPTIONS_PREFIX = "LoadingSpinner:";
    private static string SUBSCRIPTIONS_START_SUFFIX = ":Start";
    private static string SUBSCRIPTIONS_STOP_SUFFIX = ":Stop";
    private static string SUBSCRIPTIONS_PAUSE_SUFFIX = ":Pause";

    public int slowdown = 1;
	public string id;
    public List<RawImage> spinnerImages;

    private bool running = false;
    private bool show = false;
    private int frame = 0;
    private int spinnerFrame = 0;


    void Awake () {
        PubSub.subscribe(SUBSCRIPTIONS_PREFIX + id + SUBSCRIPTIONS_START_SUFFIX, this);
        PubSub.subscribe(SUBSCRIPTIONS_PREFIX + id + SUBSCRIPTIONS_STOP_SUFFIX, this);
        PubSub.subscribe(SUBSCRIPTIONS_PREFIX + id + SUBSCRIPTIONS_PAUSE_SUFFIX, this);
    }

	// Use this for initialization
	void Start () {
        hideAll();
	}

    private void hideAll() {
        foreach (RawImage spinnerImage in spinnerImages) {
            spinnerImage.gameObject.SetActive(false);
        }
    }

	// Update is called once per frame
	void Update () {
		spinnerImages[spinnerFrame].gameObject.SetActive(false);
        if (running) {
            frame++;
            if (frame > slowdown) {
                spinnerFrame = (spinnerFrame + 1) % spinnerImages.Count;
                frame -= slowdown;
            }
        }
		spinnerImages[spinnerFrame].gameObject.SetActive(show);
	}

    public PROPAGATION onMessage(string message, object data) {
        if (message.EndsWith(SUBSCRIPTIONS_START_SUFFIX)) {
            running = true;
            show = true;
            return PROPAGATION.STOP_AFTER_SAME_TYPE;
        } else if (message.EndsWith(SUBSCRIPTIONS_STOP_SUFFIX)) {
            running = false;
            show = false;
			hideAll();
            return PROPAGATION.STOP_AFTER_SAME_TYPE;
        } else if (message.EndsWith(SUBSCRIPTIONS_PAUSE_SUFFIX)) {
            running = !running;
            return PROPAGATION.STOP_AFTER_SAME_TYPE;
        }
        return PROPAGATION.DEFAULT;
    }

    public static void StartSpinner(string spinnerId) {
        PubSub.publish(SUBSCRIPTIONS_PREFIX + spinnerId + SUBSCRIPTIONS_START_SUFFIX);
    }

    public static void StopSpinner(string spinnerId) {
        PubSub.publish(SUBSCRIPTIONS_PREFIX + spinnerId + SUBSCRIPTIONS_STOP_SUFFIX);
    }

    public static void PauseSpinner(string spinnerId) {
        PubSub.publish(SUBSCRIPTIONS_PREFIX + spinnerId + SUBSCRIPTIONS_PAUSE_SUFFIX);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class Achievements {
    private static string SECRET_LABEL = "Secret achievement";
    private static string SECRET_SUBLABEL = "???";

    private static int totalAchievementPoints = 1000;
    private static List<AchievementBase> achievements = new List<AchievementBase>(){
        // Achievement (Achievement title, Achievement subtitle, Storage label, Threshold amount, Points, operator && || secret)
        { new AchievementFloat ("Beginner", "Played your first minute", "AccumulatedData:Elapsed Time", 60f, 10) },
        { new AchievementFloat ("Getting the hang of it", "Played for an hour", "AccumulatedData:Elapsed Time", 3600f, 30)},
        { new AchievementFloat ("Get a life", "Played 24 hours", "AccumulatedData:Elapsed Time", 86400f, 50)},
        { new AchievementFloat ("Herder", "Guided 100 humans to their goals", "AccumulatedData:Humans reached goal", 100f, 10)},
        { new AchievementFloat ("Traffic controller", "250 vehicles reached their goal", "AccumulatedData:Vehicles reached goal", 250f, 10)},
        { new AchievementFloat ("Oooh, they flash", "Toggled 500 traffic lights", "AccumulatedData:Manual traffic light switches", 500f, 10, secret: true)},
        { new AchievementFloat ("Walkathon", "People have walked 10km", "AccumulatedData:People walking distance", 10000f, 10)},
        { new AchievementFloat ("Remember them all?", "Have seen 5000 people", "AccumulatedData:Total # of people", 5000f, 10, secret: true)},
        { new AchievementFloat ("Punchbuggy red", "Have seen 10000 cars", "AccumulatedData:Total # of vehicles", 10000f, 10, secret: true)},
        { new AchievementFloat ("Terminator", "Wrecked 500 cars", "AccumulatedData:Vehicle crashes", 500f, 10, secret: true)},
        { new AchievementFloat ("Big carbon footprint", "Let out a lot of emission", "AccumulatedData:Vehicle:emission", 1000f, 10)},
        { new AchievementFloat ("Where are we, China?", "Emission overload", "AccumulatedData:Vehicle:emission", 5000f, 30, secret: true)},
        { new AchievementFloat ("Irritation", "200 flashed headlights", "AccumulatedData:Vehicle:flash headlight", 200f, 30, secret: true)},
        { new AchievementFloat ("Roadrage", "500 honks from vehicles" ,"AccumulatedData:Vehicle:honk", 500f, 30, secret: true)},
        { new AchievementFloat ("Welcome to 1983", "Played with lowest graphics quality", "Options:graphics_quality", 0f, 10, op: Operator.EQ, secret: true)},

        { new AchievementInt ("Better than nothing", "Got 1 star on a level", "AccumulatedData:Stars:1", 1, 1)},
        { new AchievementInt ("Almost there", "Got 2 stars on a level", "AccumulatedData:Stars:2", 1, 2)},
        { new AchievementInt ("You've done it", "Got 3 stars on a level", "AccumulatedData:Stars:3", 1, 3)},
        { new AchievementInt ("Level master", "Got the hidden 4th star on a level", "AccumulatedData:Stars:4", 1, 10, secret: true)},
        { new AchievementInt ("Is this even possible?", "Ok... there is 5 stars on some levels", "AccumulatedData:Stars:5", 1, 20, secret: true)},
        { new AchievementInt ("Twinkle, twinkle...", "Got a total of 10 stars", "AccumulatedData:TotalStars", 10, 10)},
        { new AchievementInt ("Gold star", "Got a total of 100 stars", "AccumulatedData:TotalStars", 100, 20, secret: true)},
        { new AchievementInt ("Sometimes you have to fail", "Lost 10 games", "AccumulatedData:WinLose:lose", 10, 10)},
        { new AchievementInt ("A loser is you", "Lost 50 games", "AccumulatedData:WinLose:lose", 50, 10, secret: true)},
        { new AchievementInt ("You win", "Won 10 games", "AccumulatedData:WinLose:win", 10, 10)},
        { new AchievementInt ("You're on a streak", "Won 50 games", "AccumulatedData:WinLose:win", 50, 10)},
        { new AchievementInt ("A winner is you", "Won 500 games", "AccumulatedData:WinLose:win", 500, 30, secret: true)},
        { new AchievementInt ("Never though anyone would play this much", "Won 2000 games", "AccumulatedData:WinLose:win", 2000, 50, secret: true)},

        {
            new AchievementCombo ("Soundeffects!", "Playing with only soundeffects (no music)", new List<AchievementBase>(){
                new AchievementFloat (null, null, "Options:music_vol", 0f, -1, op: Operator.EQ, isInner: true),
                new AchievementFloat (null, null, "Options:ambient_vol", 0f, -1, op: Operator.EQ, isInner: true),
                new AchievementFloat (null, null, "Options:sound_vol", 0f, -1, op: Operator.GT, isInner: true),
            }, 10, secret: true)
        }

        // TODO - Uploaded 1, 5, 25 level(s)
    };

    public static void InitAchievements() {
        // TODO - Redistribute points so they fit the system they're on

    }

    public static List<Tuple3<string, string, int>> GetFulfilledAchievements() {
        List<Tuple3<string, string, int>> fulfilled = new List<Tuple3<string, string, int>>();
        foreach (AchievementBase achievement in achievements) {
            if (achievement.fulfilled) {
                fulfilled.Add(new Tuple3<string, string, int>(achievement.title, achievement.subtitle, achievement.points));
            }
        }
        return fulfilled;
    }

    public static List<Tuple3<string, string, int>> GetNonSecretUnfulfilledAchievements() {
        List<Tuple3<string, string, int>> fulfilled = new List<Tuple3<string, string, int>>();
        foreach (AchievementBase achievement in achievements) {
            if (!achievement.fulfilled && !achievement.secret) {
                fulfilled.Add(new Tuple3<string, string, int>(achievement.title, achievement.subtitle, achievement.points));
            }
        }
        return fulfilled;
    }

    public static List<Tuple3<string, string, int>> GetSecretUnfulfilledAchievements() {
        List<Tuple3<string, string, int>> fulfilled = new List<Tuple3<string, string, int>>();
        foreach (AchievementBase achievement in achievements) {
            if (!achievement.fulfilled && achievement.secret) {
                fulfilled.Add(new Tuple3<string, string, int>(Achievements.SECRET_LABEL, Achievements.SECRET_SUBLABEL, achievement.points));
            }
        }
        return fulfilled;
    }

    public static void testAll(bool init = false) {
        if (init) {
            Achievements.InitAchievements ();
        }

        foreach (AchievementBase achievement in achievements) {
            bool becameFulfilled = achievement.test();
            if (becameFulfilled && !init) {
//                Debug.Log("Achievement met: " + achievement.Key);
                // TODO - New achievement met - publish (+register listener)
                // TODO - This one could use eg. Xbox/PS/Steam achievement system (or Gamecenter for IOS)...
                // TODO - If none, maybe we should do our own as well?
            }
        }
    }

    private enum Operator {
        EQ,
        GT,
        GTE
    }

    private enum Type {
        INTEGER,
        FLOAT,
        ACHIEVEMENTS
    }

    private class TypedValue {
        public float floatValue = -1f;
        public int intValue = -1;
        public Type type;

        public TypedValue(float value) {
            this.floatValue = value;
            this.type = Type.FLOAT;
        }

        public TypedValue(int value) {
            this.intValue = value;
            this.type = Type.INTEGER;
        }

        public float getValue() {
            return type == Type.FLOAT ? floatValue : (float)intValue;
        }

        public static bool operator== (TypedValue a, TypedValue b) {
            return a.getValue() == b.getValue();
        }

        public override bool Equals(object other) {
            return other.GetType() == this.GetType() && (TypedValue)other == this;
        }

        public override int GetHashCode() {
            return (type.ToString() + floatValue + intValue).GetHashCode();
        }

        public static bool operator!= (TypedValue a, TypedValue b) {
            return a.getValue() != b.getValue();
        }

        public static bool operator>= (TypedValue a, TypedValue b) {
            return a.getValue() >= b.getValue();
        }

        public static bool operator> (TypedValue a, TypedValue b) {
            return a.getValue() > b.getValue();
        }

        public static bool operator<= (TypedValue a, TypedValue b) {
            return a.getValue() <= b.getValue();
        }

        public static bool operator< (TypedValue a, TypedValue b) {
            return a.getValue() < b.getValue();
        }
    }

    private abstract class AchievementBase {
        private static string FulfilledHashesKey = "Achievements:Fulfilled";
        private static string FulfilledHashesData = null;

        public string title;
        public string subtitle;
        public int points;
        public bool secret;
        public bool fulfilled = false;

        public AchievementBase(string title, string subtitle, int points, bool secret) {
            this.title = title;
            this.subtitle = subtitle;
            this.points = points;
            this.secret = secret;
        }

        protected void saveFulfilled() {
            int hashCode = this.getHash ();
            if (!FulfilledHashesData.Contains(";" + hashCode + ";")) {
                FulfilledHashesData += hashCode + ";";
                PlayerPrefs.SetString(FulfilledHashesKey, FulfilledHashesData);
                PlayerPrefs.Save();
            }
        }

        protected void checkIfPreviouslyFulfilled() {
            if (FulfilledHashesData == null) {
                FulfilledHashesData = PlayerPrefs.HasKey(FulfilledHashesKey) ? PlayerPrefs.GetString(FulfilledHashesKey) : ";";
            }

            int hashCode = this.getHash ();
            if (FulfilledHashesData.Contains(";" + hashCode + ";")) {
//                Debug.Log("Fulfilled already: " + key + " " + amount.getValue());
                fulfilled = true;
            }
        }

        public int getHash() {
            return (title + subtitle + points.ToString()).GetHashCode();
        }

        public abstract bool test();
    }

    private abstract class Achievement : AchievementBase {
        public string key;
        public TypedValue amount;
        public Operator op;
        public bool isInner;
        public Type type;

        public Achievement(string title, string subtitle, string key, int points, bool secret, Operator op, bool isInner) : base(title, subtitle, points, secret) {
            this.key = key;
            this.op = op;
            this.isInner = isInner;
        }

        public override bool test() {
            TypedValue registeredValue = new TypedValue(amount.type == Type.FLOAT ? PlayerPrefs.GetFloat(key) : PlayerPrefs.GetInt(key));
//            Debug.Log("Test: " + key + " ? " + fulfilled + " " + registeredValue.getValue() + " / " + amount.getValue());
            if (!fulfilled) {
                if (
                    (op == Operator.GTE && registeredValue >= amount) ||
                    (op == Operator.GT && registeredValue > amount) ||
                    (op == Operator.EQ && registeredValue == amount)
                ) {
                    fulfilled = true;
                    Debug.Log("FULLFILLED!");
                    if (!isInner) {
                        saveFulfilled ();
                    }
                    return true;
                }
            }
            return false;
        }
    }

    private class AchievementInt : Achievement {
        public AchievementInt(string title, string subtitle, string key, int amount, int points, bool secret = false, Operator op = Operator.GTE, bool isInner = false) : base(title, subtitle, key, points, secret, op, isInner) {
            this.amount = new TypedValue(amount);
            if (!isInner) {
                checkIfPreviouslyFulfilled ();
            }
        }
    }

    private class AchievementFloat : Achievement {
        public AchievementFloat(string title, string subtitle, string key, float amount, int points, bool secret = false, Operator op = Operator.GTE, bool isInner = false) : base(title, subtitle, key, points, secret, op, isInner) {
            this.amount = new TypedValue(amount);
            if (!isInner) {
                checkIfPreviouslyFulfilled ();
            }
        }
    }

    private class AchievementCombo : AchievementBase {
        public List<AchievementBase> parts;
        public AchievementCombo(string title, string subtitle, List<AchievementBase> parts, int points, bool secret = false) : base(title, subtitle, points, secret) {
            this.parts = parts;
            checkIfPreviouslyFulfilled ();
        }

        public override bool test() {
//            Debug.Log("Test: " + title + " ; " + subtitle + " ? " + fulfilled);
            if (!fulfilled) {
                bool allFulfilled = true;
                foreach (AchievementBase part in parts) {
                    if (!part.fulfilled && !part.test()) {
                        allFulfilled = false;
                        break;
                    }
                }
                if (allFulfilled) {
                    fulfilled = true;
                    Debug.Log("FULLFILLED!");
                    saveFulfilled ();
                    return true;
                }
            }
            return false;
        }
    }

}
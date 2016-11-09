using System.Collections.Generic;
using UnityEngine;

public class Achievements {
    private static string SECRET_LABEL = "Secret achievement";

    private static int totalAchievementPoints = 1000;
    private static Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>(){
        {"Played your first minute!", new AchievementFloat("AccumulatedData:Elapsed Time", 60f, 10)},
        {"Played for an hour!", new AchievementFloat("AccumulatedData:Elapsed Time", 3600f, 30)},
        {"Guided 100 humans!", new AchievementFloat("AccumulatedData:Humans reached goal", 100f, 10)},
        {"250 vehicles reached their goal!", new AchievementFloat("AccumulatedData:Vehicles reached goal", 250f, 10)},
        {"Toggled 500 traffic lights!", new AchievementFloat("AccumulatedData:Manual traffic light switches", 500f, 10, secret: true)},
        {"People have walked 10km!", new AchievementFloat("AccumulatedData:People walking distance", 10000f, 10)},
        {"Have seen 5000 people!", new AchievementFloat("AccumulatedData:Total # of people", 5000f, 10, secret: true)},
        {"Have seen 10000 cars!", new AchievementFloat("AccumulatedData:Total # of vehicles", 10000f, 10, secret: true)},
        {"Wrecked 500 cars!", new AchievementFloat("AccumulatedData:Vehicle crashes", 500f, 10, secret: true)},
        {"Big carbon footprint!", new AchievementFloat("AccumulatedData:Vehicle:emission", 1000f, 10)},
        {"Huge carbon footprint!", new AchievementFloat("AccumulatedData:Vehicle:emission", 5000f, 30)},
        {"200 flashed headlights!", new AchievementFloat("AccumulatedData:Vehicle:flash headlight", 200f, 30, secret: true)},
        {"500 honks!", new AchievementFloat("AccumulatedData:Vehicle:honk", 500f, 30, secret: true)},
        {"Played with lowest graphics quality!", new AchievementFloat("Options:graphics_quality", 0f, 10, op: Operator.EQ, secret: true)},

        {"Got 1 Star!", new AchievementInt("AccumulatedData:Stars:1", 1, 1)},
        {"Got 2 Stars!", new AchievementInt("AccumulatedData:Stars:2", 1, 2)},
        {"Got 3 Stars!", new AchievementInt("AccumulatedData:Stars:3", 1, 3)},
        {"Got hidden 4th star!", new AchievementInt("AccumulatedData:Stars:4", 1, 10, secret: true)},
        {"Ok, there is 5 stars!", new AchievementInt("AccumulatedData:Stars:5", 1, 20, secret: true)},
        {"Lost 10 games!", new AchievementInt("AccumulatedData:WinLose:lose", 10, 10)},
        {"Lost 50 games!", new AchievementInt("AccumulatedData:WinLose:lose", 50, 10, secret: true)},
        {"Won 10 games!", new AchievementInt("AccumulatedData:WinLose:win", 10, 10)},
        {"Won 50 games!", new AchievementInt("AccumulatedData:WinLose:win", 50, 10)},
        {"Won 500 games!", new AchievementInt("AccumulatedData:WinLose:win", 500, 10, secret: true)},
        {"Won 2000 games!", new AchievementInt("AccumulatedData:WinLose:win", 2000, 10, secret: true)},

        // TODO - Uploaded 1, 5, 25 level(s)
    };

    public static void InitAchievements() {
        // TODO - Redistribute points so they fit the system they're on

    }

    public static List<KeyValuePair<string, int>> GetFulfilledAchievements() {
        List<KeyValuePair<string, int>> fulfilled = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, Achievement> achievement in achievements) {
            if (achievement.Value.fulfilled) {
                fulfilled.Add(new KeyValuePair<string, int>(achievement.Key, achievement.Value.points));
            }
        }
        return fulfilled;
    }

    public static List<KeyValuePair<string, int>> GetNonSecretUnfulfilledAchievements() {
        List<KeyValuePair<string, int>> fulfilled = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, Achievement> achievement in achievements) {
            if (!achievement.Value.fulfilled && !achievement.Value.secret) {
                fulfilled.Add(new KeyValuePair<string, int>(achievement.Key, achievement.Value.points));
            }
        }
        return fulfilled;
    }

    public static List<KeyValuePair<string, int>> GetSecretUnfulfilledAchievements() {
        List<KeyValuePair<string, int>> fulfilled = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, Achievement> achievement in achievements) {
            if (!achievement.Value.fulfilled && achievement.Value.secret) {
                fulfilled.Add(new KeyValuePair<string, int>(Achievements.SECRET_LABEL, achievement.Value.points));
            }
        }
        return fulfilled;
    }

    public static void testAll(bool init = false) {
        if (init) {
            Achievements.InitAchievements ();
        }

        foreach (KeyValuePair<string, Achievement> achievement in achievements) {
            bool becameFullfilled = achievement.Value.test();
            if (becameFullfilled && !init) {
//                Debug.Log("Achievement met: " + achievement.Key);
                // TODO - New achievement met - publish (+register listener)
                // TODO - This one could use eg. Xbox/PS/Steam achievement system (or Gamecenter for IOS)...
                // TODO - If none, maybe we should do our own as well?
            }
        }
    }

    private enum Operator {
        EQ,
        GTE
    }

    private enum Type {
        INTEGER,
        FLOAT
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

        public static bool operator<= (TypedValue a, TypedValue b) {
            return a.getValue() <= b.getValue();
        }
    }

    private abstract class Achievement {
        private static string FullfilledHashesKey = "Achievements:Fullfilled";
        private static string FullfilledHashesData = null;

        public string key;
        public TypedValue amount;
        public int points;
        public Operator op;
        public Type type;
        public bool secret;
        public bool fulfilled = false;

        public Achievement(string key, int points, bool secret, Operator op) {
            this.key = key;
            this.points = points;
            this.op = op;
            this.secret = secret;
        }

        public bool test() {
            TypedValue registeredValue = new TypedValue(amount.type == Type.FLOAT ? PlayerPrefs.GetFloat(key) : PlayerPrefs.GetInt(key));
//            Debug.Log("Test: " + key + " ? " + fulfilled + " " + registeredValue.getValue() + " / " + amount.getValue());
            if (!fulfilled) {
                if (
                    (op == Operator.GTE && registeredValue >= amount) ||
                    (op == Operator.EQ && registeredValue == amount)
                ) {
                    fulfilled = true;
                    Debug.Log("FULLFILLED!");
                    saveFullfilled();
                    return true;
                }
            }
            return false;
        }

        private void saveFullfilled() {
            int hashCode = this.getHash ();
            if (!FullfilledHashesData.Contains(";" + hashCode + ";")) {
                FullfilledHashesData += hashCode + ";";
                PlayerPrefs.SetString(FullfilledHashesKey, FullfilledHashesData);
                PlayerPrefs.Save();
            }
        }

        protected void checkIfPreviouslyFullfilled() {
            if (FullfilledHashesData == null) {
                FullfilledHashesData = PlayerPrefs.HasKey(FullfilledHashesKey) ? PlayerPrefs.GetString(FullfilledHashesKey) : ";";
            }

            int hashCode = this.getHash ();
            if (FullfilledHashesData.Contains(";" + hashCode + ";")) {
//                Debug.Log("Fulfilled already: " + key + " " + amount.getValue());
                fulfilled = true;
            }
        }

        public int getHash() {
            return (key + amount.getValue() + op.ToString()).GetHashCode();
        }
    }

    private class AchievementInt : Achievement {
        public AchievementInt(string key, int amount, int points, bool secret = false, Operator op = Operator.GTE) : base(key, points, secret, op) {
            this.amount = new TypedValue(amount);
            checkIfPreviouslyFullfilled();
        }
    }

    private class AchievementFloat : Achievement {
        public AchievementFloat(string key, float amount, int points, bool secret = false, Operator op = Operator.GTE) : base(key, points, secret, op) {
            this.amount = new TypedValue(amount);
            checkIfPreviouslyFullfilled();
        }
    }

}
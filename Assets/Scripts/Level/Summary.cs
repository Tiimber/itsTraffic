using System.Collections.Generic;

public class Summary {
    public string name;
    public string type;
    public int pointsBefore;
    public Objectives objectives;
    public List<PointCalculator.Point> alreadyIncluded;
    public List<PointCalculator.Point> notYetIncluded;
    public int numberOfStars;

    public bool newHighscore;
    public bool failedMission;
    public bool havePlayedHighscoreSound = false;
    public bool havePlayedFailedSound = false;

    public Summary(string name, string type, int pointsBefore, Objectives objectives, List<PointCalculator.Point> alreadyIncluded, List<PointCalculator.Point> notYetIncluded, int numberOfStars, bool newHighscore) {
        this.name = name;
        this.type = type;
        this.pointsBefore = pointsBefore;
        this.objectives = objectives;
        this.alreadyIncluded = alreadyIncluded;
        this.notYetIncluded = notYetIncluded;
        this.numberOfStars = numberOfStars;
        this.newHighscore = newHighscore;
        this.failedMission = type == "lose";
    }
}

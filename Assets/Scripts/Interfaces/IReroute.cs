using System.Collections.Generic;

public interface IReroute {
    void pauseMovement();
    List<Pos> getPath();
    void setPath(List<Pos> path);
    void resumeMovement();
}

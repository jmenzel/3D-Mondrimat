using UnityEngine;
public interface ITurnable
{
    void TurnCounterClockwise();
    void TurnClockwise();

    bool InRange(Vector3 circleCenter);
}

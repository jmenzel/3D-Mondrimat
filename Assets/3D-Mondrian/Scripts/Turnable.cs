using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turnable : MonoBehaviour, ITurnable
{

    public GameObject[] turnees;
    public Vector3[] positions;

    public Vector3 BigSize;// = new Vector3(0.7f, 0.7f, 0.5f);
    public Vector3 SmallSize;// = new Vector3(0.5f, 0.5f, 0.5f);

    public float MinX = -2f;
    public float MaxX = 0f;

    public float smoothPositionChangeFactor = 1f;

    private bool _circleInProgress;

    public void TurnCounterClockwise()
    {
        if (_circleInProgress) return;
        _circleInProgress = true;

        var tmpArray = new GameObject[turnees.Length];

        tmpArray[turnees.Length -1] = turnees[0];

        for (var i = 1; i < turnees.Length; ++i)
        {
            tmpArray[i - 1] = turnees[i];
        }
        turnees = tmpArray;

        RepositionGameObjects(turnees);
    }

    public void TurnClockwise()
    {
        if (_circleInProgress) return;
        _circleInProgress = true;

        var tmpArray = new GameObject[turnees.Length];
        tmpArray[0] = turnees[turnees.Length - 1];

        for (var i = 1; i < turnees.Length; ++i)
        {
            tmpArray[i] = turnees[i - 1];
        }

        turnees = tmpArray;

        RepositionGameObjects(turnees);
    }
    
    private void RepositionGameObjects(IList<GameObject> gameObjects)
    {
        for (var i = 0; i < gameObjects.Count; ++i)
        {
            StartCoroutine(MoveToPosition(gameObjects[i].transform, positions[i], smoothPositionChangeFactor,
                (transf) => { transf.localScale = SmallSize; },
                (transf, endPosition) =>
                {
                    if (endPosition == positions[0])
                    {
                        transf.localScale = BigSize;
                    }


                    if (endPosition == positions[positions.Length - 1])
                    {
                        _circleInProgress = false;
                    }
                }));
        }
    }



    public bool InRange(Vector3 circleCenter)
    {
        return (circleCenter.x > MinX && circleCenter.x < MaxX);
    }

    IEnumerator MoveToPosition(Transform transform, Vector3 newPosition, float time,Action<Transform> beforeAction = null, Action<Transform, Vector3> endPositionReachedAction = null)
    {
        var elapsedTime = 0f;
        var startingPos = transform.localPosition;

        if (beforeAction != null)
        {
            beforeAction.Invoke(transform);
        }

        while (elapsedTime < time)
        {
            transform.localPosition = Vector3.Lerp(startingPos, newPosition, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = newPosition;

        if (endPositionReachedAction != null)
        {
            endPositionReachedAction.Invoke(transform, newPosition);
        }
    }
}

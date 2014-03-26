using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class Turnable : MonoBehaviour, ITurnable
{

    public GameObject[] turnees;
    public Vector3[] positions;
    public GameObject[] notificationSubscriber;

    public Vector3 BigSize;// = new Vector3(0.7f, 0.7f, 0.5f);
    public Vector3 SmallSize;// = new Vector3(0.5f, 0.5f, 0.5f);

    public float MinX = -2f;
    public float MaxX = 0f;

    public float smoothPositionChangeFactor = 1f;

    public string activeTag;
    public string defaultTag;

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
            StartCoroutine(MoveToPosition(gameObjects[i], positions[i], smoothPositionChangeFactor,
                (transf) => { transf.localScale = SmallSize; },
                (gameObj, endPosition) =>
                {
                    var transf = gameObj.transform;

                    if (endPosition == positions[0])
                    {
                        transf.localScale = BigSize;
                        gameObj.tag = activeTag;

                        foreach (var turnNotification in notificationSubscriber.Select(x => (ITurnNotification)x.GetComponent(typeof(ITurnNotification))))
                        {
                            turnNotification.Notify(gameObj);
                        }
                    }
                    else
                    {
                        gameObj.tag = defaultTag;
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

    static IEnumerator MoveToPosition(GameObject gameObj, Vector3 newPosition, float time, Action<Transform> beforeAction = null, Action<GameObject, Vector3> endPositionReachedAction = null)
    {
        var transf = gameObj.transform;
        var elapsedTime = 0f;
        var startingPos = transf.localPosition;

        if (beforeAction != null)
        {
            beforeAction.Invoke(transf);
        }

        while (elapsedTime < time)
        {
            transf.localPosition = Vector3.Lerp(startingPos, newPosition, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transf.localPosition = newPosition;

        if (endPositionReachedAction != null)
        {
            endPositionReachedAction.Invoke(gameObj, newPosition);
        }
    }
}

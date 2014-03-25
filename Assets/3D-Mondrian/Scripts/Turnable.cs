﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turnable : MonoBehaviour, ITurnable
{

    public GameObject[] turnees;
    public Vector3[] positions = new Vector3[]
    {
        new Vector3(0.64f, 1.5182f, -3.21f),
        new Vector3(1f, 0.6f, -3.21f),
        new Vector3(1, 0, -3.21f),
        new Vector3(0.25f, 0, -3.21f),
        new Vector3(0.25f, 0, -3.21f),  
    };

    public Vector3 BigSize = new Vector3(0.7f, 0.7f, 0.5f);
    public Vector3 SmallSize = new Vector3(0.5f, 0.5f, 0.5f);

    public float MinX = -2f;
    public float MaxX = 0f;

    public float smoothPositionChangeFactor = 1f;

    public void TurnCounterClockwise()
    {
        throw new System.NotImplementedException();

    }

    public void TurnClockwise()
    {
        var tmpArray = new GameObject[turnees.Length];
        tmpArray[0] = turnees[turnees.Length - 1];

        for (var i = 1; i < turnees.Length; ++i)
        {
            tmpArray[i] = turnees[i - 1];
        }

        turnees = tmpArray;

        foreach (var turnee in turnees)
        {
            turnee.transform.localScale = SmallSize;
        }

        for (var i = 0; i < turnees.Length; ++i)
        {
            StartCoroutine(MoveToPosition(turnees[i].transform, positions[i], smoothPositionChangeFactor, (transf) =>
            {
                if (transf.localPosition == positions[0])
                {
                    transf.localScale = BigSize;
                }
            }));
        }
    }

    public bool InRange(Vector3 circleCenter)
    {
        return (circleCenter.x > MinX && circleCenter.x < MaxX);
    }

    IEnumerator MoveToPosition(Transform transform, Vector3 newPosition, float time, Action<Transform> endPositionReachedAction = null)
    {
        var elapsedTime = 0f;
        var startingPos = transform.localPosition;
        while (elapsedTime < time)
        {
            transform.localPosition = Vector3.Lerp(startingPos, newPosition, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = newPosition;

        if (endPositionReachedAction != null)
        {
            endPositionReachedAction.Invoke(transform);
        }
    }
}

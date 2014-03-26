using UnityEngine;
using System.Collections;

public class ColorMenuTurnNotification : MonoBehaviour, ITurnNotification
{
    public void Notify(GameObject activeGameObject)
    {
        renderer.material.color = activeGameObject.renderer.material.color;
    }
}

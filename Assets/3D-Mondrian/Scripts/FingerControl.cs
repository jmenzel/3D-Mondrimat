using UnityEngine;
using System.Collections;
using Leap;

public class FingerControl : MonoBehaviour 
{

    Controller _leap;
    public GameObject Pointer;
    public float PointerMaxZ = -10;
    public float PointerMinZ = -6;
    public float PointerMaxY = 20;
    float PointerZOffset;

	// Use this for initialization
	void Start () 
    {
        _leap = new Controller();
        if (transform == null)
        {
            Debug.LogError("LeapFly must have a parent object to control");
        }

        _leap.EnableGesture(Gesture.GestureType.TYPECIRCLE);
        _leap.EnableGesture(Gesture.GestureType.TYPEKEYTAP);
        _leap.EnableGesture(Gesture.GestureType.TYPESCREENTAP);
        _leap.EnableGesture(Gesture.GestureType.TYPESWIPE);
        _leap.EnableGesture(Gesture.GestureType.TYPEINVALID);

        PointerZOffset = Pointer.transform.position.z;
	}
	
	// Update is called once per frame
    void FixedUpdate()
    {
        Frame frame = _leap.Frame();
        HandleGestures(frame.Gestures());

        HandleFrontFinger((frame.Fingers.Count > 0 && frame.Hands.Count < 2) ? frame.Fingers.Frontmost : null);
	}

    private void HandleFrontFinger(Finger finger)
    {
        if (finger == null)
        {
            if (Pointer.activeSelf)
            {
                Pointer.SetActive(false);
            }
        }
        else
        {
            if (!Pointer.activeSelf)
            {
                Pointer.SetActive(true);
            }

            //Perform action
            var calcedPos = finger.TipPosition.ToUnityScaled();
            calcedPos.z = calcedPos.z + PointerZOffset;

            var avgZ = (calcedPos.z < PointerMaxZ) ? PointerMaxZ : (calcedPos.z > PointerMinZ) ? PointerMinZ : calcedPos.z;

            var newPos = new Vector3(calcedPos.x, calcedPos.y, avgZ);

            Pointer.transform.position = Vector3.Lerp(Pointer.transform.position, newPos, 0.3F);
        }
    }









    private void HandleGestures(GestureList gestures)
    {
        for (int i = 0; i < gestures.Count; ++i)
        {
            Gesture gesture = gestures[i];


            switch (gesture.Type)
            {
                case Gesture.GestureType.TYPECIRCLE:
                    Debug.Log("Circle");
                    break;
                case Gesture.GestureType.TYPEKEYTAP:
                    Debug.Log("KeyTap - ");
                    break;
                case Gesture.GestureType.TYPESCREENTAP:
                    Debug.Log("ScreenTap");
                    break;
                case Gesture.GestureType.TYPESWIPE:
                    if (gesture.DurationSeconds < 0.10) continue;
                    Debug.Log("Swipe");
                    break;
                case Gesture.GestureType.TYPEINVALID:
                    Debug.Log("Invalid");
                    break;
                default:
                    Debug.Log("Bad gesture type");
                    break;
            }
       }
    }
}

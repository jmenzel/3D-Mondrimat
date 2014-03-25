using System;
using System.Linq;
using Leap;
using UnityEngine;

namespace Assets.Scripts
{
    public class FingerControl : MonoBehaviour 
    {

        Controller _leap;
        public GameObject pointer;
        public GameObject[] turnables;

        public float PointerMaxZ = -10;
        public float PointerMinZ = -6;
        public float PointerYOffset = -9;
        float _pointerZOffset;
        private GameObject _lastHittedObject;
        private GameObject[] _pointer;

        // Use this for initialization
        void Start () 
        {
            _leap = new Controller();
            if (transform == null)
            {
                Debug.LogError("LeapFly must have a parent object to control");
            }

            _leap.EnableGesture(Gesture.GestureType.TYPECIRCLE);
            //_leap.EnableGesture(Gesture.GestureType.TYPEKEYTAP);
            _leap.EnableGesture(Gesture.GestureType.TYPESCREENTAP);
            //_leap.EnableGesture(Gesture.GestureType.TYPESWIPE);
            _leap.EnableGesture(Gesture.GestureType.TYPEINVALID);

            _pointerZOffset = pointer.transform.position.z;

            _pointer = new[]
            {
                Instantiate(pointer, pointer.transform.position, pointer.transform.rotation) as GameObject,
                Instantiate(pointer, pointer.transform.position, pointer.transform.rotation) as GameObject
            };
        }
	
        // Update is called once per frame
        void FixedUpdate()
        {
            if (_leap == null) return;

            var frame = _leap.Frame();

            if(frame.Fingers.Count > 0 && frame.Fingers.Count < 2 && frame.Hands.Count == 1)
            {
                var finger = frame.Fingers.Frontmost;
                if (finger != null)
                {
                    HandleFinger(finger, _pointer[0]);
                }
                else
                {
                    _pointer[0].SetActive(false);
                }
                _pointer[1].SetActive(false);
            }
            else if (frame.Fingers.Count > 0 && frame.Fingers.Count < 3 && frame.Hands.Count == 2)
            {
                var fingerLeft = frame.Fingers.Leftmost;
                var fingerRight = frame.Fingers.Rightmost;

                if (fingerLeft != null) HandleFinger(fingerLeft, _pointer[0]);
                if (fingerRight != null) HandleFinger(fingerRight, _pointer[1]);
            }
            else
            {
                _pointer[0].SetActive(false);
                _pointer[1].SetActive(false);
            }


            //HandleFrontFinger((frame.Fingers.Count > 0 && frame.Hands.Count < 2) ? frame.Fingers.Frontmost : null);
            HandleGestures(frame.Gestures());
        
        }

        private void HandleFinger(Finger finger, GameObject pPointer)
        {
            if (finger == null)
            {
                if (pPointer.activeSelf)
                {
                    pPointer.SetActive(false);
                }
            }
            else
            {
                if (!pPointer.activeSelf)
                {
                    pPointer.SetActive(true);
                }

                //Perform action
                var calcedPos = finger.TipPosition.ToUnityScaled();

                calcedPos.z = calcedPos.z + _pointerZOffset;

                var avgZ = (calcedPos.z < PointerMaxZ) ? PointerMaxZ : (calcedPos.z > PointerMinZ) ? PointerMinZ : calcedPos.z;

                var newPos = new Vector3(calcedPos.x, calcedPos.y + PointerYOffset, avgZ);

                pPointer.transform.position = Vector3.Lerp(pPointer.transform.position, newPos, 0.5F);

            
                RaycastHit hit;
                var directionRay = new Ray(pPointer.transform.position, Vector3.forward);
                Debug.DrawRay(pPointer.transform.position, Vector3.forward * 10);
                if (Physics.Raycast(directionRay, out hit, 10))
                {
                    var newHittedObject = hit.collider.gameObject;

                    if (newHittedObject == null) return;
                    if (newHittedObject.tag != "Menu") return;

                    if (newHittedObject != _lastHittedObject)
                    {
                        if (_lastHittedObject != null)
                        {

                            _lastHittedObject.GetComponent<WireFrameLineRenderer>().enabled = false;
                        }

                        newHittedObject.GetComponent<WireFrameLineRenderer>().enabled = true;

                        _lastHittedObject = newHittedObject;

                        Debug.Log("I Hit something =): " + newHittedObject + " - " + newHittedObject.tag);
                    }
                }

       
        
            }
        }

        private void HandleGestures(GestureList gestures)
        {
            foreach (var gesture in gestures)
            {
                switch (gesture.Type)
                {
                    case Gesture.GestureType.TYPECIRCLE:
                        if (gesture.State != Gesture.GestureState.STATESTOP) continue;
                        var circle = new CircleGesture(gesture);

                        var circleCenter = circle.Center.ToUnityScaled();
                        var clockwise = (circle.Pointable.Direction.AngleTo(circle.Normal) <= Math.PI/2);
                
                        Debug.Log("Circle "+ ((clockwise) ? "Clockwise" : "Counter Clockwise") +" x:" + circleCenter.x);

                        foreach (var turnable in turnables.Select(x => x.GetComponent(typeof(ITurnable))).Where(x => x != null).Cast<ITurnable>().Where(turnable => turnable.InRange(circleCenter)))
                        {
                            if (clockwise) turnable.TurnClockwise();
                            else turnable.TurnCounterClockwise();
                        }

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
}

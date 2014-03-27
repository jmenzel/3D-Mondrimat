using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap;
using UnityEngine;

namespace Assets.Scripts
{
    public class FingerControl : MonoBehaviour
    {

        private Controller _leap;
        public GameObject pointer;
        public GameObject[] turnables;

        public float PointerMaxZ = -10;
        public float PointerMinZ = -6;
        public float PointerYOffset = -9;
        private float _pointerZOffset;
        private GameObject _lastHittedObject;
        private GameObject[] _pointer;

        public float circleRadiusChangeViewLimit = 90f;
        public float circleRadiusIgnoreLimit = 40f;

        private Color _savedColor;

        private Finger _activeFingerA = null;
        private Finger _activeFingerB = null;

        private List<Action<Gesture>> _registerdCircleHandler;

        private Vector3 _originCamPosition;

        private bool runCamChange;

        private RestartSceneAfterTimeout resetTimer;
        public Color highlightColor = Color.cyan;

        // Use this for initialization
        private void Start()
        {
            _leap = new Controller();
            if (transform == null)
            {
                Debug.LogError("Must have a parent object to control");
            }

            _originCamPosition = Camera.main.transform.position;
            resetTimer = Camera.main.GetComponent<RestartSceneAfterTimeout>();

            _leap.EnableGesture(Gesture.GestureType.TYPECIRCLE);
            //_leap.EnableGesture(Gesture.GestureType.TYPEKEYTAP);
            _leap.EnableGesture(Gesture.GestureType.TYPESCREENTAP);
            //_leap.EnableGesture(Gesture.GestureType.TYPESWIPE);
            //_leap.EnableGesture(Gesture.GestureType.TYPEINVALID);

            _pointerZOffset = pointer.transform.position.z;

            _pointer = new[]
            {
                Instantiate(pointer, pointer.transform.position, pointer.transform.rotation) as GameObject,
                //Instantiate(pointer, pointer.transform.position, pointer.transform.rotation) as GameObject
            };

            _registerdCircleHandler = new List<Action<Gesture>>
            {
                HandleCircleGestureForTurnables,
                HandleCircleGestureForViewChange
            };
        }


        // Update is called once per frame
        private void FixedUpdate()
        {
            if (_leap == null) return;

            var frame = _leap.Frame();

            //Debug.Log("I Found " + frame.Fingers.Count + " fingers...");


            if (frame.Hands.Count > 1 && frame.Fingers.Count < 6)
            {
                if (_activeFingerA != null && frame.Fingers.Count(x => x.Id == _activeFingerA.Id) == 0)
                    _activeFingerA = null;

                _activeFingerA = (_activeFingerA != null) ? frame.Finger(_activeFingerA.Id) : frame.Fingers.Frontmost;

                foreach (var finger in frame.Fingers.Where(finger => finger.Id != _activeFingerA.Id &&
                                                                     (finger.TipPosition.ToUnityScaled().x > -2 &&
                                                                      finger.TipPosition.ToUnityScaled().x < 2) &&
                                                                     !(_activeFingerA.TipPosition.ToUnityScaled().x > -2 &&
                                                                       _activeFingerA.TipPosition.ToUnityScaled().x < 2)
                    ))
                {
                    _activeFingerA = finger;
                }


                //Force quit fingers if action disabled
                if (!ActionEnabled())
                {
                    _activeFingerA = null;
                    // _activeFingerB = null;
                }

            }
            else
            {
                _activeFingerA = null;
            }

            HandleFinger(_activeFingerA, _pointer[0]);
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

                //Reset Timer
                resetTimer.ResetCounter();

                //Perform action
                var calcedPos = finger.TipPosition.ToUnityScaled();

                calcedPos.z = calcedPos.z + _pointerZOffset;

                var avgZ = (calcedPos.z < PointerMaxZ)
                    ? PointerMaxZ
                    : (calcedPos.z > PointerMinZ) ? PointerMinZ : calcedPos.z;

                var newPos = new Vector3(calcedPos.x, calcedPos.y + PointerYOffset, avgZ);

                pPointer.transform.position = Vector3.Lerp(pPointer.transform.position, newPos, 0.5F);


                RaycastHit hit;
                var directionRay = new Ray(pPointer.transform.position, Vector3.forward);
                //Debug.DrawRay(pPointer.transform.position, Vector3.forward * 10);

                if (Physics.Raycast(directionRay, out hit, 10))
                {
                    HandleRaycastHit(hit);
                }
                else
                {
                    ResetGameObjectColor();
                }



            }
        }

        private void HandleRaycastHit(RaycastHit hit)
        {
            var newHittedObject = hit.collider.gameObject;

            if (newHittedObject == null)
            {
                ResetGameObjectColor();
                return;
            }
            if (newHittedObject == _lastHittedObject) return;

            if (newHittedObject.tag == "MondrianCube")
            {
                if (_lastHittedObject != null)
                {
                    _lastHittedObject.renderer.material.color = _savedColor;
                }

                _savedColor = newHittedObject.renderer.material.color;
                _lastHittedObject = newHittedObject;
                //Debug.Log("I Hit da Cube " + newHittedObject.name + " =): " + newHittedObject + " - " + newHittedObject.tag);

                newHittedObject.renderer.material.color = highlightColor;
            }
            else
            {
                ResetGameObjectColor();
            }
        }

        private void ResetGameObjectColor()
        {
            if (_lastHittedObject == null) return;
            _lastHittedObject.renderer.material.color = _savedColor;
            _lastHittedObject = null;
        }

        private void HandleGestures(IEnumerable<Gesture> gestures)
        {
            foreach (var gesture in gestures)
            {
                switch (gesture.Type)
                {
                    case Gesture.GestureType.TYPECIRCLE:
                        _registerdCircleHandler.ForEach(x => x.Invoke(gesture));
                        break;
                    case Gesture.GestureType.TYPEKEYTAP:
                        //if(ActionEnabled()) HandleKeyTapGesture(gesture);
                        break;
                    case Gesture.GestureType.TYPESCREENTAP:
                        if (ActionEnabled()) HandleScreenTapGesture(gesture);
                        break;
                    case Gesture.GestureType.TYPESWIPE:
                        //HandleSwipeGesture(gesture);
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

//        private void HandleKeyTapGesture(Gesture gesture)
//        {
//            if (gesture.Frame.Fingers.Count < 2) return;
//
//            //Reset Timer
//            resetTimer.ResetCounter();
//
//            var keytap = new KeyTapGesture(gesture);
//            Debug.Log("KeyTap " + ((keytap.Position.x > 0) ? "right" : "left") + " - " + keytap.Position.x);
//
//            var right = keytap.Position.x > 0;
//
//            if (_activeFingerA != null && _lastHittedObject != null)
//            {
//                var mondrian = _lastHittedObject.GetComponent<MondrianBehaviour>();
//                if (right)
//                {
//                    var actionObject = GameObject.FindGameObjectWithTag("ActiveAction");
//
//                    if (actionObject.name.ToLower().Contains("vertical"))
//                    {
//                        mondrian.ChangeColour(_savedColor);
//                        mondrian.SplitVertical();
//                        //mondrian.ChangeColour(highlightColor);
//                    }
//                    else if (actionObject.name.ToLower().Contains("horizontal"))
//                    {
//                        mondrian.ChangeColour(_savedColor);
//                        mondrian.SplitHorizontal();
//                        //mondrian.ChangeColour(highlightColor);
//                    }
//                }
//                else
//                {
//                    var colorObject = GameObject.FindGameObjectWithTag("ActiveColor");
//
//                    var color = colorObject.renderer.material.color;
//
//                    mondrian.ChangeColour(color);
//                    _savedColor = color;
//                }
//            }
//
//        }

        private void HandleScreenTapGesture(Gesture gesture)
        {
            if (gesture.Frame.Fingers.Count < 2) return;

            //Reset Timer
            resetTimer.ResetCounter();

            var keytap = new ScreenTapGesture(gesture);
            Debug.Log("KeyTap " + ((keytap.Position.x > 0) ? "right" : "left") + " - " + keytap.Position.x);

            var right = keytap.Position.x > 0;

            if (_activeFingerA != null && _lastHittedObject != null)
            {
                var mondrian = _lastHittedObject.GetComponent<MondrianBehaviour>();
                if (right)
                {
                    var actionObject = GameObject.FindGameObjectWithTag("ActiveAction");

                    if (actionObject.name.ToLower().Contains("vertical"))
                    {
                        mondrian.SplitVertical(_savedColor);
                    }
                    else if (actionObject.name.ToLower().Contains("horizontal"))
                    {
                        mondrian.SplitHorizontal(_savedColor);
                    }
                }
                else
                {
                    var colorObject = GameObject.FindGameObjectWithTag("ActiveColor");

                    var color = colorObject.renderer.material.color;

                    mondrian.ChangeColour(color);
                    _savedColor = color;
                }
            }

        }

        private static void HandleSwipeGesture(Gesture gesture)
        {
            //if (gesture.State == Gesture.GestureState.STATESTART || gesture.State == Gesture.GestureState.STATEINVALID ||
            //    gesture.State == Gesture.GestureState.STATEUPDATE) return;
            //if (gesture.DurationSeconds < 0.10) return;

            if (gesture.State != Gesture.GestureState.STATESTOP) return;

            var swipe = new SwipeGesture(gesture);
            Debug.Log("Swipe " + swipe.Id + ", " + swipe.State.ToString() + " : " +
                      ((swipe.StartPosition.x > swipe.Position.x) ? "<<<<<<<<<<" : ">>>>>>>>>>"));
                //) + swipe.StartPosition.x + " -> " + swipe.Position.x);
        }

        private void HandleCircleGestureForTurnables(Gesture gesture)
        {
            if (!ActionEnabled()) return;
            
            //Reset Timer
            resetTimer.ResetCounter();

            var circle = new CircleGesture(gesture);

            //if (circle.Radius > circleRadiusChangeViewLimit) return;
            if (gesture.Frame.Hands.Count < 2) return;

            if (gesture.State == Gesture.GestureState.STATESTART || gesture.State == Gesture.GestureState.STATEINVALID)
                return;
            if (circle.Radius < circleRadiusIgnoreLimit) return;

            var circleCenter = circle.Center.ToUnityScaled();
            var clockwise = (circle.Pointable.Direction.AngleTo(circle.Normal) <= Math.PI/2);

            foreach (
                var turnable in
                    turnables.Select(x => x.GetComponent(typeof (ITurnable)))
                        .Where(x => x != null)
                        .Cast<ITurnable>()
                        .Where(turnable => turnable.InRange(circleCenter)))
            {
                if (clockwise) turnable.TurnClockwise();
                else turnable.TurnCounterClockwise();
            }
        }

        private static bool ActionEnabled()
        {
            return !Camera.main.GetComponent<LeapCamControl>().enabled;
        }

        private void HandleCircleGestureForViewChange(Gesture gesture)
        {
            //Reset Timer
            resetTimer.ResetCounter();

            var circle = new CircleGesture(gesture);

//            if (circle.Frame.Fingers.Count < 3)
//            {
//                Debug.Log("Expected Fingers < 3 - actual: " + circle.Frame.Fingers.Count);
//                return;
//            }
            if (circle.Frame.Hands.Count > 1)
            {
                Debug.Log("Expected Hand 1 - actual: " + circle.Frame.Hands.Count);
                return;
            }
            if (circle.Radius < circleRadiusChangeViewLimit) return;
//            if (circle.Radius <= circleRadiusChangeViewLimit)
//            {
//                Debug.Log("Expected Radius: " + circleRadiusChangeViewLimit + " <= - actual: " + circle.Radius);
//                return;
//            }

            if (gesture.State != Gesture.GestureState.STATESTOP) return;

            Debug.Log("Accepted: Fingers: " + circle.Frame.Fingers.Count + " Hands: " + circle.Frame.Hands.Count + " Radius: " + circle.Radius);


            var mondrianContainer = GameObject.Find("MondrianContainer");


            var camComponent = Camera.main.GetComponent<LeapCamControl>();
            var mondrianComponent = mondrianContainer.GetComponent<LeapObjectControl>();

            //Run only once at time
            if (runCamChange) return;

            if (!ActionEnabled())
            {
                camComponent.enabled = false;
                mondrianComponent.enabled = true;

                StartCoroutine(MoveCamToOriginPosition(_originCamPosition, mondrianComponent.transform.position, 1));
            }
            else
            {
                camComponent.enabled = true;
                mondrianComponent.enabled = false;

                StartCoroutine(MoveCamToOriginPosition(new Vector3(_originCamPosition.x, _originCamPosition.y + 2, _originCamPosition.z), mondrianComponent.transform.position, 1));
            }
        }

        IEnumerator MoveCamToOriginPosition(Vector3 origin, Vector3 directedTo, float time)
        {
            runCamChange = true;
            var transf = Camera.main.transform;
            var elapsedTime = 0f;
            var startingPos = transf.localPosition;

            while (elapsedTime < time)
            {
                transf.localPosition = Vector3.Lerp(startingPos, origin, (elapsedTime / time));
                elapsedTime += Time.deltaTime;
                transf.LookAt(directedTo);
                yield return null;
            }
            transf.localPosition = origin;
            transf.LookAt(directedTo);
            runCamChange = false;
        }
    }
}

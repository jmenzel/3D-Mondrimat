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
        public GameObject pointer;
        public GameObject[] turnables;
        public float PointerMaxZ = -10;
        public float PointerMinZ = -6;
        public float PointerYOffset = -9;
        public float CircleRadiusChangeViewLimit = 90f;
        public float CircleRadiusIgnoreLimit = 40f;
        public Color HighlightColor = Color.cyan;
        public float ScreenTapDelayLimitInMs = 500;
        
        private float _pointerZOffset;
        private GameObject _lastHittedObject;
        private GameObject _pointer;

        private Controller _leap;
        private Color _savedColor;
        private Finger _activeFinger;
        private List<Action<Gesture>> _registerdCircleHandler;
        private Vector3 _originCamPosition;
        private bool _runCamChange;
        private RestartSceneAfterTimeout _resetTimer;
        private int _lastGestureId = -1;
        private float _lastScreenTap;

        private void Start()
        {
            _leap = new Controller();
            if (transform == null)
            {
                Debug.LogError("Must have a parent object to control");
            }


            _originCamPosition = Camera.main.transform.position;
            _resetTimer = Camera.main.GetComponent<RestartSceneAfterTimeout>();

            _leap.EnableGesture(Gesture.GestureType.TYPECIRCLE);
            _leap.EnableGesture(Gesture.GestureType.TYPESCREENTAP);

            _pointerZOffset = pointer.transform.position.z;

            _pointer = Instantiate(pointer, pointer.transform.position, pointer.transform.rotation) as GameObject;

            _registerdCircleHandler = new List<Action<Gesture>>
            {
                HandleCircleGestureForTurnables,
                HandleCircleGestureForViewChange
            };
        }

        private void FixedUpdate()
        {
            if (_leap == null) return;

            var frame = _leap.Frame();

            if (frame.Hands.Count > 1 && frame.Fingers.Count < 6)
            {
                //is last finger in fingerlist ?
                if (_activeFinger != null && frame.Fingers.Count(x => x.Id == _activeFinger.Id) == 0)
                    _activeFinger = null;

                //Get last finger again or frontmost as default
                _activeFinger = (_activeFinger != null) ? frame.Finger(_activeFinger.Id) : frame.Fingers.Frontmost;

                //Switch finger to the finger, which is closer to the mondrian cube
                foreach (var finger in frame.Fingers.Where(finger => finger.Id != _activeFinger.Id &&
                                                                     (finger.TipPosition.ToUnityScaled().x > -2 &&
                                                                      finger.TipPosition.ToUnityScaled().x < 2) &&
                                                                     !(_activeFinger.TipPosition.ToUnityScaled().x > -2 &&
                                                                       _activeFinger.TipPosition.ToUnityScaled().x < 2)
                    ))
                {
                    _activeFinger = finger;
                }

                //Force quit fingers if action disabled
                if (!ActionEnabled())
                {
                    _activeFinger = null;
                }
            }
            else
            {
                _activeFinger = null;
            }

            HandleFinger(_activeFinger, _pointer);
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
                _resetTimer.ResetCounter();

                //Calc position for pointer object - finger representation
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

                //Check if we point to something
                if (Physics.Raycast(directionRay, out hit, 10))
                {
                    HandleRaycastHit(hit);
                }
                else
                {
                    //No hit - reset highlight color etc
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

            //Just handle the Hover for mondrian cubes
            if (newHittedObject.tag == "MondrianCube")
            {
                if (_lastHittedObject != null)
                {
                    //Reset color of previous object
                    _lastHittedObject.renderer.material.color = _savedColor;
                }

                //save new color and current object
                _savedColor = newHittedObject.renderer.material.color;
                _lastHittedObject = newHittedObject;

                //Set color of current object to highlight color
                newHittedObject.renderer.material.color = HighlightColor;
            }
            else
            {
                //The object we hit, is not allowed -> reset last object
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
                    case Gesture.GestureType.TYPESCREENTAP:
                        //Filter double fired or old queued Gestures
                        if (gesture.Id != _lastGestureId && gesture.Id > _lastGestureId)
                        {
                            if (ActionEnabled()) HandleScreenTapGesture(gesture);
                        }
                        _lastGestureId = gesture.Id;
                        break;
                    case Gesture.GestureType.TYPEINVALID:
                        Debug.Log("Invalid Gesture");
                        break;
                    default:
                        Debug.Log("Bad gesture type");
                        break;
                }
            }
        }

        private void HandleScreenTapGesture(Gesture gesture)
        {
            //Delay
            if ((Time.time - _lastScreenTap) <= (ScreenTapDelayLimitInMs / 1000)) return;
            _lastScreenTap = Time.time;


            if (gesture.Frame.Fingers.Count < 2) return;
            if (gesture.State != Gesture.GestureState.STATESTOP) return;
            //Reset Timer
            _resetTimer.ResetCounter();

            var screenTap = new ScreenTapGesture(gesture);
            //Debug.Log("ScreenTap ("+gesture.Id+")  IsValuid:" +gesture.IsValid+ " Position" + ((keytap.Position.x > 0) ? "right" : "left") + " - " + keytap.Position.x + " - State: " + gesture.State);

            //on wich side was the tap?
            var right = screenTap.Position.x > 0;

            //Just handle the tap, if we have a selected object and a pointer finger
            if (_activeFinger != null && _lastHittedObject != null)
            {
                //Get the mondrianBehaviour component
                var mondrian = _lastHittedObject.GetComponent<MondrianBehaviour>();

                //Right side? -> Split action
                if (right)
                {
                    //Get the setted Action
                    var actionObject = GameObject.FindGameObjectWithTag("ActiveAction");

                    //And fire action
                    if (actionObject.name.ToLower().Contains("vertical"))
                    {
                        mondrian.SplitVertical(_savedColor);
                    }
                    else if (actionObject.name.ToLower().Contains("horizontal"))
                    {
                        mondrian.SplitHorizontal(_savedColor);
                    }
                }
                //Left side? -> Color change action
                else
                {
                    //Get the setted Color 
                    var colorObject = GameObject.FindGameObjectWithTag("ActiveColor");
                    var color = colorObject.renderer.material.color;

                    //And set the new Color
                    mondrian.ChangeColour(color);

                    //overwrite saved color
                    _savedColor = color;
                }
            }

        }

        private void HandleCircleGestureForTurnables(Gesture gesture)
        {
            if (!ActionEnabled()) return;
            
            //Reset Timer
            _resetTimer.ResetCounter();

            var circle = new CircleGesture(gesture);

            //No mini circles
            if (circle.Radius < CircleRadiusIgnoreLimit) return;

            //Only with two hands - we are in action mode ;)
            if (gesture.Frame.Hands.Count < 2) return;

            //Just Stop and Update events allowed
            if (gesture.State == Gesture.GestureState.STATESTART || gesture.State == Gesture.GestureState.STATEINVALID)
                return;

            var circleCenter = circle.Center.ToUnityScaled();

            //calc the direction of the circle
            var clockwise = (circle.Pointable.Direction.AngleTo(circle.Normal) <= Math.PI/2);

            //For all registered turnables wich are in the right position
            foreach (
                var turnable in
                    turnables.Select(x => x.GetComponent(typeof (ITurnable)))
                        .Where(x => x != null)
                        .Cast<ITurnable>()
                        .Where(turnable => turnable.InRange(circleCenter)))
            {
                //Turn =)
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
            _resetTimer.ResetCounter();

            var circle = new CircleGesture(gesture);
            
            //Just one hand - we handle view mode
            if (circle.Frame.Hands.Count > 1) return;

            //if (circle.Radius < CircleRadiusChangeViewLimit) return;
            
            //Just stop events
            if (gesture.State != Gesture.GestureState.STATESTOP) return;

            //Get the needed components
            var mondrianContainer = GameObject.Find("MondrianContainer");
            var camComponent = Camera.main.GetComponent<LeapCamControl>();
            var mondrianComponent = mondrianContainer.GetComponent<LeapObjectControl>();

            //Run only once at time
            if (_runCamChange) return;

            //Are we in view or in action mode?
            if (!ActionEnabled())
            {
                //Back to action mode
                camComponent.enabled = false;
                mondrianComponent.enabled = true;
                
                //Reset Cam position
                StartCoroutine(MoveCamToOriginPosition(_originCamPosition, mondrianComponent.transform.position, 1));
            }
            else
            {
                //Lets go into view mode
                camComponent.enabled = true;
                mondrianComponent.enabled = false;

                //Move cam a little up to signal view mode
                StartCoroutine(MoveCamToOriginPosition(new Vector3(_originCamPosition.x, _originCamPosition.y + 2, _originCamPosition.z), mondrianComponent.transform.position, 1));
            }
        }

        IEnumerator MoveCamToOriginPosition(Vector3 origin, Vector3 directedTo, float time)
        {
            _runCamChange = true;
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
            _runCamChange = false;
        }
    }
}

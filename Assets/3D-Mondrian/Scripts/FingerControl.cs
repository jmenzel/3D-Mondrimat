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
        public float circleRadiusChangeViewLimit = 90f;
        public float circleRadiusIgnoreLimit = 40f;
        public Color highlightColor = Color.cyan;

        
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
                if (_activeFinger != null && frame.Fingers.Count(x => x.Id == _activeFinger.Id) == 0)
                    _activeFinger = null;

                _activeFinger = (_activeFinger != null) ? frame.Finger(_activeFinger.Id) : frame.Fingers.Frontmost;

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
                    case Gesture.GestureType.TYPESCREENTAP:

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
            if (gesture.Frame.Fingers.Count < 2) return;
            if (gesture.State != Gesture.GestureState.STATESTOP) return;
            //Reset Timer
            _resetTimer.ResetCounter();

            var keytap = new ScreenTapGesture(gesture);
            //Debug.Log("ScreenTap ("+gesture.Id+")  IsValuid:" +gesture.IsValid+ " Position" + ((keytap.Position.x > 0) ? "right" : "left") + " - " + keytap.Position.x + " - State: " + gesture.State);

            var right = keytap.Position.x > 0;

            if (_activeFinger != null && _lastHittedObject != null)
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

        private void HandleCircleGestureForTurnables(Gesture gesture)
        {
            if (!ActionEnabled()) return;
            
            //Reset Timer
            _resetTimer.ResetCounter();

            var circle = new CircleGesture(gesture);

            if (circle.Radius < circleRadiusIgnoreLimit) return;
            if (gesture.Frame.Hands.Count < 2) return;
            if (gesture.State == Gesture.GestureState.STATESTART || gesture.State == Gesture.GestureState.STATEINVALID)
                return;

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
            _resetTimer.ResetCounter();

            var circle = new CircleGesture(gesture);
            
            if (circle.Frame.Hands.Count > 1) return;
            if (circle.Radius < circleRadiusChangeViewLimit) return;
            if (gesture.State != Gesture.GestureState.STATESTOP) return;

            var mondrianContainer = GameObject.Find("MondrianContainer");
            var camComponent = Camera.main.GetComponent<LeapCamControl>();
            var mondrianComponent = mondrianContainer.GetComponent<LeapObjectControl>();

            //Run only once at time
            if (_runCamChange) return;

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

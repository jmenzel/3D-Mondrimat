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

        // Use this for initialization
        private void Start()
        {
            _leap = new Controller();
            if (transform == null)
            {
                Debug.LogError("Must have a parent object to control");
            }

            _originCamPosition = Camera.main.transform.position;


//            var config = _leap.Config;
//            config.SetFloat("Gesture.KeyTap.MinDistance", 2f);
//            config.SetFloat("Gesture.KeyTap.MinDownVelocity", 20f);
//            var success = config.Save();
//            Debug.Log("Save Leap config: " + success);

            _leap.EnableGesture(Gesture.GestureType.TYPECIRCLE);
            _leap.EnableGesture(Gesture.GestureType.TYPEKEYTAP);
            //_leap.EnableGesture(Gesture.GestureType.TYPESCREENTAP);
            //_leap.EnableGesture(Gesture.GestureType.TYPESWIPE);
            //_leap.EnableGesture(Gesture.GestureType.TYPEINVALID);

            _pointerZOffset = pointer.transform.position.z;

            _pointer = new[]
            {
                Instantiate(pointer, pointer.transform.position, pointer.transform.rotation) as GameObject,
                Instantiate(pointer, pointer.transform.position, pointer.transform.rotation) as GameObject
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

            switch (frame.Fingers.Count)
            {
                case 0:
                    _activeFingerA = null;
                    //_activeFingerB = null;
                    break;
                case 1:
                    _activeFingerA = _activeFingerA != null ? frame.Finger(_activeFingerA.Id) : frame.Fingers.Frontmost;
                    _activeFingerB = null;
                    break;
                default:
                    _activeFingerA = _activeFingerA != null ? frame.Finger(_activeFingerA.Id) : frame.Fingers.Frontmost;
                    //_activeFingerA = _activeFingerA != null ? frame.Finger(_activeFingerA.Id) : frame.Fingers.Rightmost;
                    //_activeFingerB = _activeFingerB != null ? frame.Finger(_activeFingerB.Id) : frame.Fingers.Leftmost;
                    break;
            }

            //Force quit fingers if action disabled
            if (!ActionEnabled())
            {
                _activeFingerA = null;
            }


            HandleFinger(_activeFingerA, _pointer[0]);

            //HandleFinger(_activeFingerB, _pointer[1]);
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

                newHittedObject.renderer.material.color = Color.cyan;
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
                        if(ActionEnabled()) HandleKeyTapGesture(gesture);
                        break;
                    case Gesture.GestureType.TYPESCREENTAP:
                        Debug.Log("ScreenTap");
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

        private void HandleKeyTapGesture(Gesture gesture)
        {
            if (gesture.Frame.Fingers.Count != 2) return;

            var keytap = new KeyTapGesture(gesture);
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
                        mondrian.SplitVertical();
                    }
                    else if (actionObject.name.ToLower().Contains("horizontal"))
                    {
                        mondrian.SplitHorizontal();
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
            var circle = new CircleGesture(gesture);

            if (circle.Radius > circleRadiusChangeViewLimit) return;

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
            var circle = new CircleGesture(gesture);
            if (circle.Radius <= circleRadiusChangeViewLimit) return;
            if (gesture.State != Gesture.GestureState.STATESTOP) return;

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

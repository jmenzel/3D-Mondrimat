using System.Linq;
using UnityEngine;
using Leap;

public class LeapCamControl : MonoBehaviour {


    Controller _leap;

    public GameObject DirectedTo;
    public float MoveSpeed = 0.5f;
    public float HandHeightStart = 2f;
    public float SmoothLerpFactor = 0.2f;
    public float CamZoomOffset = 2;

    private RestartSceneAfterTimeout _resetTimer;

	// Use this for initialization
	void Start () 
    {
        _leap = new Controller();
        if (transform == null)
        {
            Debug.LogError("Must have a parent object to control");
        }

        _resetTimer = Camera.main.GetComponent<RestartSceneAfterTimeout>();
	}
	

    void FixedUpdate()
    {

        var frame = _leap.Frame();

        if (frame.Hands.Count >= 2)
        {
            if (frame.Fingers.Count < 3)
            {
                return;
            }

            //Reset Timer
            _resetTimer.ResetCounter();

            var leftHand = GetLeftMostHand(frame);
            var rightHand = GetRightMostHand(frame);

            var avgPalmForward = (frame.Hands[0].Direction.ToUnity() + frame.Hands[1].Direction.ToUnity()) * 0.5f;
            var handDiff = leftHand.PalmPosition.ToUnityScaled() - rightHand.PalmPosition.ToUnityScaled();
            var avgPos = (leftHand.PalmPosition.ToUnityScaled() + rightHand.PalmPosition.ToUnityScaled()) / 2;
            var newRot = transform.localRotation.eulerAngles;
            
            newRot.z = -handDiff.y * 20.0f;
            newRot.y += handDiff.z * 3.0f - newRot.z * 0.03f * transform.rigidbody.velocity.magnitude;
            newRot.x = -(avgPalmForward.y - 0.1f) * 100.0f;

            transform.RotateAround(DirectedTo.transform.position, new Vector3(0, 1, 0), newRot.z / 100 * MoveSpeed);

            var posY = ((avgPos.y - HandHeightStart) + transform.position.y) / 2;
            var posZ = transform.position.z;

            var newPosition = new Vector3(transform.position.x, posY, (posZ));

            transform.position = Vector3.Lerp(transform.position, newPosition, SmoothLerpFactor);

            if (DirectedTo != null)
            {
                transform.LookAt(DirectedTo.transform.position);
            }
        }

    }


    static Hand GetLeftMostHand(Frame frame)
    {
        float[] smallestVal = {float.MaxValue};
        Hand h = null;
        foreach (var hand in frame.Hands.Where(t => t.PalmPosition.ToUnity().x < smallestVal[0]))
        {
            smallestVal[0] = hand.PalmPosition.ToUnity().x;
            h = hand;
        }
        return h;
    }

    static Hand GetRightMostHand(Frame frame)
    {
        float[] largestVal = {-float.MaxValue};
        Hand h = null;
        foreach (var hand in frame.Hands.Where(hand => hand.PalmPosition.ToUnity().x > largestVal[0]))
        {
            largestVal[0] = hand.PalmPosition.ToUnity().x;
            h = hand;
        }
        return h;
    }
}

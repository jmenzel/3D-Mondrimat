using UnityEngine;
using System.Collections;
using Leap;

public class LeapObjectControl : MonoBehaviour
{


    Controller m_leapController;

    public GameObject DirectedTo;
    public float MoveSpeed = 0.5f;
    public float HandHeightStart = 2f;
    public float SmoothLerpFactor = 0.2f;
    public float CamZoomOffset = 2;

    private Vector3 _rotationEuler;

	// Use this for initialization
	void Start () 
    {
        m_leapController = new Controller();
        if (transform == null)
        {
            Debug.LogError("LeapFly must have a parent object to control");
        }

        _rotationEuler = gameObject.transform.eulerAngles;
	}
	

    void FixedUpdate()
    {

        Frame frame = m_leapController.Frame();

        if (frame.Hands.Count >= 2)
        {
            if (frame.Fingers.Count < 4)
            {
                return;
            }

            Hand leftHand = GetLeftMostHand(frame);
            Hand rightHand = GetRightMostHand(frame);

            // takes the average vector of the forward vector of the hands, used for the
            // pitch of the plane.
            Vector3 avgPalmForward = (frame.Hands[0].Direction.ToUnity() + frame.Hands[1].Direction.ToUnity()) * 0.5f;

            Vector3 handDiff = leftHand.PalmPosition.ToUnityScaled() - rightHand.PalmPosition.ToUnityScaled();

            Vector3 avgPos = (leftHand.PalmPosition.ToUnityScaled() + rightHand.PalmPosition.ToUnityScaled()) / 2;

            Vector3 newRot = transform.localRotation.eulerAngles;
            newRot.z = -handDiff.y * 20.0f;

            // adding the rot.z as a way to use banking (rolling) to turn.
            newRot.y += handDiff.z * 3.0f - newRot.z * 0.03f;// *transform.rigidbody.velocity.magnitude;
            newRot.x = -(avgPalmForward.y - 0.1f) * 100.0f;


            _rotationEuler = _rotationEuler + new Vector3(0, (newRot.z / 100) * MoveSpeed, 0);
            gameObject.transform.eulerAngles = new Vector3(_rotationEuler.x % 360, _rotationEuler.y % 360, _rotationEuler.z % 360);
        }

    }


    Hand GetLeftMostHand(Frame f)
    {
        float smallestVal = float.MaxValue;
        Hand h = null;
        for (int i = 0; i < f.Hands.Count; ++i)
        {
            if (f.Hands[i].PalmPosition.ToUnity().x < smallestVal)
            {
                smallestVal = f.Hands[i].PalmPosition.ToUnity().x;
                h = f.Hands[i];
            }
        }
        return h;
    }

    Hand GetRightMostHand(Frame f)
    {
        float largestVal = -float.MaxValue;
        Hand h = null;
        for (int i = 0; i < f.Hands.Count; ++i)
        {
            if (f.Hands[i].PalmPosition.ToUnity().x > largestVal)
            {
                largestVal = f.Hands[i].PalmPosition.ToUnity().x;
                h = f.Hands[i];
            }
        }
        return h;
    }
}

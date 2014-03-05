using UnityEngine;
using System.Collections;
using Leap;

public class LeapCamControl : MonoBehaviour {


    Controller m_leapController;

    public GameObject DirectedTo;
    public float MoveSpeed = 0.5f;
    public float HandHeightStart = 2f;
    public float SmoothLerpFactor = 0.2f;
    public float CamZoomOffset = 2;

	// Use this for initialization
	void Start () 
    {
        m_leapController = new Controller();
        if (transform == null)
        {
            Debug.LogError("LeapFly must have a parent object to control");
        }
	}
	

    void FixedUpdate()
    {

        Frame frame = m_leapController.Frame();

        if (frame.Hands.Count >= 2)
        {
            if (frame.Fingers.Count < 3)
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
            newRot.y += handDiff.z * 3.0f - newRot.z * 0.03f * transform.rigidbody.velocity.magnitude;
            newRot.x = -(avgPalmForward.y - 0.1f) * 100.0f;
            //float forceMult = 20.0f;

            // if closed fist, then stop the plane and slowly go backwards.

            //Debug.Log(handDiff.x + " | " + handDiff.y + " | " + handDiff.z);


            transform.RotateAround(DirectedTo.transform.position, new Vector3(0, 1, 0), newRot.z / 100 * MoveSpeed);   //MoveSpeed * Time.deltaTime);

            float posY = ((avgPos.y - HandHeightStart) + transform.position.y) / 2;
            float posZ = transform.position.z;// (handDiff.x + CamZoomOffset + transform.position.z) / 2;

            Vector3 newPosition = new Vector3(transform.position.x, posY, (posZ));

            transform.position = Vector3.Lerp(transform.position, newPosition, SmoothLerpFactor);

            if (DirectedTo != null)
            {
                transform.LookAt(DirectedTo.transform.position);
            }
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

using System.Linq;
using UnityEngine;
using Leap;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedMember.Global
public class LeapObjectControl : MonoBehaviour
{


    private Controller _mLeapController;

    public float MoveSpeed = 0.5f;

    private Vector3 _rotationEuler;

	// Use this for initialization
	void Start () 
    {
        _mLeapController = new Controller();
        if (transform == null)
        {
            Debug.LogError("Must have a parent object to control");
        }

        _rotationEuler = gameObject.transform.eulerAngles;
	}
	

    void FixedUpdate()
    {

        if (_mLeapController == null) return;

        var frame = _mLeapController.Frame();

        if (frame.Hands.Count < 2) return;
        if (frame.Fingers.Count < 7)
        {
            return;
        }

        var leftHand = GetLeftMostHand(frame);
        var rightHand = GetRightMostHand(frame);

        // takes the average vector of the forward vector of the hands, used for the
        // pitch of the plane.
        var avgPalmForward = (frame.Hands[0].Direction.ToUnity() + frame.Hands[1].Direction.ToUnity()) * 0.5f;

        var handDiff = leftHand.PalmPosition.ToUnityScaled() - rightHand.PalmPosition.ToUnityScaled();

        var newRot = transform.localRotation.eulerAngles;
        newRot.z = -handDiff.y * 20.0f;

        // adding the rot.z as a way to use banking (rolling) to turn.
        newRot.y += handDiff.z * 3.0f - newRot.z * 0.03f;// *transform.rigidbody.velocity.magnitude;
        newRot.x = -(avgPalmForward.y - 0.1f) * 100.0f;


        _rotationEuler = _rotationEuler + new Vector3(0, (newRot.z / 100) * MoveSpeed, 0);
        gameObject.transform.eulerAngles = new Vector3(_rotationEuler.x % 360, _rotationEuler.y % 360, _rotationEuler.z % 360);
    }


    Hand GetLeftMostHand(Frame f)
    {
        float[] smallestVal = {float.MaxValue};
        Hand h = null;
        foreach (var hand in f.Hands.Where(hand => hand.PalmPosition.ToUnity().x < smallestVal[0]))
        {
            smallestVal[0] = hand.PalmPosition.ToUnity().x;
            h = hand;
        }
        return h;
    }

    Hand GetRightMostHand(Frame f)
    {
        float[] largestVal = {-float.MaxValue};
        Hand h = null;
        foreach (var hand in f.Hands.Where(hand => hand.PalmPosition.ToUnity().x > largestVal[0]))
        {
            largestVal[0] = hand.PalmPosition.ToUnity().x;
            h = hand;
        }
        return h;
    }
}

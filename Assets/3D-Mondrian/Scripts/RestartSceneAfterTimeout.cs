using System.Collections;
using UnityEngine;

public class RestartSceneAfterTimeout : MonoBehaviour
{
    public int TimeInSec = 20;
    public int GravityFallTime = 7;

    private int _counter;

    private bool _anyActionReceived;

	// Use this for initialization
	void Start ()
	{
        StartCoroutine(StartCounter());
	}

    public void ResetCounter()
    {
        ResetCounter(true);
    }

    private void ResetCounter(bool recordAction)
    {
        _anyActionReceived = recordAction;
        _counter = TimeInSec;
    }

    IEnumerator StartCounter()
    {
        do
        {
            ResetCounter(_anyActionReceived);

            var wait = _counter + 0;
            _counter = 0;
            yield return new WaitForSeconds(wait);

        } while (_counter > 0 || !_anyActionReceived);
        
        ReloadScene();
    }

    private void ReloadScene()
    {
        Debug.Log("Reload Scene!");

        foreach (var obj in (GameObject[])FindObjectsOfType(typeof(GameObject)))
        {
           SetGravityForGameObject(obj);
        }

        StartCoroutine(ReloadSceneAfter(GravityFallTime));
    }

    private static void SetGravityForGameObject(GameObject obj)
    {
        if (obj.tag == "NoPhysic") return;

        var rigidbody = obj.GetComponent<Rigidbody>();

        if (rigidbody == null)
        {
            rigidbody = obj.AddComponent<Rigidbody>();
        }

        rigidbody.mass = 1;
        rigidbody.useGravity = true;
    }

    IEnumerator ReloadSceneAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        Application.LoadLevel("CubeTransforming");
    }
}

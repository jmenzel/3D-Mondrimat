using System.Collections;
using UnityEngine;

public class RestartSceneAfterTimeout : MonoBehaviour
{
    public int TimeInSec = 20;

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

    private static void ReloadScene()
    {
        Debug.Log("Reload Scene!");
        Application.LoadLevel("CubeTransforming");
    }
}

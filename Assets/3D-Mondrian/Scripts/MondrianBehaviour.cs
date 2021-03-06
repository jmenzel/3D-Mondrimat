﻿using System.IO.IsolatedStorage;
using UnityEngine;

namespace Assets
{
    public class MondrianBehaviour : MonoBehaviour
    {
        public GameObject MonCube;

        private Color[] _supportedColors;
        private uint _colorPointer;
        private RestartSceneAfterTimeout resetTimer;

        private void Start ()
        {
            _supportedColors = new[] { Color.red, Color.yellow, Color.blue, Color.black, Color.white };
            _colorPointer = 0;
            resetTimer = Camera.main.GetComponent<RestartSceneAfterTimeout>();
            //renderer.material.color = Color.red;
        }

        private void OnMouseDown()
        {
            //Split Horizontal
            if (Input.GetKey(KeyCode.Y))
            {
                resetTimer.ResetCounter();
                SplitHorizontal(_supportedColors[_colorPointer]);
            }

            //Split Vertical
            if (Input.GetKey(KeyCode.X))
            {
                resetTimer.ResetCounter();
                SplitVertical(_supportedColors[_colorPointer]);
            }

            //Change Color
            if (Input.GetKey(KeyCode.C))
            {
                resetTimer.ResetCounter();
                ChangeColour();
            }
        }

        public void ChangeColour(Color color)
        {
            renderer.material.color = color;
        }

        private void ChangeColour()
        {
            if (_colorPointer >= _supportedColors.Length)
            {
                _colorPointer = 0;
            }

            renderer.material.color = _supportedColors[_colorPointer++];
        }

        public void SplitVertical(Color color)
        {
            var scale = transform.localScale;

            transform.localScale = new Vector3(scale.x / 2, scale.y, scale.z);
            var o = Instantiate(MonCube, transform.position, transform.rotation) as GameObject;

            transform.Translate(Vector3.left * (scale.x / 4));
            if (o == null) return;
            
            o.transform.Translate(Vector3.right * (scale.x / 4));
            o.transform.parent = transform.parent;
            o.renderer.material.color = color;
            //o.renderer.material.color = renderer.material.color;
        }

        public void SplitHorizontal(Color color)
        {
            var scale = transform.localScale;

            transform.localScale = new Vector3(scale.x, (scale.y / 2), scale.z);
            var o = Instantiate(MonCube, transform.position, transform.rotation) as GameObject;

            transform.Translate(Vector3.up * (scale.y / 4));
            if (o == null) return;
            
            o.transform.Translate(Vector3.down * (scale.y / 4));
            o.transform.parent = transform.parent;
            o.renderer.material.color = color;
            //o.renderer.material.color = renderer.material.color;
        }
    }
}

using System.IO.IsolatedStorage;
using UnityEngine;

namespace Assets
{
    public class MondrianBehaviour : MonoBehaviour
    {
        public float ObjectDistance = 0.01f;
        public GameObject MonCube;
        public float SplitLerpFactor = 0.5f;

        private Color[] _supportedColors;
        private uint _colorPointer;
        
        public void Start ()
        {
            _supportedColors = new[] { Color.red, Color.yellow, Color.blue, Color.black, Color.white };
            _colorPointer = 0;
        }

        public void Update()
        {
        }

        public void OnMouseDown()
        {
            //Split Horizontal
            if (Input.GetKey(KeyCode.Y))
            {
                SplitHorizontal();
            }

            //Split Vertical
            if (Input.GetKey(KeyCode.X))
            {
                SplitVertical();
            }

            //Change Color
            if (Input.GetKey(KeyCode.C))
            {
                ChangeColour();
            }
        }

        private void ChangeColour()
        {
            if(_colorPointer >= _supportedColors.Length)
            {
                _colorPointer = 0;
            }

            renderer.material.color = _supportedColors[_colorPointer++];
        }

        private void SplitVertical()
        {
            var scale = transform.localScale;
            var position = transform.position;

            transform.localScale = new Vector3(scale.x / 2, scale.y, scale.z);
            transform.position = new Vector3(position.x - (scale.x / 4), position.y, position.z);


            var newPosition = new Vector3(position.x + (scale.x / 4), position.y, position.z);
            var o = Instantiate(MonCube, newPosition, transform.rotation) as GameObject;

            if (o != null) o.renderer.material.color = _supportedColors[0];
        }

        private void SplitHorizontal()
        {
            var scale = transform.localScale;
            var position = transform.position;

            transform.localScale = new Vector3(scale.x, (scale.y / 2), scale.z);
            transform.position = new Vector3(position.x, position.y + (scale.y / 4), position.z);

            var o = Instantiate(MonCube, new Vector3(position.x, position.y - (scale.y / 4), position.z), transform.rotation) as GameObject;

            if (o != null) o.renderer.material.color = _supportedColors[0];
        }
    }
}

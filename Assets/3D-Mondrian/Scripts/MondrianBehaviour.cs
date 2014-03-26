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
            renderer.material.color = Color.red;
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

        public void ChangeColour(Color color)
        {
            renderer.material.color = color;
        }

        public void ChangeColour()
        {
            if (_colorPointer >= _supportedColors.Length)
            {
                _colorPointer = 0;
            }

            renderer.material.color = _supportedColors[_colorPointer++];
        }

        public void SplitVertical()
        {
            var scale = transform.localScale;

            transform.localScale = new Vector3(scale.x / 2, scale.y, scale.z);
            var o = Instantiate(MonCube, transform.position, transform.rotation) as GameObject;

            transform.Translate(Vector3.left * (scale.x / 4));
            if (o == null) return;
            
            o.transform.Translate(Vector3.right * (scale.x / 4));
            o.transform.parent = transform.parent;
            o.renderer.material.color = _supportedColors[0];
        }

        public void SplitHorizontal()
        {
            var scale = transform.localScale;
            var position = transform.position;

            transform.localScale = new Vector3(scale.x, (scale.y / 2), scale.z);

            var o = Instantiate(MonCube, transform.position, transform.rotation) as GameObject;

            transform.Translate(Vector3.up * (scale.y / 4));
            o.transform.Translate(Vector3.down * (scale.y / 4));

            o.transform.parent = transform.parent;

            if (o != null) o.renderer.material.color = _supportedColors[0];
        }
    }
}

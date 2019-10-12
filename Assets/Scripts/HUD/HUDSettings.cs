using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;


/// <summary>
/// This class stores settings for the entire canvas. It also stores useful methods for converting cooridinates to and from 2d canvas to curved canvas, or world space.
/// CurvedUIVertexEffect components (added to every canvas gameobject)ask this class for per-canvas settings when applying their curve effect.
/// </summary>

namespace CurvedHUD
{
    [RequireComponent(typeof(Canvas))]
    public class HUDSettings : MonoBehaviour
    {
        #region --- Fields ---

        [SerializeField]
        private bool interactable = true;
        [SerializeField]
        private bool blocksRaycasts = true;
        [SerializeField]
        private bool raycastMyLayerOnly = false;

        //Cyllinder settings
        [SerializeField]
        private int angle = 90;

        //Sphere settings
        [SerializeField]
        private int vertAngle = 90;


        //internal system settings
        private readonly int baseCircleSegments = 24;


        //support variables
        private Vector2 rectSize;
        private float radius;
        private Canvas canvas;

        #endregion

        #region --- Properties ---

        /// <summary>
        /// The measure of the arc of the Canvas.
        /// </summary>
        public int Angle
        {
            get { return angle; }
            set
            {
                if (angle != value)
                    SetUIAngle(value);
            }
        }

        /// <summary>
        /// Vertical angle of the canvas. Used in sphere shape and ring shape.
        /// </summary>
        public int VerticalAngle
        {
            get { return vertAngle; }
            set
            {
                if (vertAngle != value)
                {
                    vertAngle = value;
                    SetUIAngle(angle);
                }
            }
        }

        /// <summary>
        /// Calculated radius of the curved canvas. 
        /// </summary>
        public float Radius
        {
            get
            {
                if (radius == 0)
                    radius = GetCyllinderRadiusInCanvasSpace();

                return radius;
            }
        }

        /// <summary>
        /// Can the canvas be interacted with?
        /// </summary>
        public bool Interactable
        {
            get { return interactable; }
            set { interactable = value; }
        }

        /// <summary>
        /// Will the canvas block raycasts
        /// Settings this to false will destroy the canvas' collider.
        /// </summary>
        public bool BlocksRaycasts
        {
            get { return blocksRaycasts; }
            set
            {
                if (blocksRaycasts != value)
                {
                    blocksRaycasts = value;

                    //tell raycaster to update its collider now that angle has changed.
                    if (Application.isPlaying && GetComponent<HUDRaycaster>() != null)
                        GetComponent<HUDRaycaster>().RebuildCollider();
                }
            }
        }

        /// <summary>
        /// Should the raycaster take other layers into account to determine if canvas has been interacted with.
        /// </summary>
        public bool RaycastMyLayerOnly
        {
            get { return raycastMyLayerOnly; }
            set { raycastMyLayerOnly = value; }
        }

        #endregion

        #region --- Unity ---

        void Start()
        {
            if (Application.isPlaying)
            {

                //lets get rid of any raycasters and add our custom one
                GraphicRaycaster castie = GetComponent<GraphicRaycaster>();

                if (castie != null)
                {
                    if (!(castie is HUDRaycaster))
                    {
                        Destroy(castie);
                        this.gameObject.AddComponent<HUDRaycaster>();
                    }
                }
                else
                {
                    this.gameObject.AddComponent<HUDRaycaster>();
                }
            }

            //find needed references
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            radius = GetCyllinderRadiusInCanvasSpace();
        }

        void OnEnable()
        {
            //Redraw canvas object on enable.
            foreach (UnityEngine.UI.Graphic graph in (this).GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                graph.SetAllDirty();
            }
        }

        void OnDisable()
        {
            foreach (UnityEngine.UI.Graphic graph in (this).GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                graph.SetAllDirty();
            }
        }

        void Update()
        {

            //recreate the geometry if entire canvas has been resized
            if ((transform as RectTransform).rect.size != rectSize)
            {
                rectSize = (transform as RectTransform).rect.size;
                SetUIAngle(angle);
            }

            //check for improper canvas size
            if (rectSize.x == 0 || rectSize.y == 0)
                Debug.LogError("CurvedUI: Your Canvas size must be bigger than 0!");
        }
       
        #endregion

        #region --- Methods ---

        /// <summary>
        /// Changes the horizontal angle of the canvas.
        /// </summary>
        /// <param name="newAngle"></param>
        private void SetUIAngle(int newAngle)
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            //temp fix to make interactions with angle 0 possible
            if (newAngle == 0) newAngle = 1;

            angle = newAngle;

            radius = GetCyllinderRadiusInCanvasSpace();

            foreach (Graphic graph in GetComponentsInChildren<Graphic>())
                graph.SetVerticesDirty();

            if (Application.isPlaying && GetComponent<HUDRaycaster>() != null)
                //tell raycaster to update its collider now that angle has changed.
                GetComponent<HUDRaycaster>().RebuildCollider();
        }

        private Vector3 CanvasToSphere(Vector3 pos)
        {
            float radius = Radius;
            float vAngle;

            vAngle = angle * (rectSize.y / rectSize.x);
            radius += Angle > 0 ? -pos.z : pos.z;

            //convert planar coordinates to spherical coordinates
            float theta = (pos.x / rectSize.x).Remap(-0.5f, 0.5f, (180 - angle) / 2.0f - 90, 180 - (180 - angle) / 2.0f - 90);
            theta *= Mathf.Deg2Rad;
            float gamma = (pos.y / rectSize.y).Remap(-0.5f, 0.5f, (180 - vAngle) / 2.0f, 180 - (180 - vAngle) / 2.0f);
            gamma *= Mathf.Deg2Rad;

            pos.z = Mathf.Sin(gamma) * Mathf.Cos(theta) * radius;
            pos.y = -radius * Mathf.Cos(gamma);
            pos.x = Mathf.Sin(gamma) * Mathf.Sin(theta) * radius;
            pos.z -= radius;

            return pos;
        }

        /// <summary>
        /// Converts a point in Canvas space to a point on Curved surface in world space units. 
        /// </summary>
        /// <param name="pos">Position on canvas in canvas space</param>
        /// <returns>
        /// Position on curved canvas in world space.
        /// </returns>
        public Vector3 CanvasToCurvedCanvas(Vector3 pos)
        {
            pos = CanvasToSphere(pos);
            if (float.IsNaN(pos.x) || float.IsInfinity(pos.x)) return Vector3.zero;
            else return transform.localToWorldMatrix.MultiplyPoint3x4(pos);
        }

        /// <summary>
        /// Returns the radius of curved canvas cyllinder, expressed in Cavas's local space units.
        /// </summary>
        public float GetCyllinderRadiusInCanvasSpace()
        {
            float ret;
            ret = ((transform as RectTransform).rect.size.x / ((2 * Mathf.PI) * (angle / 360.0f)));

            return angle == 0 ? 0 : ret;
        }

        /// <summary>
        /// Tells you how big UI quads can get before they should be tesselate to look good on current canvas settings.
        /// Used by CurvedUIVertexEffect to determine how many quads need to be created for each graphic.
        /// </summary>
        public Vector2 GetSize(bool UnmodifiedByQuality = false)
        {
            Vector2 canvasSize = GetComponent<RectTransform>().rect.size;
            float ret = canvasSize.x;
            float ret2 = canvasSize.y;

            if (Angle != 0 )
            {
                ret = Mathf.Min(canvasSize.x / 4, canvasSize.x / (Mathf.Abs(angle).Remap(0.0f, 360.0f, 0, 1) * baseCircleSegments * 0.5f));
                ret2 = ret * canvasSize.y / canvasSize.x;
            }
            return new Vector2(ret, ret2) / (UnmodifiedByQuality ? 1 : Mathf.Clamp(10, 0.01f, 10.0f));
        }

        #endregion
    }
}

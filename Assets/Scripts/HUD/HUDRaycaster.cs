using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace CurvedHUD
{
    public class HUDRaycaster : GraphicRaycaster
    {

        [SerializeField]
        private bool showDebug = false;
        private bool overrideEventData = true;

        //Variables --------------------------------------//
        private Canvas canvas;
        private HUDSettings settings;
        private List<GameObject> objectsUnderPointer = new List<GameObject>();

        #region --- Unity ---

        protected override void Awake()
        {
            base.Awake();
            canvas = GetComponent<Canvas>();
            settings = GetComponent<HUDSettings>();

            //the canvas needs an event camera set up to process events correctly. Try to use main camera if no one is provided.
            if (canvas.worldCamera == null && Camera.main != null)
                canvas.worldCamera = Camera.main;
        }

        protected override void Start()
        {
            CreateCollider();
        }
        
        #endregion

        #region --- Methods ---

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {

            if (!settings.Interactable)
                return;

            //check if we have a world camera to process events by
            if (canvas.worldCamera == null)
                Debug.LogWarning("CurvedUIRaycaster requires Canvas to have a world camera reference to process events!", canvas.gameObject);

            Camera worldCamera = canvas.worldCamera;
            Ray ray3D;

            //get a ray to raycast with depending on the control method
           
            // Get a ray from the camera through the point on the screen - used for mouse input
            ray3D = worldCamera.ScreenPointToRay(eventData.position);
                
            //Create a copy of the eventData to be used by this canvas. This allows
            PointerEventData newEventData = new PointerEventData(EventSystem.current);
            if (!overrideEventData)
            {
                newEventData.pointerEnter = eventData.pointerEnter;
                newEventData.rawPointerPress = eventData.rawPointerPress;
                newEventData.pointerDrag = eventData.pointerDrag;
                newEventData.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
                newEventData.pointerPressRaycast = eventData.pointerPressRaycast;
                newEventData.hovered = new List<GameObject>();
                newEventData.hovered.AddRange(eventData.hovered);
                newEventData.eligibleForClick = eventData.eligibleForClick;
                newEventData.pointerId = eventData.pointerId;
                newEventData.position = eventData.position;
                newEventData.delta = eventData.delta;
                newEventData.pressPosition = eventData.pressPosition;
                newEventData.clickTime = eventData.clickTime;
                newEventData.clickCount = eventData.clickCount;
                newEventData.scrollDelta = eventData.scrollDelta;
                newEventData.useDragThreshold = eventData.useDragThreshold;
                newEventData.dragging = eventData.dragging;
                newEventData.button = eventData.button;
            }



            if (settings.Angle != 0 && settings.enabled)
            { // use custom raycasting only if Curved effect is enabled



                //Getting remappedPosition on the curved canvas ------------------------------//
                //This will be later passed to GraphicRaycaster so it can discover interactions as usual.
                //If we did not hit the curved canvas, return - no interactions are possible

                //Test only this object's layer if settings require it.
                int myLayerMask = -1;
                if (settings.RaycastMyLayerOnly)
                {
                    myLayerMask = 1 << this.gameObject.layer;
                }

                //Physical raycast to find interaction point
                Vector2 remappedPosition = eventData.position;

                if (!RaycastToSphereCanvas(ray3D, out remappedPosition, false, myLayerMask)) return;


                //Creating eventData for canvas Raycasting -------------------//
                //Which eventData were going to use?
                PointerEventData eventDataToUse = overrideEventData ? eventData : newEventData;

                // Swap event data pressPosition to our remapped pos if this is the frame of the press
                if (eventDataToUse.pressPosition == eventDataToUse.position)
                    eventDataToUse.pressPosition = remappedPosition;

                // Swap event data position to our remapped pos
                eventDataToUse.position = remappedPosition;
            }



            //store objects under pointer so they can quickly retrieved if needed by other scripts
            objectsUnderPointer = eventData.hovered;

            // Use base class raycast method to finish the raycast if we hit anything
            base.Raycast(overrideEventData ? eventData : newEventData, resultAppendList);

        }

        public virtual bool RaycastToSphereCanvas(Ray ray3D, out Vector2 o_canvasPos, bool OutputInCanvasSpace = false, int myLayerMask = -1)
        {

            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray3D, out hit, float.PositiveInfinity, myLayerMask))
            {
                //find if we hit this canvas
                if (overrideEventData && hit.collider.gameObject != this.gameObject)
                {
                    o_canvasPos = Vector2.zero;
                    return false;
                }

                Vector2 canvasSize = canvas.GetComponent<RectTransform>().rect.size;
                float radius = settings.GetCyllinderRadiusInCanvasSpace();

                //local hit point on canvas, direction from its center and a vector perpendicular to direction, so we can use it to calculate its angle in both planes.
                Vector3 localHitPoint = canvas.transform.worldToLocalMatrix.MultiplyPoint3x4(hit.point);
                Vector3 SphereCenter = new Vector3(0, 0, -radius);
                Vector3 directionFromSphereCenter = (localHitPoint - SphereCenter).normalized;
                Vector3 XZPlanePerpendicular = Vector3.Cross(directionFromSphereCenter, directionFromSphereCenter.ModifyY(0)).normalized * (directionFromSphereCenter.y < 0 ? 1 : -1);

                //horizontal and vertical angle between middle of the sphere and the hit point.
                //We do some fancy checks to determine vectors we compare them to,
                //to make sure they are negative on the left and bottom side of the canvas
                float hAngle = -AngleSigned(directionFromSphereCenter.ModifyY(0), (settings.Angle > 0 ? Vector3.forward : Vector3.back), (settings.Angle > 0 ? Vector3.up : Vector3.down));
                float vAngle = -AngleSigned(directionFromSphereCenter, directionFromSphereCenter.ModifyY(0), XZPlanePerpendicular);

                //find the size of the canvas expressed as measure of the arc it occupies on the sphere
                float hAngularSize = Mathf.Abs(settings.Angle) * 0.5f;
                float vAngularSize = Mathf.Abs(hAngularSize * canvasSize.y / canvasSize.x);

                //map the intersection point to 2d point in canvas space
                Vector2 pointOnCanvas = new Vector2(hAngle.Remap(-hAngularSize, hAngularSize, -canvasSize.x * 0.5f, canvasSize.x * 0.5f),
                                                    vAngle.Remap(-vAngularSize, vAngularSize, -canvasSize.y * 0.5f, canvasSize.y * 0.5f));

                if (showDebug)
                {
                    Debug.Log("h: " + hAngle + " / v: " + vAngle + " poc: " + pointOnCanvas);
                    Debug.DrawRay(canvas.transform.localToWorldMatrix.MultiplyPoint3x4(SphereCenter), canvas.transform.localToWorldMatrix.MultiplyVector(directionFromSphereCenter) * Mathf.Abs(radius), Color.red);
                    Debug.DrawRay(canvas.transform.localToWorldMatrix.MultiplyPoint3x4(SphereCenter), canvas.transform.localToWorldMatrix.MultiplyVector(XZPlanePerpendicular) * 300, Color.magenta);
                }

                if (OutputInCanvasSpace)
                    o_canvasPos = pointOnCanvas;
                else // convert the result to screen point in camera.This will be later used by raycaster and world camera to determine what we're pointing at
                    o_canvasPos = canvas.worldCamera.WorldToScreenPoint(canvas.transform.localToWorldMatrix.MultiplyPoint3x4(pointOnCanvas));

                return true;
            }

            o_canvasPos = Vector2.zero;
            return false;
        }

        /// <summary>
        /// Creates a mesh collider for curved canvas based on current angle and curve segments.
        /// </summary>
        /// <returns>The collider.</returns>
        protected void CreateCollider()
        {

            //remove all colliders on this object
            List<Collider> Cols = new List<Collider>();
            Cols.AddRange(this.GetComponents<Collider>());
            for (int i = 0; i < Cols.Count; i++)
            {
                Destroy(Cols[i]);
            }

            if (!settings.BlocksRaycasts) return; //null;

                        //rigidbody in parent?
             if (GetComponent<Rigidbody>() != null || GetComponentInParent<Rigidbody>() != null)
             Debug.LogWarning("CurvedUI: Sphere shape canvases as children of rigidbodies do not support user input. Switch to Cyllinder shape or remove the rigidbody from parent.", this.gameObject);

            SetupMeshColliderUsingMesh(CreateSphereColliderMesh());
            return;

        }

        /// <summary>
        /// Adds neccessary components and fills them with given mesh data.
        /// </summary>
        /// <param name="meshie"></param>
        private void SetupMeshColliderUsingMesh(Mesh meshie)
        {
            MeshFilter mf = this.AddComponentIfMissing<MeshFilter>();
            MeshCollider mc = this.gameObject.AddComponent<MeshCollider>();
            mf.mesh = meshie;
            mc.sharedMesh = meshie;
        }

        private Mesh CreateSphereColliderMesh()
        {

            Mesh meshie = new Mesh();

            Vector3[] Corners = new Vector3[4];
            (canvas.transform as RectTransform).GetWorldCorners(Corners);

            List<Vector3> verts = new List<Vector3>(Corners);
            for (int i = 0; i < verts.Count; i++)
            {
                verts[i] = settings.transform.worldToLocalMatrix.MultiplyPoint3x4(verts[i]);
            }

            if (settings.Angle != 0)
            {
                // Tesselate quads and apply transformation
                int startingVertexCount = verts.Count;
                for (int i = 0; i < startingVertexCount; i += 4)
                    ModifyQuad(verts, i, settings.GetSize(true));

                // Remove old quads
                verts.RemoveRange(0, startingVertexCount);

                //curve verts
                float vangle = settings.VerticalAngle;
                float cylinder_angle = settings.Angle;
                Vector2 canvasSize = (canvas.transform as RectTransform).rect.size;
                float radius = settings.GetCyllinderRadiusInCanvasSpace();

                //caluclate vertical angle for aspect - consistent mapping
                vangle = settings.Angle * (canvasSize.y / canvasSize.x);

                //curve the vertices 
                for (int i = 0; i < verts.Count; i++)
                {

                    float theta = (verts[i].x / canvasSize.x).Remap(-0.5f, 0.5f, (180 - cylinder_angle) / 2.0f - 90, 180 - (180 - cylinder_angle) / 2.0f - 90);
                    theta *= Mathf.Deg2Rad;
                    float gamma = (verts[i].y / canvasSize.y).Remap(-0.5f, 0.5f, (180 - vangle) / 2.0f, 180 - (180 - vangle) / 2.0f);
                    gamma *= Mathf.Deg2Rad;

                    verts[i] = new Vector3(Mathf.Sin(gamma) * Mathf.Sin(theta) * radius,
                        -radius * Mathf.Cos(gamma),
                        Mathf.Sin(gamma) * Mathf.Cos(theta) * radius + -radius);
                }
            }
            meshie.vertices = verts.ToArray();

            //create triangles from verts
            List<int> tris = new List<int>();
            for (int i = 0; i < verts.Count; i += 4)
            {
                tris.Add(i + 0);
                tris.Add(i + 1);
                tris.Add(i + 2);

                tris.Add(i + 3);
                tris.Add(i + 0);
                tris.Add(i + 2);
            }


            meshie.triangles = tris.ToArray();
            return meshie;
        }

        /// <summary>
        /// Determine the signed angle between two vectors, with normal 'n'
        /// as the rotation axis.
        /// </summary>
        float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            return Mathf.Atan2(
                Vector3.Dot(n, Vector3.Cross(v1, v2)),
                Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
        }

        private bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        /// <summary>
        /// REturns a screen point under which a ray intersects the curved canvas in its event camera view
        /// </summary>
        /// <returns><c>true</c>, if screen space point by ray was gotten, <c>false</c> otherwise.</returns>
        /// <param name="ray">Ray.</param>
        /// <param name="o_positionOnCanvas">O position on canvas.</param>
        bool GetScreenSpacePointByRay(Ray ray, out Vector2 o_positionOnCanvas)
        {
            return RaycastToSphereCanvas(ray, out o_positionOnCanvas, false);
        }

        public void RebuildCollider()
        {
            CreateCollider();
        }

        /// <summary>
        /// Returns all objects currently under the pointer
        /// </summary>
        /// <returns>The objects under pointer.</returns>
        public List<GameObject> GetObjectsUnderPointer()
        {
            if (objectsUnderPointer == null) objectsUnderPointer = new List<GameObject>();
            return objectsUnderPointer;
        }

        /// <summary>
        /// Returns all the canvas objects that are intersected by given ray
        /// </summary>
        /// <returns>The objects hit by ray.</returns>
        /// <param name="ray">Ray.</param>
        public List<GameObject> GetObjectsHitByRay(Ray ray)
        {
            List<GameObject> results = new List<GameObject>();

            Vector2 pointerPosition;

            //ray outside the canvas, return null
            if (!GetScreenSpacePointByRay(ray, out pointerPosition))
                return results;

            //lets find the graphics under ray!
            List<Graphic> s_SortedGraphics = new List<Graphic>();
            var foundGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            for (int i = 0; i < foundGraphics.Count; ++i)
            {
                Graphic graphic = foundGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1 || !graphic.raycastTarget)
                    continue;

                if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera))
                    continue;

                if (graphic.Raycast(pointerPosition, eventCamera))
                    s_SortedGraphics.Add(graphic);

            }

            s_SortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
            for (int i = 0; i < s_SortedGraphics.Count; ++i)
                results.Add(s_SortedGraphics[i].gameObject);

            s_SortedGraphics.Clear();

            return results;
        }

        /// <summary>
        /// Sends OnClick event to every Button under pointer.
        /// </summary>
        public void Click()
        {
            for (int i = 0; i < GetObjectsUnderPointer().Count; i++)
            {
                Button butt = GetObjectsUnderPointer()[i].GetComponent<Button>();
                if (butt)
                {
                    butt.onClick.Invoke();
                    if (showDebug) Debug.Log("Clicked on: " + butt.gameObject.name, butt.gameObject);
                }
            }
        }

        private void ModifyQuad(List<Vector3> verts, int vertexIndex, Vector2 requiredSize)
        {

            // Read the existing quad vertices
            List<Vector3> quad = new List<Vector3>();
            for (int i = 0; i < 4; i++)
                quad.Add(verts[vertexIndex + i]);

            // horizotal and vertical directions of a quad. We're going to tesselate parallel to these.
            Vector3 horizontalDir = quad[2] - quad[1];
            Vector3 verticalDir = quad[1] - quad[0];

            // Find how many quads we need to create
            int horizontalQuads = Mathf.CeilToInt(horizontalDir.magnitude * (1.0f / Mathf.Max(1.0f, requiredSize.x)));
            int verticalQuads = Mathf.CeilToInt(verticalDir.magnitude * (1.0f / Mathf.Max(1.0f, requiredSize.y)));

            // Create the quads!
            float yStart = 0.0f;
            for (int y = 0; y < verticalQuads; ++y)
            {

                float yEnd = (y + 1.0f) / verticalQuads;
                float xStart = 0.0f;

                for (int x = 0; x < horizontalQuads; ++x)
                {
                    float xEnd = (x + 1.0f) / horizontalQuads;

                    //Add new quads to list
                    verts.Add(TesselateQuad(quad, xStart, yStart));
                    verts.Add(TesselateQuad(quad, xStart, yEnd));
                    verts.Add(TesselateQuad(quad, xEnd, yEnd));
                    verts.Add(TesselateQuad(quad, xEnd, yStart));

                    //begin the next quad where we ened this one
                    xStart = xEnd;
                }
                //begin the next row where we ended this one
                yStart = yEnd;
            }
        }

        private Vector3 TesselateQuad(List<Vector3> quad, float x, float y)
        {

            Vector3 ret = Vector3.zero;

            //1. calculate weighting factors
            List<float> weights = new List<float>(){
                (1-x) * (1-y),
                (1-x) * y,
                x * y,
                x * (1-y),
            };

            //2. interpolate pos using weighting factors
            for (int i = 0; i < 4; i++)
                ret += quad[i] * weights[i];
            return ret;
        }

        #endregion
    }
}

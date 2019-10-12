using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace CurvedHUD
{
    public partial class HUDVertexEffect : BaseMeshEffect
    {
        #region --- Fields ---

        VertexHelper vertexHelper; //used int 5.2 and later
        List<UIVertex> vertices;

       
        //public settings
        [Tooltip("Check to skip tesselation pass on this object. CurvedUI will not create additional vertices to make this object have a smoother curve. Checking this can solve some issues if you create your own procedural mesh for this object. Default false.")]
        public bool DoNotTesselate = false;

        //settings
        private bool tesselationRequired = true;
        private bool curvingRequired = true;

        //saved variables and references
        private float angle = 90;
        private Canvas canvas;
        private HUDSettings settings;

        //internal
        private Matrix4x4 canvasToWorld;
        private Matrix4x4 canvasToLocal;
        private Matrix4x4 toWorld;
        private Matrix4x4 toLocal;

        private Vector3 position;
        private Vector3 up;
        private Vector2 rectSize;
        private Color color;
        private Vector2 textUV0;
        private float fill;

        private List<UIVertex> tesselatedVerts;

        //my components references
        private Graphic myGraphic;
        private Image myImage;
        private Text myText;

        #endregion


        #region --- Unity ---

        protected override void OnEnable()
        {
            //find the settings object and its canvas.
            FindParentSettings();

            //If there is an update to the graphic, we cant reuse old vertices, so new tesselation will be required
            myGraphic = GetComponent<Graphic>();
            if (myGraphic)
            {
                myGraphic.RegisterDirtyMaterialCallback(TesselationRequiredCallback);
                myGraphic.SetVerticesDirty();
            }


            myText = GetComponent<Text>();
            if (myText)
            {
                myText.RegisterDirtyVerticesCallback(TesselationRequiredCallback);
                Font.textureRebuilt += FontTextureRebuiltCallback;
            }
        }

        protected override void OnDisable()
        {

            //If there is an update to the graphic, we cant reuse old vertices, so new tesselation will be required
            if (myGraphic)
                myGraphic.UnregisterDirtyMaterialCallback(TesselationRequiredCallback);

            if (myText)
            {
                myText.UnregisterDirtyVerticesCallback(TesselationRequiredCallback);
                Font.textureRebuilt -= FontTextureRebuiltCallback;
            }
        }

        void Update()
        {
            //Find if the change in transform requires us to retesselate the UI
            if (!tesselationRequired)
            { // do not perform tesselation required check if we already know it is, god damnit!

                if ((transform as RectTransform).rect.size != rectSize)
                {
                    //the size of this RectTransform has changed, we have to tesselate again! =(
                    //Debug.Log("tess required - size");
                    tesselationRequired = true;
                }
                else if (myGraphic != null)//test for color changes if it has a graphic component
                {
                    if (myGraphic.color != color)
                    {
                        tesselationRequired = true;
                        color = myGraphic.color;
                        //Debug.Log("tess req - color");
                    }
                    else if (myImage != null)
                    {
                        if (myImage.fillAmount != fill)
                        {
                            tesselationRequired = true;
                            fill = myImage.fillAmount;
                        }
                    }

                }
            }

            if (!tesselationRequired && !curvingRequired) // do not perform a check if we're already tesselating or curving. Tesselation includes curving.
            {
                //test if position in canvas's local space has been changed. We would need to recalculate vertices again
                Vector3 testedPos = settings.transform.worldToLocalMatrix.MultiplyPoint3x4(this.transform.position);
                if (!testedPos.AlmostEqual(position))
                {

                    //we dont have to curve vertices if we only moved the object vertically in a cylinder.
                    if (Mathf.Pow(testedPos.x - position.x, 2) > 0.00001 || Mathf.Pow(testedPos.z - position.z, 2) > 0.00001)
                    {
                        position = testedPos;
                        curvingRequired = true;
                        // Debug.Log("crv req - tested pos: " + testedPos, this.gameObject);
                    }
                }

                //test this object's rotation in relation to canvas.
                Vector3 testedUp = settings.transform.worldToLocalMatrix.MultiplyVector(this.transform.up).normalized;
                if (!up.AlmostEqual(testedUp, 0.0001))
                {
                    bool testedEqual = testedUp.AlmostEqual(Vector3.up.normalized);
                    bool savedEqual = up.AlmostEqual(Vector3.up.normalized);

                    //special case - if we change the z angle from or to 0, we need to retesselate to properly display geometry in cyllinder
                    if ((!testedEqual && savedEqual) || (testedEqual && !savedEqual))
                        tesselationRequired = true;

                    up = testedUp;
                    curvingRequired = true;
                    //Debug.Log("crv req - tested up: " + testedUp);
                }

            }

            ////if we find we need to make a change in the mesh, set vertices dirty to trigger BaseMeshEffect firing.
            if (myGraphic && (tesselationRequired || curvingRequired)) myGraphic.SetVerticesDirty();
        }

        #endregion

        #region --- Methods ---

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!this.IsActive())
                return;

            if (settings == null)
                FindParentSettings();

            if (settings == null || !settings.enabled)
                return;

            //check for changes in text font material that would mean a retesselation in required to get fresh UV's
            CheckTextFontMaterial();

            //if curving or tesselation is required, we'll run the code to calculate vertices.
            if (tesselationRequired || curvingRequired || vertexHelper == null || vertexHelper.currentVertCount == 0)
            {
                //Debug.Log("updating: tes:" + tesselationRequired + ", crv:" + curvingRequired, this.gameObject);
                //Get vertices from the vertex stream. These come as triangles.
                vertices = new List<UIVertex>();
                vh.GetUIVertexStream(vertices);

                // calls the old ModifyVertices which was used on pre 5.2. 
                ModifyVerts(vertices);

                //create or reuse our temp vertexhelper
                if (vertexHelper == null)
                    vertexHelper = new VertexHelper();
                else
                    vertexHelper.Clear();


                //Save our tesselated and curved vertices to new vertex helper. They can come as quads or as triangles.
                if (vertices.Count % 4 == 0)
                {
                    for (int i = 0; i < vertices.Count; i += 4)
                    {
                        vertexHelper.AddUIVertexQuad(new UIVertex[]{
                        vertices[i + 0], vertices[i + 1], vertices[i + 2], vertices[i + 3],
                    });
                    }
                }
                else
                    vertexHelper.AddUIVertexTriangleStream(vertices);

                //download proper vertex stream to a list we're going to save
                vertexHelper.GetUIVertexStream(vertices);
                curvingRequired = false;
            }

            //copy the saved verts list to current VertexHelper
            vh.Clear();
            vh.AddUIVertexTriangleStream(vertices);
        }

        /// <summary>
        /// Subscribed to graphic componenet to find out when vertex information changes and we need to create new geometry based on that.
        /// </summary>
        private void TesselationRequiredCallback()
        {
            tesselationRequired = true;
        }

        /// <summary>
        /// Called by Font class to let us know font atlas has ben rebuilt and we need to update our vertices.
        /// </summary>
        private void FontTextureRebuiltCallback(Font fontie)
        {
            if (myText.font == fontie)
                tesselationRequired = true;
        }

        private void CheckTextFontMaterial()
        {
            //we check for a sudden change in text's fontMaterialTexture. This is a very hacky way, but the only one working reliably for now.
            if (myText)
            {
                if (myText.cachedTextGenerator.verts.Count > 0 && myText.cachedTextGenerator.verts[0].uv0 != textUV0)
                {
                    //Debug.Log("tess req - texture");
                    textUV0 = myText.cachedTextGenerator.verts[0].uv0;
                    tesselationRequired = true;
                }
            }
        }

        public HUDSettings FindParentSettings(bool forceNew = false)
        {
            if (settings == null || forceNew)
            {
                settings = GetComponentInParent<HUDSettings>();

                if (settings == null) return null;
                else
                {
                    canvas = settings.GetComponent<Canvas>();
                    angle = settings.Angle;

                    myImage = GetComponent<Image>();
                }
            }

            return settings;
        }

        private void ModifyVerts(List<UIVertex> verts)
        {

            if (verts == null || verts.Count == 0) return;

            //update transformation matrices we're going to use in curving the verts.
            canvasToWorld = canvas.transform.localToWorldMatrix;
            canvasToLocal = canvas.transform.worldToLocalMatrix;
            toWorld = transform.localToWorldMatrix;
            toLocal = transform.worldToLocalMatrix;

            //tesselate the vertices if needed and save them to a list,
            //so we don't have to retesselate if RectTransform's size has not changed.
            if (tesselationRequired || !Application.isPlaying)
            {


                TesselateGeometry(verts);


                // Save the tesselated vertices, so if the size does not change,
                // we can use them when redrawing vertices.
                tesselatedVerts = new List<UIVertex>(verts);

                //save the transform properties we last tesselated for.
                rectSize = (transform as RectTransform).rect.size;

                tesselationRequired = false;
            }


            //lets get some values needed for curving from settings
            angle = settings.Angle;
            float radius = settings.GetCyllinderRadiusInCanvasSpace();
            Vector2 canvasSize = (canvas.transform as RectTransform).rect.size;

            int initialCount = verts.Count;

            if (tesselatedVerts != null)
            { // use saved verts if we have those

                UIVertex[] copiedVerts = new UIVertex[tesselatedVerts.Count];
                for (int i = 0; i < tesselatedVerts.Count; i++)
                {
                    copiedVerts[i] = CurveVertex(tesselatedVerts[i], angle, radius, canvasSize);
                }
                verts.AddRange(copiedVerts);
                verts.RemoveRange(0, initialCount);

            }
            else
            { // or just the mesh's vertices if we do not

                UIVertex[] copiedVerts = new UIVertex[verts.Count];
                for (int i = 0; i < initialCount; i++)
                {
                    copiedVerts[i] = CurveVertex(verts[i], angle, radius, canvasSize);
                }
                verts.AddRange(copiedVerts);
                verts.RemoveRange(0, initialCount);

            }
        }

        /// <summary>
        /// Map position of a vertex to a section of a circle. calculated in canvas's local space
        /// </summary>
        private UIVertex CurveVertex(UIVertex input, float cylinder_angle, float radius, Vector2 canvasSize)
        {
            Vector3 pos = input.position;

            //calculated in canvas local space version:
            pos = canvasToLocal.MultiplyPoint3x4(toWorld.MultiplyPoint3x4(pos));

            if (settings.Angle != 0)
            {

                float vangle = settings.VerticalAngle;
                float savedZ = -pos.z;

                vangle = cylinder_angle * (canvasSize.y / canvasSize.x);

                //convert planar coordinates to spherical coordinates
                float theta = (pos.x / canvasSize.x).Remap(-0.5f, 0.5f, (180 - cylinder_angle) / 2.0f - 90, 180 - (180 - cylinder_angle) / 2.0f - 90);
                theta *= Mathf.Deg2Rad;
                float gamma = (pos.y / canvasSize.y).Remap(-0.5f, 0.5f, (180 - vangle) / 2.0f, 180 - (180 - vangle) / 2.0f);
                gamma *= Mathf.Deg2Rad;

                pos.z = Mathf.Sin(gamma) * Mathf.Cos(theta) * (radius + savedZ);
                pos.y = -(radius + savedZ) * Mathf.Cos(gamma);
                pos.x = Mathf.Sin(gamma) * Mathf.Sin(theta) * (radius + savedZ);
                pos.z -= radius;
            }

            //4. write output
            input.position = toLocal.MultiplyPoint3x4(canvasToWorld.MultiplyPoint3x4(pos));

            return input;
        }

        private void TesselateGeometry(List<UIVertex> verts)
        {
            Vector2 tessellatedSize = settings.GetSize();

            /// Convert the list from triangles to quads to be used by the tesselation
            TrianglesToQuads(verts);

            //do not tesselate text verts. Text usually is small and has plenty of verts already.
            if (myText == null && !DoNotTesselate)
            {
                // Tesselate quads and apply transformation
                int startingVertexCount = verts.Count;
                for (int i = 0; i < startingVertexCount; i += 4)
                    ModifyQuad(verts, i, tessellatedSize);

                // Remove old quads
                verts.RemoveRange(0, startingVertexCount);
            }

        }

        private void ModifyQuad(List<UIVertex> verts, int vertexIndex, Vector2 requiredSize)
        {

            // Read the existing quad vertices
            UIVertex[] quad = new UIVertex[4];
            for (int i = 0; i < 4; i++)
                quad[i] = verts[vertexIndex + i];

            // horizotal and vertical directions of a quad. We're going to tesselate parallel to these.
            Vector3 horizontalDir = quad[2].position - quad[1].position;
            Vector3 verticalDir = quad[1].position - quad[0].position;

            //To make sure filled image is properly tesselated, were going to find the bigger side of the quad.
            if (myImage != null && myImage.type == Image.Type.Filled)
            {
                horizontalDir = (horizontalDir).x > (quad[3].position - quad[0].position).x ? horizontalDir : quad[3].position - quad[0].position;
                verticalDir = (verticalDir).y > (quad[2].position - quad[3].position).y ? verticalDir : quad[2].position - quad[3].position;
            }

            // Find how many quads we need to create
            int horizontalQuads = 1;
            int verticalQuads = 1;

            // Tesselate vertically only if the recttransform (or parent) is rotated
            // This cuts down the time needed to tesselate by 90% in some cases.
            verticalQuads = Mathf.CeilToInt(verticalDir.magnitude * (1.0f / Mathf.Max(1.0f, requiredSize.y)));

            horizontalQuads = Mathf.CeilToInt(horizontalDir.magnitude * (1.0f / Mathf.Max(1.0f, requiredSize.x)));

            bool oneVert = false;
            bool oneHori = false;

            // Create the quads!
            float yStart = 0.0f;
            for (int y = 0; y < verticalQuads || !oneVert; ++y)
            {
                oneVert = true;
                float yEnd = (y + 1.0f) / verticalQuads;
                float xStart = 0.0f;

                for (int x = 0; x < horizontalQuads || !oneHori; ++x)
                {
                    oneHori = true;

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

        private void TrianglesToQuads(List<UIVertex> verts)
        {

            int addCount = 0;
            int vertsInTrisCount = verts.Count;
            UIVertex[] vertsInQuads = new UIVertex[vertsInTrisCount / 6 * 4];

            for (int i = 0; i < vertsInTrisCount; i += 6)
            {
                // Get four corners from two triangles. Basic UI always comes in quads anyway.
                vertsInQuads[addCount++] = (verts[i + 0]);
                vertsInQuads[addCount++] = (verts[i + 1]);
                vertsInQuads[addCount++] = (verts[i + 2]);
                vertsInQuads[addCount++] = (verts[i + 4]);
            }
            //add quads to the list and remove the triangles
            verts.AddRange(vertsInQuads);
            verts.RemoveRange(0, vertsInTrisCount);
        }

        private UIVertex TesselateQuad(UIVertex[] quad, float x, float y)
        {

            UIVertex ret = new UIVertex();

            //1. calculate weighting factors
            float[] weights = new float[4]{
                (1-x) * (1-y),
                (1-x) * y,
                x * y,
                x * (1-y),
            };

            //2. interpolate all the vertex properties using weighting factors
            Vector2 uv0 = Vector2.zero, uv1 = Vector2.zero;
            Vector3 pos = Vector3.zero;

            for (int i = 0; i < 4; i++)
            {
                uv0 += quad[i].uv0 * weights[i];
                uv1 += quad[i].uv1 * weights[i];
                pos += quad[i].position * weights[i];
                //normal += quad[i].normal * weights[i]; // normals should be recalculated to take the curve into account;
                //tan += quad[i].tangent * weights[i]; // tangents should be recalculated to take the curve into account;
            }


            //4. return output
            ret.position = pos;
            //ret.color = Color32.Lerp(Color32.Lerp(quad[3].color, quad[1].color, y), Color32.Lerp(quad[0].color, quad[2].color, y), x);
            ret.color = quad[0].color; //used instead to save performance. Color lerps are expensive.
            ret.uv0 = uv0;
            ret.uv1 = uv1;
            ret.normal = quad[0].normal;
            ret.tangent = quad[0].tangent;

            return ret;
        }
        
        #endregion
    }

    public static class Math
    {
        /// <summary>
        ///Direct Vector3 comparison can produce wrong results sometimes due to float inacuracies.
        ///This is an aproximate comparison.
        /// <returns></returns>
        public static bool AlmostEqual(this Vector3 a, Vector3 b, double accuracy = 0.01)
        {
            return Vector3.SqrMagnitude(a - b) < accuracy;
        }

        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static float RemapAndClamp(this float value, float from1, float to1, float from2, float to2)
        {
            return value.Remap(from1, to1, from2, to2).Clamp(from2, to2);
        }


        public static float Remap(this int value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;

        }

        public static double Remap(this double value, double from1, double to1, double from2, double to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }



        public static float Clamp(this float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        public static float Clamp(this int value, int min, int max)
        {
            return Mathf.Clamp(value, min, max);
        }

        public static int Abs(this int value)
        {
            return Mathf.Abs(value);
        }

        public static float Abs(this float value)
        {
            return Mathf.Abs(value);
        }


        /// <summary>
        /// Returns value rounded to nearest integer (both up and down).
        /// </summary>
        /// <returns>The int.</returns>
        /// <param name="value">Value.</param>
        public static int ToInt(this float value)
        {
            return Mathf.RoundToInt(value);
        }

        public static int FloorToInt(this float value)
        {
            return Mathf.FloorToInt(value);
        }

        public static int CeilToInt(this float value)
        {
            return Mathf.FloorToInt(value);
        }

        public static Vector3 ModifyX(this Vector3 trans, float newVal)
        {
            trans = new Vector3(newVal, trans.y, trans.z);
            return trans;
        }

        public static Vector3 ModifyY(this Vector3 trans, float newVal)
        {
            trans = new Vector3(trans.x, newVal, trans.z);
            return trans;
        }

        public static Vector3 ModifyZ(this Vector3 trans, float newVal)
        {
            trans = new Vector3(trans.x, trans.y, newVal);
            return trans;
        }

        public static Vector2 ModifyVectorX(this Vector2 trans, float newVal)
        {
            trans = new Vector3(newVal, trans.y);
            return trans;
        }

        public static Vector2 ModifyVectorY(this Vector2 trans, float newVal)
        {
            trans = new Vector3(trans.x, newVal);
            return trans;
        }


        /// <summary>
        /// Resets transform's local position, rotation and scale
        /// </summary>
        /// <param name="trans">Trans.</param>
        public static void ResetTransform(this Transform trans)
        {
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
        }

        public static T AddComponentIfMissing<T>(this GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null)
            {
                return go.AddComponent<T>();
            }
            else return go.GetComponent<T>();
        }

        /// <summary>
        /// Checks if given component is preset and if not, adds it and returns it.
        /// </summary>
        public static T AddComponentIfMissing<T>(this Component go) where T : Component
        {
            return go.gameObject.AddComponentIfMissing<T>();
        }
    }
}

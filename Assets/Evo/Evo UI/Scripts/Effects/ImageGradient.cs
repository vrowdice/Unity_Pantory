using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL)]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("Evo/UI/Effects/Image Gradient")]
    public class ImageGradient : BaseMeshEffect
    {
        [Header("Gradient")]
        public Gradient gradientEffect = new() { colorKeys = new GradientColorKey[] { new(Color.black, 0), new(Color.white, 1) } };

        [Header("Settings")]
        public Type gradientType;
        [Range(-1, 1)] public float effectOffset = 0f;
        [Range(0.1f, 10)] public float zoom = 1f;

        // Cache
        Graphic cachedGraphic;

        public enum Type
        {
            Horizontal,
            Vertical
        }

        protected override void Awake()
        {
            base.Awake();
            cachedGraphic = GetComponent<Graphic>();
        }

        /// <summary>
        ///     Updates the gradient by marking vertices as dirty
        /// </summary>
        public void UpdateGradient()
        {
            if (cachedGraphic != null)
            {
                cachedGraphic.SetVerticesDirty();
            }
        }

        /// <summary>
        ///     Sets a new gradient and updates the visual
        /// </summary>
        public void SetGradient(Gradient newGradient)
        {
            gradientEffect = newGradient;
            UpdateGradient();
        }

        /// <summary>
        ///     Sets gradient colors for simple two-color gradients
        /// </summary>
        public void SetGradientColors(Color startColor, Color endColor)
        {
            gradientEffect = new Gradient
            {
                colorKeys = new GradientColorKey[]
                {
                    new(startColor, 0f),
                    new(endColor, 1f)
                },
                alphaKeys = new GradientAlphaKey[]
                {
                    new(startColor.a, 0f),
                    new(endColor.a, 1f)
                }
            };
            UpdateGradient();
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0)
                return;

            List<UIVertex> _vertexList = new();
            helper.GetUIVertexStream(_vertexList);
            int nCount = _vertexList.Count;

            switch (gradientType)
            {
                case Type.Horizontal:
                case Type.Vertical:
                    {
                        Rect bounds = GetBounds(_vertexList);
                        float min = bounds.xMin;
                        float w = bounds.width;
                        Func<UIVertex, float> GetPosition = v => v.position.x;

                        if (gradientType == Type.Vertical)
                        {
                            min = bounds.yMin;
                            w = bounds.height;
                            GetPosition = v => v.position.y;
                        }

                        float width = w == 0f ? 0f : 1f / w / zoom;
                        float zoomoffset = (1 - (1 / zoom)) * 0.5f;
                        float offset = (effectOffset * (1 - zoomoffset)) - zoomoffset;

                        SplitTrianglesAtGradientStops(_vertexList, bounds, zoomoffset, helper);

                        UIVertex vertex = new();
                        for (int i = 0; i < helper.currentVertCount; i++)
                        {
                            helper.PopulateUIVertex(ref vertex, i);
                            vertex.color = BlendColor(vertex.color, gradientEffect.Evaluate((GetPosition(vertex) - min) * width - offset));
                            helper.SetUIVertex(vertex, i);
                        }
                    }
                    break;
            }
        }

        Rect GetBounds(List<UIVertex> vertices)
        {
            float left = vertices[0].position.x;
            float right = left;
            float bottom = vertices[0].position.y;
            float top = bottom;

            for (int i = vertices.Count - 1; i >= 1; --i)
            {
                float x = vertices[i].position.x;
                float y = vertices[i].position.y;

                if (x > right) { right = x; }
                else if (x < left) { left = x; }

                if (y > top) { top = y; }
                else if (y < bottom) { bottom = y; }
            }

            return new Rect(left, bottom, right - left, top - bottom);
        }

        void SplitTrianglesAtGradientStops(List<UIVertex> _vertexList, Rect bounds, float zoomoffset, VertexHelper helper)
        {
            List<float> stops = FindStops(zoomoffset, bounds);
            if (stops.Count > 0)
            {
                helper.Clear();
                int nCount = _vertexList.Count;

                for (int i = 0; i < nCount; i += 3)
                {
                    float[] positions = GetPositions(_vertexList, i);
                    List<int> originIndices = new(3);
                    List<UIVertex> starts = new(3);
                    List<UIVertex> ends = new(2);

                    for (int s = 0; s < stops.Count; s++)
                    {
                        int initialCount = helper.currentVertCount;
                        bool hadEnds = ends.Count > 0;
                        bool earlyStart = false;

                        for (int p = 0; p < 3; p++)
                        {
                            if (!originIndices.Contains(p) && positions[p] < stops[s])
                            {
                                int p1 = (p + 1) % 3;
                                var start = _vertexList[p + i];

                                if (positions[p1] > stops[s])
                                {
                                    originIndices.Insert(0, p);
                                    starts.Insert(0, start);
                                    earlyStart = true;
                                }

                                else
                                {
                                    originIndices.Add(p);
                                    starts.Add(start);
                                }
                            }
                        }

                        if (originIndices.Count == 0) { continue; }
                        if (originIndices.Count == 3) { break; }

                        foreach (var start in starts)
                        {
                            helper.AddVert(start);
                        }

                        ends.Clear();
                        foreach (int index in originIndices)
                        {
                            int oppositeIndex = (index + 1) % 3;
                            if (positions[oppositeIndex] < stops[s]) { oppositeIndex = (oppositeIndex + 1) % 3; }
                            ends.Add(CreateSplitVertex(_vertexList[index + i], _vertexList[oppositeIndex + i], stops[s]));
                        }

                        if (ends.Count == 1)
                        {
                            int oppositeIndex = (originIndices[0] + 2) % 3;
                            ends.Add(CreateSplitVertex(_vertexList[originIndices[0] + i], _vertexList[oppositeIndex + i], stops[s]));
                        }

                        foreach (var end in ends)
                        {
                            helper.AddVert(end);
                        }

                        if (hadEnds)
                        {
                            helper.AddTriangle(initialCount - 2, initialCount, initialCount + 1);
                            helper.AddTriangle(initialCount - 2, initialCount + 1, initialCount - 1);

                            if (starts.Count > 0)
                            {
                                if (earlyStart) { helper.AddTriangle(initialCount - 2, initialCount + 3, initialCount); }
                                else { helper.AddTriangle(initialCount + 1, initialCount + 3, initialCount - 1); }
                            }
                        }

                        else
                        {
                            int vertexCount = helper.currentVertCount;
                            helper.AddTriangle(initialCount, vertexCount - 2, vertexCount - 1);
                            if (starts.Count > 1) { helper.AddTriangle(initialCount, vertexCount - 1, initialCount + 1); }
                        }

                        starts.Clear();
                    }

                    if (ends.Count > 0)
                    {
                        if (starts.Count == 0)
                        {
                            for (int p = 0; p < 3; p++)
                            {
                                if (!originIndices.Contains(p) && positions[p] > stops[^1])
                                {
                                    int p1 = (p + 1) % 3;
                                    UIVertex end = _vertexList[p + i];

                                    if (positions[p1] > stops[^1]) { starts.Insert(0, end); }
                                    else { starts.Add(end); }
                                }
                            }
                        }

                        foreach (var start in starts)
                            helper.AddVert(start);

                        int vertexCount = helper.currentVertCount;

                        if (starts.Count > 1)
                        {
                            helper.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 1);
                            helper.AddTriangle(vertexCount - 4, vertexCount - 1, vertexCount - 3);
                        }

                        else if (starts.Count > 0)
                        {
                            helper.AddTriangle(vertexCount - 3, vertexCount - 1, vertexCount - 2);
                        }
                    }

                    else
                    {
                        helper.AddVert(_vertexList[i]);
                        helper.AddVert(_vertexList[i + 1]);
                        helper.AddVert(_vertexList[i + 2]);
                        int vertexCount = helper.currentVertCount;
                        helper.AddTriangle(vertexCount - 3, vertexCount - 2, vertexCount - 1);
                    }
                }
            }
        }

        float[] GetPositions(List<UIVertex> _vertexList, int index)
        {
            float[] positions = new float[3];

            if (gradientType == Type.Horizontal)
            {
                positions[0] = _vertexList[index].position.x;
                positions[1] = _vertexList[index + 1].position.x;
                positions[2] = _vertexList[index + 2].position.x;
            }

            else
            {
                positions[0] = _vertexList[index].position.y;
                positions[1] = _vertexList[index + 1].position.y;
                positions[2] = _vertexList[index + 2].position.y;
            }

            return positions;
        }

        List<float> FindStops(float zoomoffset, Rect bounds)
        {
            List<float> stops = new();
            var offset = effectOffset * (1 - zoomoffset);
            var startBoundary = zoomoffset - offset;
            var endBoundary = (1 - zoomoffset) - offset;

            foreach (var color in gradientEffect.colorKeys)
            {
                if (color.time >= endBoundary) { break; }
                if (color.time > startBoundary) { stops.Add((color.time - startBoundary) * zoom); }
            }

            foreach (var alpha in gradientEffect.alphaKeys)
            {
                if (alpha.time >= endBoundary) { break; }
                if (alpha.time > startBoundary) { stops.Add((alpha.time - startBoundary) * zoom); }
            }

            float min = bounds.xMin;
            float size = bounds.width;

            if (gradientType == Type.Vertical)
            {
                min = bounds.yMin;
                size = bounds.height;
            }

            stops.Sort();

            for (int i = 0; i < stops.Count; i++)
            {
                stops[i] = (stops[i] * size) + min;

                if (i > 0 && Math.Abs(stops[i] - stops[i - 1]) < 2)
                {
                    stops.RemoveAt(i);
                    --i;
                }
            }

            return stops;
        }

        UIVertex CreateSplitVertex(UIVertex vertex1, UIVertex vertex2, float stop)
        {
            if (gradientType == Type.Horizontal)
            {
                float sx = vertex1.position.x - stop;
                float dx = vertex1.position.x - vertex2.position.x;
                float dy = vertex1.position.y - vertex2.position.y;
                float uvx = vertex1.uv0.x - vertex2.uv0.x;
                float uvy = vertex1.uv0.y - vertex2.uv0.y;
                float ratio = sx / dx;
                float splitY = vertex1.position.y - (dy * ratio);

                UIVertex splitVertex = new()
                {
                    position = new Vector3(stop, splitY, vertex1.position.z),
                    normal = vertex1.normal,
                    uv0 = new Vector2(vertex1.uv0.x - (uvx * ratio), vertex1.uv0.y - (uvy * ratio)),
                    color = Color.white
                };

                return splitVertex;
            }

            else
            {
                float sy = vertex1.position.y - stop;
                float dy = vertex1.position.y - vertex2.position.y;
                float dx = vertex1.position.x - vertex2.position.x;
                float uvx = vertex1.uv0.x - vertex2.uv0.x;
                float uvy = vertex1.uv0.y - vertex2.uv0.y;
                float ratio = sy / dy;
                float splitX = vertex1.position.x - (dx * ratio);

                UIVertex splitVertex = new()
                {
                    position = new Vector3(splitX, stop, vertex1.position.z),
                    normal = vertex1.normal,
                    uv0 = new Vector2(vertex1.uv0.x - (uvx * ratio), vertex1.uv0.y - (uvy * ratio)),
                    color = Color.white
                };
                return splitVertex;
            }
        }

        Color BlendColor(Color colorA, Color colorB)
        {
            return colorA * colorB;
        }
    }
}
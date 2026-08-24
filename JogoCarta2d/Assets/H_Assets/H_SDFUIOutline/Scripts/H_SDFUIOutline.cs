using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
[ExecuteAlways]
public class H_SDFUIOutline : MaskableGraphic
{
    public enum OutlineRenderMode
    {
        ProceduralSdf,
        LegacyMeshRing,
    }

    [SerializeField] Texture m_Texture;

    [Header("Rendering")]
    [SerializeField] OutlineRenderMode _renderMode = OutlineRenderMode.ProceduralSdf;
    [SerializeField, Range(0.5f, 3f)] float _edgeSoftness = 1f;

    [SerializeField] Shader _sdfShader;
    [SerializeField, Range(0f, 500f)] float _outlineWidth = 100f;
    [SerializeField, Range(0f, 500f)] float _cornerRadius = 50f;
    [SerializeField, Range(1, 20)] int _cornerSegments = 1;
    [SerializeField, Range(0f, 1f)] float _mappingBias = 0.5f;
    [SerializeField] bool _fillCenter;

    /// <summary>Outline technique to draw with. Assigning at runtime rebuilds the graphic so the change shows immediately.</summary>
    public OutlineRenderMode RenderMode
    {
        get => _renderMode;
        set
        {
            if (_renderMode == value) return;
            _renderMode = value;
            // A plain field write won't rebuild the CanvasRenderer; dirty it so the switch shows immediately.
            SetVerticesDirty();
            SetMaterialDirty();
        }
    }

    /// <summary>Outline thickness in UI units. Rebuilds the outline when changed.</summary>
    public float OutlineWidth
    {
        get => _outlineWidth;
        set
        {
            if (_outlineWidth == value) return;
            _outlineWidth = value;
            SetVerticesDirty();
            SetMaterialDirty();
        }
    }

    /// <summary>Corner rounding radius in UI units. Rebuilds the outline when changed.</summary>
    public float CornerRadius
    {
        get => _cornerRadius;
        set
        {
            if (_cornerRadius == value) return;
            _cornerRadius = value;
            SetVerticesDirty();
            SetMaterialDirty();
        }
    }

    /// <summary>Softness of the anti-aliased SDF edge. Lower is sharper. SDF mode only.</summary>
    public float EdgeSoftness
    {
        get => _edgeSoftness;
        set
        {
            if (_edgeSoftness == value) return;
            _edgeSoftness = value;
            SetMaterialDirty();
        }
    }

    /// <summary>Fill the area inside the outline instead of drawing only the ring.</summary>
    public bool FillCenter
    {
        get => _fillCenter;
        set
        {
            if (_fillCenter == value) return;
            _fillCenter = value;
            SetVerticesDirty();
            SetMaterialDirty();
        }
    }

    private Vector3[] _corners = new Vector3[4];
    private List<UIVertex> _verts = new List<UIVertex>();

    private Material _runtimeSdfMaterial;

    public override Texture mainTexture => m_Texture == null ? s_WhiteTexture : m_Texture;

    public override Material materialForRendering
    {
        get
        {
            if (_renderMode != OutlineRenderMode.ProceduralSdf)
            {
                return base.materialForRendering;
            }

            var mat = GetOrCreateSdfMaterial();
            ApplySdfProperties(mat);
            var modified = GetModifiedMaterial(mat);
            if (modified != null && modified != mat)
            {
                ApplySdfProperties(modified);
            }
            return modified;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_renderMode == OutlineRenderMode.ProceduralSdf)
        {
            GetOrCreateSdfMaterial();
            SetMaterialDirty();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // Keep runtime material alive across enables to avoid allocations during rebuild loop.
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_runtimeSdfMaterial != null)
        {
            if (Application.isPlaying) Destroy(_runtimeSdfMaterial);
            else DestroyImmediate(_runtimeSdfMaterial);
            _runtimeSdfMaterial = null;
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
        SetMaterialDirty();
    }
#endif

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
        SetMaterialDirty();
    }

    protected override void UpdateMaterial()
    {
        base.UpdateMaterial();

        if (_renderMode != OutlineRenderMode.ProceduralSdf)
        {
            return;
        }

        // In Edit Mode, Graphic rebuilds can be sporadic; ensure the material that ends up
        // on the CanvasRenderer has our latest per-rect properties.
        var mat = canvasRenderer.GetMaterial();
        ApplySdfProperties(mat);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_renderMode == OutlineRenderMode.ProceduralSdf)
        {
            PopulateQuad(vh);
            return;
        }

        // Clamp corner radius
        var rect = rectTransform.rect;
        var clampedCornerRadius = Mathf.Min((Mathf.Min(rect.width, rect.height) / 2f), _cornerRadius);

        // Offset corner based on clamped corner radius
        rectTransform.GetLocalCorners(_corners);
        _corners[0] += new Vector3(clampedCornerRadius, clampedCornerRadius, 0f);
        _corners[1] += new Vector3(clampedCornerRadius, -clampedCornerRadius, 0f);
        _corners[2] += new Vector3(-clampedCornerRadius, -clampedCornerRadius, 0f);
        _corners[3] += new Vector3(-clampedCornerRadius, clampedCornerRadius, 0f);

        // Calculate dimensions
        var height = _corners[1].y - _corners[0].y;
        var width = _corners[2].x - _corners[1].x;
        var edgeLengths = new[] { height, width, height, width };
        var circumference = 2f * Mathf.PI * Mathf.Lerp(clampedCornerRadius, clampedCornerRadius + _outlineWidth, _mappingBias);
        var around = height * 2f + width * 2f + circumference;
        var cornerLength = circumference / 4f;
        var segmentLength = cornerLength / _cornerSegments;

        var vert = new UIVertex { color = color };
        _verts.Clear();

        // Create corners
        var u = 0f;
        for (var c = 0; c < 4; c++)
        {
            // Create verts
            var origin = _corners[c];
            for (var i = 0; i < _cornerSegments + 1; i++)
            {
                var angle = (float)i / _cornerSegments * Mathf.PI / 2f + Mathf.PI * 0.5f - Mathf.PI * c * 1.5f;
                var direction = new Vector3(Mathf.Cos(-angle), Mathf.Sin(-angle), 0f);

                vert.position = origin + direction * clampedCornerRadius;
                vert.uv0 = new Vector2(u, 0f);
                _verts.Add(vert);

                vert.position = origin + direction * (clampedCornerRadius + _outlineWidth);
                vert.uv0 = new Vector2(u, 1f);
                _verts.Add(vert);

                if (_fillCenter)
                {
                    vert.position = rect.center;
                    vert.uv0 = new Vector2(u, 0f);
                    _verts.Add(vert);
                }

                if (i < _cornerSegments)
                    u += segmentLength / around;
                else
                    u += edgeLengths[c] / around;
            }
        }

        // Add end verts
        vert = _verts[0];
        vert.uv0 = new Vector2(1f, 0f);
        _verts.Add(vert);

        vert = _verts[1];
        vert.uv0 = new Vector2(1f, 1f);
        _verts.Add(vert);

        if (_fillCenter)
        {
            vert = _verts[2];
            vert.uv0 = new Vector2(1f, 1f);
            _verts.Add(vert);
        }

        // Add verts to VertexHelper
        foreach (var vertex in _verts)
            vh.AddVert(vertex);

        // Add triangles to VertexHelper 
        if (_fillCenter)
        {
            for (var v = 0; v < vh.currentVertCount - 3; v += 3)
            {
                vh.AddTriangle(v, v + 1, v + 4);
                vh.AddTriangle(v, v + 4, v + 3);

                vh.AddTriangle(v + 2, v, v + 3);
                vh.AddTriangle(v + 2, v + 3, v + 5);
            }
        }
        else
        {
            for (var v = 0; v < vh.currentVertCount - 2; v += 2)
            {
                vh.AddTriangle(v, v + 1, v + 3);
                vh.AddTriangle(v, v + 3, v + 2);
            }
        }
    }

    private void PopulateQuad(VertexHelper vh)
    {
        var rect = GetPixelAdjustedRect();
        // Our SDF outline is rendered outside the rect, so expand the quad to include that area.
        // Add a small extra padding (~2px) for AA to avoid edge clipping.
        var padUnits = 0f;
        if (_outlineWidth > 0f)
        {
            padUnits = _outlineWidth + 2f;
        }

        var vert = UIVertex.simpleVert;
        vert.color = color;

        vert.position = new Vector3(rect.xMin - padUnits, rect.yMin - padUnits, 0f);
        vert.uv0 = new Vector2(0f, 0f);
        vh.AddVert(vert);

        vert.position = new Vector3(rect.xMin - padUnits, rect.yMax + padUnits, 0f);
        vert.uv0 = new Vector2(0f, 1f);
        vh.AddVert(vert);

        vert.position = new Vector3(rect.xMax + padUnits, rect.yMax + padUnits, 0f);
        vert.uv0 = new Vector2(1f, 1f);
        vh.AddVert(vert);

        vert.position = new Vector3(rect.xMax + padUnits, rect.yMin - padUnits, 0f);
        vert.uv0 = new Vector2(1f, 0f);
        vh.AddVert(vert);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    private Material GetOrCreateSdfMaterial()
    {
        if (_runtimeSdfMaterial != null)
        {
            return _runtimeSdfMaterial;
        }

        if (_sdfShader == null)
        {
            _sdfShader = Shader.Find("H_Shaders/H_UIRoundedRectOutlineSDF");
        }

        if (_sdfShader == null)
        {
            // Shader missing (e.g., stripped); fall back to default UI material.
            return defaultMaterial;
        }

        _runtimeSdfMaterial = new Material(_sdfShader)
        {
            name = "UIOutline (Runtime SDF)",
            hideFlags = HideFlags.HideAndDontSave
        };

        return _runtimeSdfMaterial;
    }

    private void ApplySdfProperties(Material mat)
    {
        if (mat == null) return;

        var rect = GetPixelAdjustedRect();

        var sizeUnits = new Vector2(rect.width, rect.height);

        var padUnits = 0f;
        if (_outlineWidth > 0f)
        {
            padUnits = _outlineWidth + 2f;
        }

        mat.SetVector("_RectSizeUnits", sizeUnits);
        mat.SetFloat("_PadUnits", padUnits);
        // Despite the names, these are in local UI units (typically pixels in Screen Space).
        mat.SetFloat("_OutlineWidthPx", _outlineWidth);
        mat.SetFloat("_CornerRadiusPx", _cornerRadius);
        mat.SetFloat("_FillCenter", _fillCenter ? 1f : 0f);
        mat.SetFloat("_EdgeSoftness", _edgeSoftness);
    }
}
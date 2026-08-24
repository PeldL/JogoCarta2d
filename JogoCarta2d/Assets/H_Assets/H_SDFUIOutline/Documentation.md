# H SDF UI Outline (URP/HDRP)

H SDF UI Outline is a simple drop-in component that draws a clean outline around a Unity UI element. It has two render modes: a **traditional mesh ring**, and a **procedural SDF** (signed-distance-field) mode that stays crisp and never flickers or disappears, even at very small sizes.

It's a plain `MaskableGraphic`, so it lives on a UI GameObject under a Canvas just like an `Image`, and it works in both URP and HDRP (and the built-in pipeline) with no render feature or setup.

---

## Why SDF?

Most UI outlines are built from extra geometry — either a ring of triangles or vertices pushed outward. That looks fine when the outline is a few pixels thick, but it has two well-known problems:

- **Thin outlines flicker and disappear.** When the ring gets sub-pixel thin (small UI, high-DPI screens, or an animated scale), the rasteriser can miss it on some frames, so the edge shimmers or vanishes.
- **Corners alias.** A mesh ring approximates rounded corners with a fixed number of segments, so tight corners look faceted unless you crank the segment count.

The SDF mode sidesteps both. Instead of building geometry for the outline, it draws a single quad and evaluates the outline mathematically per pixel from a distance field. The edge is computed, not sampled, so it stays sharp and consistent at **any** resolution or scale — no flicker, no dropouts, smooth rounded corners for free.

Use the **mesh ring** mode when you want a classic vertex-based outline (or need to match existing UI). Use **SDF** mode when you want a crisp outline that holds up at small sizes and while scaling — which is most of the time.

---

## Quick start

1. Create one from **GameObject → UI → H SDF UI Outline** (it drops under your Canvas, making one if the scene has none), or add the **H SDF UI Outline** component to any existing UI RectTransform.
2. In the inspector, pick a **Render Mode** and set the **Outline Width** and **Corner Radius**.
3. Set the outline **Color** in the Appearance section.

That's it — the outline renders immediately in the Scene and Game views.

---

## Settings

**Outline**
- **Render Mode** — `Procedural Sdf` (crisp, no flicker) or `Legacy Mesh Ring` (vertex-based).
- **Outline Width** — thickness of the outline, in UI units.
- **Corner Radius** — rounds the corners of the outline.
- **Edge Softness** *(SDF only)* — how soft the anti-aliased edge is. Lower is sharper.
- **Corner Segments** *(mesh ring only)* — how many segments approximate each rounded corner. Higher is smoother, at the cost of more vertices.
- **Mapping Bias** *(mesh ring only)* — biases the outline's UV mapping between the inner and outer edge.
- **Fill Center** — also fill the area inside the outline, instead of drawing just the ring.

**Appearance**
- **Color** — outline colour (and fill colour when Fill Center is on).
- **Material** — leave empty to use the default; SDF mode supplies its own runtime material automatically.
- **Texture** — optional texture sampled by the graphic.
- **Raycast Target / Maskable** — standard Unity UI Graphic options.

---

## Scripting

Everything is drivable from code. The properties rebuild the outline for you when changed, so they're safe to animate:

```csharp
var outline = GetComponent<H_SDFUIOutline>();
outline.RenderMode = H_SDFUIOutline.OutlineRenderMode.ProceduralSdf;
outline.color = Color.cyan;   // standard Graphic colour
outline.OutlineWidth = 6f;    // pulse this for a hover/select effect
outline.CornerRadius = 12f;
outline.EdgeSoftness = 1f;    // SDF only
outline.FillCenter = false;
```

The included demo (`Scenes/H_SDFUIOutlineDemo`) has a small helper, `H_SDFUIOutlineDebugWindow`, that flips every outline in the scene between the two modes when you press **Tab** — handy for comparing SDF vs mesh ring side by side.

---

## Notes

- The SDF shader is `H_Shaders/H_UIRoundedRectOutlineSDF`. The component builds its material from it at runtime; an editor step automatically registers the shader in **Project Settings → Graphics → Always Included Shaders** so it's never stripped from a player build. If you ever remove it from that list, add it back (or keep a material that references it), otherwise SDF mode falls back to the default UI material in builds.
- The outline is drawn slightly outside the RectTransform, so the component pads its quad to avoid clipping the edge — no need to oversize the RectTransform.
- The demo scene and its `H_SDFUIOutlineDebugWindow` helper use TextMeshPro (a default Unity package). The runtime outline component itself has no TextMeshPro dependency.
- Free to use. If it's handy, a rating on the Asset Store is appreciated.
</content>

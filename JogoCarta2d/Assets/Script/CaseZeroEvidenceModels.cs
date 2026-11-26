using System.Collections.Generic;
using UnityEngine;

// 3D models for each clue, built from primitives in the style of the clue drawings: crimson
// ink shading (CaseZero/Evidence3D) and a thick dark outline (CaseZero/Evidence3DOutline).
// The colours below only set how light or dark each part is; the shader maps them onto the
// red palette. Replace any model by assigning a real one in ArtesCasoZero ▸ "Modelos 3D das pistas".
public static class CaseZeroEvidenceModels
{
    static Shader shader, outlineShader;
    static Material outline;
    static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    // Value scale, darkest to lightest.
    static readonly Color Ink = new Color(0.07f, 0.07f, 0.07f);
    static readonly Color Dark = new Color(0.2f, 0.2f, 0.2f);
    static readonly Color Deep = new Color(0.32f, 0.32f, 0.32f);
    static readonly Color Mid = new Color(0.5f, 0.5f, 0.5f);
    static readonly Color Body = new Color(0.62f, 0.62f, 0.62f);
    static readonly Color Light = new Color(0.78f, 0.78f, 0.78f);
    static readonly Color Pale = new Color(0.9f, 0.9f, 0.9f);
    static readonly Color White = new Color(1f, 1f, 1f);

    /// Builds the model for a clue key, or null when the clue has no physical object.
    public static GameObject Build(string key)
    {
        var root = new GameObject("Modelo 3D • " + key);
        var t = root.transform;
        switch (key)
        {
            // Case 1 • The Last Witness
            case "registro": Agenda(t, true); break;
            case "pistola": Pistol(t); break;
            case "capsula": CartridgeCase(t); break;
            case "sangue": CrimeScene(t); break;
            // Case 2 • The Second Opinion
            case "c2_rua_lixeira": Dumpster(t); break;
            case "c2_rua_cracha": KeyRing(t); break;
            case "c2_rua_marcas": WheelTracks(t); break;
            case "c2_lab_seringa": Syringe(t); break;
            case "c2_lab_estoque": DrugLog(t); break;
            case "c2_lab_camera": CameraPanel(t); break;
            case "c2_lab_maca": Trolley(t); break;
            case "c2_police_autopsia": Folder(t, Sheet.Autopsy); break;
            case "c2_police_rascunho": Phone(t); break;
            case "c2_police_digitais": FingerprintCard(t, false); break;
            case "c2_police_transferencia": Statement(t); break;
            // Case 3 • The Last Coffee
            case "c3_padaria_xicara": Mug(t); break;
            case "c3_padaria_camera": SecurityCamera(t); break;
            case "c3_padaria_sache": Sachet(t); break;
            case "c3_padaria_comanda": OrderSlip(t); break;
            case "c3_est_envelope": Envelope(t); break;
            case "c3_est_caderno": Notebook(t); break;
            case "c3_est_jornal": Newspaper(t); break;
            case "c3_police_tox": Folder(t, Sheet.Toxicology); break;
            case "c3_police_livro": Agenda(t, false); break;
            case "c3_police_digitais": FingerprintCard(t, true); break;
            case "c3_police_viatura": Folder(t, Sheet.Repair); break;
            case "depoimento": Object.Destroy(root); return null;
            default: EvidenceBag(t); break;
        }
        return root;
    }

    // ---------- Case 1 ----------

    static void Pistol(Transform t)
    {
        // Slide with the rear serrations, ejection port and sights.
        Box(t, new Vector3(0.12f, 0.34f, 0), Vector3.zero, new Vector3(2.0f, 0.36f, 0.32f), Deep);
        Box(t, new Vector3(1.08f, 0.31f, 0), Vector3.zero, new Vector3(0.18f, 0.28f, 0.3f), Deep); // Rounded nose
        Cyl(t, new Vector3(1.18f, 0.31f, 0), new Vector3(0, 0, 90), new Vector3(0.11f, 0.02f, 0.11f), Ink); // Muzzle
        Box(t, new Vector3(0.25f, 0.53f, 0), Vector3.zero, new Vector3(0.42f, 0.03f, 0.18f), Ink); // Ejection port
        Box(t, new Vector3(1.02f, 0.55f, 0), Vector3.zero, new Vector3(0.07f, 0.06f, 0.06f), Dark); // Front sight
        Box(t, new Vector3(-0.8f, 0.56f, 0), Vector3.zero, new Vector3(0.1f, 0.07f, 0.2f), Dark);   // Rear sight
        Box(t, new Vector3(0.12f, 0.525f, 0), Vector3.zero, new Vector3(1.96f, 0.012f, 0.2f), Body); // Bevelled top edge
        Cyl(t, new Vector3(0.25f, 0.5f, 0), new Vector3(0, 0, 90), new Vector3(0.14f, 0.2f, 0.14f), Body); // Barrel hood in the port
        Box(t, new Vector3(-0.87f, 0.34f, 0), Vector3.zero, new Vector3(0.03f, 0.3f, 0.26f), Dark); // Back plate
        Box(t, new Vector3(0.12f, 0.34f, -0.165f), Vector3.zero, new Vector3(1.0f, 0.03f, 0.01f), Dark); // Engraved line
        for (int i = 0; i < 7; i++)
            Box(t, new Vector3(-0.78f + i * 0.065f, 0.34f, 0), Vector3.zero, new Vector3(0.022f, 0.3f, 0.335f), Ink);
        // Frame, accessory rail and slide stop.
        Box(t, new Vector3(0.18f, 0.08f, 0), Vector3.zero, new Vector3(1.75f, 0.18f, 0.28f), Body);
        Box(t, new Vector3(0.75f, -0.03f, 0), Vector3.zero, new Vector3(0.55f, 0.06f, 0.26f), Body);
        for (int i = 0; i < 3; i++)
            Box(t, new Vector3(0.58f + i * 0.15f, -0.065f, 0), Vector3.zero, new Vector3(0.04f, 0.02f, 0.27f), Ink);
        Box(t, new Vector3(-0.08f, 0.2f, -0.15f), Vector3.zero, new Vector3(0.22f, 0.05f, 0.03f), Dark);
        Box(t, new Vector3(0.12f, 0.2f, -0.15f), Vector3.zero, new Vector3(0.06f, 0.05f, 0.03f), Dark); // Takedown lever
        // Trigger guard as a closed loop, and the trigger inside it.
        Box(t, new Vector3(0.12f, -0.33f, 0), Vector3.zero, new Vector3(0.48f, 0.05f, 0.14f), Body);
        Box(t, new Vector3(0.38f, -0.18f, 0), new Vector3(0, 0, -12), new Vector3(0.05f, 0.32f, 0.14f), Body);
        Box(t, new Vector3(-0.14f, -0.18f, 0), Vector3.zero, new Vector3(0.05f, 0.3f, 0.14f), Body);
        Box(t, new Vector3(0.1f, -0.13f, 0), new Vector3(0, 0, 14), new Vector3(0.05f, 0.18f, 0.07f), Light);
        Box(t, new Vector3(0.11f, -0.14f, -0.04f), new Vector3(0, 0, 14), new Vector3(0.02f, 0.1f, 0.01f), Ink); // Trigger safety
        for (int i = 0; i < 3; i++) // Finger grooves on the front strap
            Cap(t, Quaternion.Euler(0, 0, -17) * new Vector3(0.24f, -0.12f - i * 0.22f, 0) + new Vector3(-0.5f, -0.42f, 0), new Vector3(90, 0, 0), new Vector3(0.11f, 0.15f, 0.11f), Body);
        Box(t, new Vector3(-0.78f, -1.0f, 0), new Vector3(0, 0, -17), new Vector3(0.12f, 0.05f, 0.34f), Ink); // Floor plate notch
        // Grip with stippled panel and magazine base plate.
        Box(t, new Vector3(-0.5f, -0.42f, 0), new Vector3(0, 0, -17), new Vector3(0.5f, 1.0f, 0.3f), Body);
        Box(t, new Vector3(-0.66f, -0.97f, 0), new Vector3(0, 0, -17), new Vector3(0.58f, 0.1f, 0.33f), Dark);
        Box(t, new Vector3(-0.7f, 0.0f, 0), new Vector3(0, 0, -17), new Vector3(0.16f, 0.18f, 0.32f), Body); // Beavertail
        for (int row = 0; row < 6; row++)
            for (int col = 0; col < 3; col++)
            {
                var p = Quaternion.Euler(0, 0, -17) * new Vector3(-0.12f + col * 0.12f, 0.28f - row * 0.13f, 0);
                Box(t, new Vector3(-0.5f, -0.42f, -0.155f) + p, new Vector3(0, 0, -17), new Vector3(0.06f, 0.06f, 0.012f), Deep);
                Box(t, new Vector3(-0.5f, -0.42f, 0.155f) + p, new Vector3(0, 0, -17), new Vector3(0.06f, 0.06f, 0.012f), Deep);
            }
    }

    static void CartridgeCase(Transform t)
    {
        // Fired case: open mouth, extractor groove, rim and a dented primer.
        Cyl(t, Vector3.zero, Vector3.zero, new Vector3(0.46f, 0.62f, 0.46f), Light);
        Cyl(t, new Vector3(0, 0.6f, 0), Vector3.zero, new Vector3(0.44f, 0.04f, 0.44f), Body);
        Cyl(t, new Vector3(0, 0.63f, 0), Vector3.zero, new Vector3(0.34f, 0.02f, 0.34f), Ink); // Mouth
        Cyl(t, new Vector3(0, -0.66f, 0), Vector3.zero, new Vector3(0.38f, 0.05f, 0.38f), Deep); // Groove
        Cyl(t, new Vector3(0, -0.76f, 0), Vector3.zero, new Vector3(0.48f, 0.06f, 0.48f), Light); // Rim
        Cyl(t, new Vector3(0, -0.83f, 0), Vector3.zero, new Vector3(0.16f, 0.015f, 0.16f), Mid); // Primer
        Sph(t, new Vector3(0, -0.84f, 0), Vector3.zero, new Vector3(0.05f, 0.02f, 0.05f), Ink); // Firing pin dent
        Box(t, new Vector3(0.2f, 0.15f, -0.13f), new Vector3(0, -30, 4), new Vector3(0.012f, 0.55f, 0.02f), Body); // Ejector scratch
        Cyl(t, new Vector3(0, 0.42f, 0), Vector3.zero, new Vector3(0.47f, 0.015f, 0.47f), Body);   // Crimp line
        Cyl(t, new Vector3(0, -0.5f, 0), Vector3.zero, new Vector3(0.47f, 0.012f, 0.47f), Body);   // Head stamp edge
        t.localRotation = Quaternion.Euler(0, 0, 65);
    }

    static void CrimeScene(Transform t)
    {
        // Corner of the counter floor: tiles, the untouched drink and the blood.
        for (int x = 0; x < 4; x++)
            for (int z = 0; z < 3; z++)
                Box(t, new Vector3(-0.9f + x * 0.6f, -0.1f, -0.6f + z * 0.6f), Vector3.zero, new Vector3(0.58f, 0.1f, 0.58f), (x + z) % 2 == 0 ? Deep : Mid);
        var random = new System.Random(7);
        Sph(t, new Vector3(0.1f, -0.04f, 0.05f), Vector3.zero, new Vector3(1.05f, 0.03f, 0.75f), Ink);
        Sph(t, new Vector3(0.0f, -0.035f, 0.0f), Vector3.zero, new Vector3(0.45f, 0.03f, 0.3f), Dark); // Wet sheen
        for (int i = 0; i < 10; i++)
        {
            float s = 0.08f + (float)random.NextDouble() * 0.28f;
            Sph(t, new Vector3((float)random.NextDouble() * 2f - 1f, -0.04f, (float)random.NextDouble() * 1.4f - 0.7f), Vector3.zero, new Vector3(s, 0.03f, s * 0.8f), Ink);
        }
        // The drink she never touched: glass, liquid, ice and a slice of lime.
        var glass = Group(t, new Vector3(-0.75f, 0.32f, -0.3f));
        Cyl(glass, Vector3.zero, Vector3.zero, new Vector3(0.42f, 0.36f, 0.42f), Pale);
        Cyl(glass, new Vector3(0, 0.08f, 0), Vector3.zero, new Vector3(0.38f, 0.26f, 0.38f), Mid);
        Box(glass, new Vector3(0.05f, 0.32f, 0), new Vector3(10, 20, 5), Vector3.one * 0.13f, White);
        Box(glass, new Vector3(-0.08f, 0.3f, 0.06f), new Vector3(-12, 40, 0), Vector3.one * 0.11f, White);
        Cyl(glass, new Vector3(0.18f, 0.36f, 0), new Vector3(0, 0, 70), new Vector3(0.2f, 0.02f, 0.2f), Light);
        Cyl(glass, new Vector3(-0.05f, 0.48f, 0), new Vector3(0, 0, 12), new Vector3(0.03f, 0.3f, 0.03f), Body); // Straw
        // Broken lock plate from the storeroom door.
        var lockPlate = Group(t, new Vector3(0.75f, 0.0f, -0.4f));
        Box(lockPlate, Vector3.zero, new Vector3(0, 25, 0), new Vector3(0.45f, 0.04f, 0.22f), Light);
        Cyl(lockPlate, new Vector3(0.08f, 0.04f, 0), Vector3.zero, new Vector3(0.12f, 0.02f, 0.12f), Ink);
        Box(lockPlate, new Vector3(-0.2f, 0.05f, 0.1f), new Vector3(0, -30, 20), new Vector3(0.18f, 0.03f, 0.05f), Body); // Bent bolt
        t.localRotation = Quaternion.Euler(-32, 0, 0);
    }

    static void Agenda(Transform t, bool reminders)
    {
        // Open diary, as in the drawing: two page blocks in a shallow V, header, lines and a ribbon.
        foreach (int side in new[] { -1, 1 })
        {
            var half = Group(t, new Vector3(side * 0.66f, 0, 0), new Vector3(0, 0, side * -6));
            Box(half, new Vector3(side * 0.02f, -0.07f, 0), Vector3.zero, new Vector3(1.32f, 0.05f, 1.7f), Deep); // Cover
            Box(half, new Vector3(0, 0, 0), Vector3.zero, new Vector3(1.24f, 0.12f, 1.6f), Pale);              // Pages
            Box(half, new Vector3(side * 0.63f, -0.01f, 0), Vector3.zero, new Vector3(0.015f, 0.11f, 1.58f), Light); // Page edges
            for (int i = 0; i < 2; i++)
                Box(half, new Vector3(side * 0.55f, 0.065f, (i == 0 ? 1 : -1) * 0.72f), new Vector3(0, 45, 0), new Vector3(0.18f, 0.01f, 0.18f), Deep); // Corner guards
            Box(half, new Vector3(-0.25f, 0.065f, 0.62f), Vector3.zero, new Vector3(0.45f, 0.006f, 0.08f), Dark); // Header
            for (int i = 0; i < 7; i++)
            {
                float z = 0.42f - i * 0.17f;
                Box(half, new Vector3(0.02f, 0.064f, z), Vector3.zero, new Vector3(1.0f, 0.004f, 0.02f), Mid);
                if (reminders && side > 0 && i < 5)
                {
                    Box(half, new Vector3(-0.42f, 0.068f, z + 0.06f), Vector3.zero, new Vector3(0.09f, 0.006f, 0.09f), Dark);
                    Box(half, new Vector3(-0.42f, 0.071f, z + 0.06f), Vector3.zero, new Vector3(0.06f, 0.006f, 0.06f), Pale);
                }
                else if (side < 0 && i % 2 == 0)
                    Box(half, new Vector3(-0.1f + (i % 3) * 0.08f, 0.068f, z + 0.06f), Vector3.zero, new Vector3(0.5f - (i % 3) * 0.1f, 0.005f, 0.035f), Ink); // Handwriting
            }
            Box(half, new Vector3(side * 0.69f, -0.03f, 0.3f), Vector3.zero, new Vector3(0.08f, 0.06f, 0.22f), Body); // Tabs
            Box(half, new Vector3(side * 0.69f, -0.03f, -0.15f), Vector3.zero, new Vector3(0.08f, 0.06f, 0.22f), Body);
        }
        Cyl(t, new Vector3(0, -0.05f, 0), new Vector3(90, 0, 0), new Vector3(0.12f, 0.85f, 0.12f), Deep); // Spine
        Box(t, new Vector3(0.05f, 0.02f, -0.95f), new Vector3(0, 0, 0), new Vector3(0.1f, 0.012f, 0.4f), Body); // Ribbon
        Box(t, new Vector3(0.05f, 0.02f, -1.17f), new Vector3(0, 45, 0), new Vector3(0.08f, 0.012f, 0.08f), Body);
        t.localRotation = Quaternion.Euler(-38, 0, 0);
    }

    // ---------- Case 2 ----------

    static void Dumpster(Transform t)
    {
        // Wheeled bin like the drawing: ribbed body, rim, one lid thrown open and the coat inside.
        Box(t, Vector3.zero, Vector3.zero, new Vector3(2.1f, 1.15f, 1.15f), Body);
        Box(t, new Vector3(0, 0.6f, 0), Vector3.zero, new Vector3(2.2f, 0.08f, 1.22f), Light); // Rim
        Box(t, new Vector3(0, 0.6f, 0), Vector3.zero, new Vector3(2.0f, 0.1f, 1.0f), Ink);     // Opening
        foreach (int side in new[] { -1, 1 })
        {
            Box(t, new Vector3(side * 1.1f, 0.2f, 0), Vector3.zero, new Vector3(0.12f, 0.14f, 0.9f), Deep); // Side lifting bars
            Box(t, new Vector3(side * 1.1f, -0.25f, 0), Vector3.zero, new Vector3(0.12f, 0.14f, 0.9f), Deep);
        }
        for (int i = 0; i < 4; i++) // Front ribs
            Box(t, new Vector3(-0.75f + i * 0.5f, -0.05f, -0.585f), Vector3.zero, new Vector3(0.08f, 1.0f, 0.03f), Mid);
        Box(t, new Vector3(-0.5f, 0.38f, -0.6f), Vector3.zero, new Vector3(0.3f, 0.06f, 0.03f), Dark); // Handles
        Box(t, new Vector3(0.5f, 0.38f, -0.6f), Vector3.zero, new Vector3(0.3f, 0.06f, 0.03f), Dark);
        // Closed lid on the left, open lid leaning back on the right.
        Box(t, new Vector3(-0.53f, 0.68f, 0), new Vector3(8, 0, 0), new Vector3(1.04f, 0.06f, 1.2f), Body);
        Box(t, new Vector3(0.53f, 1.08f, 0.7f), new Vector3(-62, 0, 0), new Vector3(1.04f, 0.06f, 1.2f), Body);
        Box(t, new Vector3(0.53f, 1.06f, 0.66f), new Vector3(-62, 0, 0), new Vector3(0.9f, 0.065f, 1.0f), Deep);
        // Rubbish bags and the body in its lab coat, an arm hanging over the rim.
        Sph(t, new Vector3(-0.6f, 0.55f, 0.2f), Vector3.zero, new Vector3(0.6f, 0.45f, 0.55f), Dark);
        Sph(t, new Vector3(-0.15f, 0.55f, 0.3f), Vector3.zero, new Vector3(0.45f, 0.4f, 0.45f), Ink);
        Cap(t, new Vector3(0.35f, 0.62f, 0.05f), new Vector3(0, 0, 90), new Vector3(0.48f, 0.55f, 0.42f), White); // Torso in the coat
        Box(t, new Vector3(0.35f, 0.75f, -0.1f), Vector3.zero, new Vector3(0.9f, 0.03f, 0.05f), Mid);          // Coat seam
        Sph(t, new Vector3(0.82f, 0.72f, 0.05f), Vector3.zero, new Vector3(0.3f, 0.28f, 0.3f), Light);          // Head
        Cap(t, new Vector3(0.05f, 0.62f, -0.55f), new Vector3(70, 0, 0), new Vector3(0.14f, 0.24f, 0.14f), White); // Sleeve over the rim
        Sph(t, new Vector3(0.05f, 0.42f, -0.66f), Vector3.zero, new Vector3(0.13f, 0.1f, 0.12f), Light);        // Hand
        foreach (float x in new[] { -0.2f, 0.2f, 0.85f })
            Cyl(t, new Vector3(x, 0.64f, 0.6f), new Vector3(0, 0, 90), new Vector3(0.07f, 0.08f, 0.07f), Dark); // Hinges
        Sph(t, new Vector3(-0.7f, -0.35f, -0.58f), Vector3.zero, new Vector3(0.25f, 0.18f, 0.02f), Deep); // Rust stains
        Sph(t, new Vector3(0.6f, 0.15f, -0.58f), Vector3.zero, new Vector3(0.15f, 0.25f, 0.02f), Deep);
        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -0.85f : 0.85f, z = i % 2 == 0 ? -0.42f : 0.42f;
            Box(t, new Vector3(x, -0.64f, z), Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), Dark);
            Cyl(t, new Vector3(x, -0.78f, z), new Vector3(90, 0, 0), new Vector3(0.22f, 0.05f, 0.22f), Ink);
            Cyl(t, new Vector3(x, -0.78f, z), new Vector3(90, 0, 0), new Vector3(0.08f, 0.06f, 0.08f), Mid);
        }
        t.localRotation = Quaternion.Euler(-12, 22, 0);
    }

    static void KeyRing(Transform t)
    {
        // As in the drawing: big ring, two small links, an old skeleton key and two flat keys.
        Ring(t, new Vector3(0.25f, 0.75f, 0), Vector3.zero, 0.45f, 0.07f, Body);
        Ring(t, new Vector3(-0.1f, 0.25f, 0.02f), new Vector3(0, 40, 0), 0.18f, 0.045f, Body);
        Ring(t, new Vector3(0.12f, 0.2f, -0.02f), new Vector3(0, -40, 0), 0.16f, 0.045f, Body);
        // Skeleton key.
        var old = Group(t, new Vector3(-0.3f, 0.02f, 0.03f), new Vector3(0, 0, -28));
        Box(old, new Vector3(0, -0.12f, 0), new Vector3(0, 0, 45), new Vector3(0.36f, 0.36f, 0.05f), Body);
        Box(old, new Vector3(0, -0.12f, 0), new Vector3(0, 0, 45), new Vector3(0.24f, 0.24f, 0.055f), Mid);
        Box(old, new Vector3(0, -0.72f, 0), Vector3.zero, new Vector3(0.1f, 0.95f, 0.06f), Body);
        Box(old, new Vector3(-0.11f, -1.1f, 0), Vector3.zero, new Vector3(0.14f, 0.1f, 0.06f), Body); // Bit
        Box(old, new Vector3(-0.11f, -0.96f, 0), Vector3.zero, new Vector3(0.14f, 0.07f, 0.06f), Body);
        Box(old, new Vector3(0, -0.32f, 0), Vector3.zero, new Vector3(0.16f, 0.05f, 0.07f), Deep);   // Collar
        // Two flat cylinder keys with notched blades.
        for (int k = 0; k < 2; k++)
        {
            var key = Group(t, new Vector3(0.05f + k * 0.3f, 0.0f, -0.04f - k * 0.04f), new Vector3(0, 0, 4 + k * 18));
            Box(key, new Vector3(0, -0.2f, 0), Vector3.zero, new Vector3(0.38f, 0.34f, 0.06f), k == 0 ? Light : Body);
            Box(key, new Vector3(0, -0.2f, -0.035f), Vector3.zero, new Vector3(0.28f, 0.24f, 0.01f), k == 0 ? Body : Mid); // Head bevel
            Cyl(key, new Vector3(0, -0.1f, -0.04f), new Vector3(90, 0, 0), new Vector3(0.08f, 0.01f, 0.08f), Ink);          // Hole
            Box(key, new Vector3(0, -0.84f, 0), Vector3.zero, new Vector3(0.15f, 0.95f, 0.045f), k == 0 ? Light : Body);
            Box(key, new Vector3(0.02f, -0.84f, -0.025f), Vector3.zero, new Vector3(0.025f, 0.85f, 0.01f), Deep);           // Groove
            for (int n = 0; n < 5; n++)
                Box(key, new Vector3(0.09f, -0.5f - n * 0.15f, 0), new Vector3(0, 0, 45), new Vector3(0.07f, 0.07f, 0.045f), k == 0 ? Light : Body);
            Box(key, new Vector3(0, -1.32f, 0), new Vector3(0, 0, 45), new Vector3(0.1f, 0.1f, 0.045f), k == 0 ? Light : Body); // Tip
        }
        // The laboratory access tag.
        var tag = Group(t, new Vector3(0.72f, 0.05f, 0.05f), new Vector3(0, 0, -20));
        Box(tag, new Vector3(0, -0.2f, 0), Vector3.zero, new Vector3(0.32f, 0.45f, 0.06f), Deep);
        Box(tag, new Vector3(0, -0.25f, -0.035f), Vector3.zero, new Vector3(0.2f, 0.12f, 0.01f), Pale);
        Ring(tag, new Vector3(0, 0.05f, 0), new Vector3(90, 0, 0), 0.06f, 0.025f, Body);
    }

    static void WheelTracks(Transform t)
    {
        // Pavement slab with curb, two thin muddy tracks and a puddle they cross.
        Box(t, new Vector3(0, -0.1f, 0), Vector3.zero, new Vector3(2.4f, 0.12f, 1.7f), Mid);
        Box(t, new Vector3(0, 0.0f, 0.95f), Vector3.zero, new Vector3(2.4f, 0.3f, 0.25f), Light); // Curb
        var random = new System.Random(3);
        for (int i = 0; i < 18; i++) // Grit
            Box(t, new Vector3((float)random.NextDouble() * 2.2f - 1.1f, -0.035f, (float)random.NextDouble() * 1.5f - 0.75f),
                new Vector3(0, (float)random.NextDouble() * 90, 0), new Vector3(0.05f, 0.02f, 0.04f), Deep);
        Sph(t, new Vector3(-0.35f, -0.04f, -0.1f), Vector3.zero, new Vector3(0.7f, 0.02f, 0.5f), Dark);
        foreach (float z in new[] { -0.32f, 0.32f })
            for (int i = 0; i < 9; i++)
            {
                float x = -1.05f + i * 0.26f;
                Box(t, new Vector3(x, -0.03f, z + Mathf.Sin(i * 0.7f) * 0.03f), new Vector3(0, Mathf.Cos(i * 0.7f) * 6, 0), new Vector3(0.24f, 0.02f, 0.06f), Ink);
                if (i % 3 == 0) Sph(t, new Vector3(x + 0.06f, -0.02f, z + 0.07f), Vector3.zero, new Vector3(0.07f, 0.03f, 0.05f), Dark); // Mud clumps
            }
        t.localRotation = Quaternion.Euler(-35, 0, 0);
    }

    static void Syringe(Transform t)
    {
        // Barrel with graduations, liquid and stopper, flange, cross-shaped plunger and needle.
        Cyl(t, Vector3.zero, new Vector3(0, 0, 90), new Vector3(0.32f, 0.62f, 0.32f), Pale);
        Cyl(t, new Vector3(0.2f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.27f, 0.4f, 0.27f), Deep); // Liquid
        Cyl(t, new Vector3(-0.22f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.28f, 0.05f, 0.28f), Ink); // Rubber stopper
        for (int i = 0; i < 7; i++)
            Box(t, new Vector3(-0.4f + i * 0.15f, 0.12f, -0.12f), new Vector3(45, 0, 0), new Vector3(0.012f, i % 2 == 0 ? 0.12f : 0.07f, 0.02f), Ink);
        Cyl(t, new Vector3(-0.64f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.62f, 0.025f, 0.62f), Light); // Flange
        Box(t, new Vector3(-0.92f, 0, 0), Vector3.zero, new Vector3(0.6f, 0.12f, 0.025f), Body);           // Plunger rod
        Box(t, new Vector3(-0.92f, 0, 0), Vector3.zero, new Vector3(0.6f, 0.025f, 0.12f), Body);
        Cyl(t, new Vector3(-1.24f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.42f, 0.03f, 0.42f), Body); // Thumb press
        Cyl(t, new Vector3(0.68f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.14f, 0.07f, 0.14f), Light); // Tip
        Cyl(t, new Vector3(0.8f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.1f, 0.06f, 0.1f), Mid);    // Hub
        Cyl(t, new Vector3(1.22f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.018f, 0.38f, 0.018f), White); // Needle
        Box(t, new Vector3(0.05f, 0.0f, -0.165f), Vector3.zero, new Vector3(0.35f, 0.18f, 0.01f), Dark); // Torn label stub
        t.localRotation = Quaternion.Euler(0, 0, 32);
    }

    static void DrugLog(Transform t)
    {
        // Stock ledger with the signed check, beside a vial rack with two empty slots.
        var book = Group(t, new Vector3(-0.35f, 0, 0.1f), new Vector3(0, -8, 0));
        Box(book, new Vector3(0, -0.06f, 0), Vector3.zero, new Vector3(1.5f, 0.05f, 1.15f), Deep);
        Box(book, Vector3.zero, Vector3.zero, new Vector3(1.44f, 0.08f, 1.08f), Pale);
        for (int r = 0; r < 6; r++)
        {
            Box(book, new Vector3(0, 0.045f, 0.4f - r * 0.15f), Vector3.zero, new Vector3(1.34f, 0.004f, 0.012f), Mid);
            for (int c = 0; c < 3; c++)
                Box(book, new Vector3(-0.45f + c * 0.42f, 0.048f, 0.34f - r * 0.15f), Vector3.zero, new Vector3(0.22f - (r % 2) * 0.05f, 0.005f, 0.03f), Ink);
        }
        foreach (float x in new[] { -0.2f, 0.25f })
            Box(book, new Vector3(x, 0.046f, 0), Vector3.zero, new Vector3(0.012f, 0.004f, 0.95f), Mid);
        Box(book, new Vector3(0.4f, 0.05f, -0.42f), new Vector3(0, 12, 0), new Vector3(0.4f, 0.006f, 0.05f), Ink); // Signature
        Box(book, new Vector3(0.4f, 0.05f, 0.25f), Vector3.zero, new Vector3(0.36f, 0.006f, 0.1f), Dark); // Highlighted row
        var rack = Group(t, new Vector3(0.95f, 0.15f, -0.1f), new Vector3(0, 15, 0));
        Box(rack, Vector3.zero, Vector3.zero, new Vector3(0.5f, 0.3f, 1.05f), Body);
        for (int i = 0; i < 5; i++)
        {
            var slot = new Vector3(0, 0.155f, -0.4f + i * 0.2f);
            Cyl(rack, slot, Vector3.zero, new Vector3(0.14f, 0.01f, 0.14f), Ink);
            if (i == 1 || i == 3) continue; // The two missing vials.
            Cyl(rack, slot + new Vector3(0, 0.22f, 0), Vector3.zero, new Vector3(0.13f, 0.22f, 0.13f), Pale);
            Cyl(rack, slot + new Vector3(0, 0.14f, 0), Vector3.zero, new Vector3(0.11f, 0.13f, 0.11f), Mid);
            Cyl(rack, slot + new Vector3(0, 0.46f, 0), Vector3.zero, new Vector3(0.14f, 0.04f, 0.14f), Deep);
        }
        t.localRotation = Quaternion.Euler(-30, 0, 0);
    }

    static void CameraPanel(Transform t)
    {
        // Old monitor with four camera feeds, two dead, an OFF warning, and the keyboard.
        Box(t, new Vector3(0, 0.45f, 0.1f), Vector3.zero, new Vector3(1.75f, 1.25f, 0.6f), Body);
        Box(t, new Vector3(0, 0.45f, -0.21f), Vector3.zero, new Vector3(1.55f, 1.05f, 0.02f), Ink);
        for (int i = 0; i < 4; i++)
        {
            var feed = new Vector3(-0.38f + (i % 2) * 0.76f, 0.7f - (i / 2) * 0.48f, -0.225f);
            bool dead = i == 1 || i == 2;
            Box(t, feed, Vector3.zero, new Vector3(0.68f, 0.4f, 0.01f), dead ? Dark : Mid);
            if (dead)
            {
                Box(t, feed + new Vector3(0, 0, -0.01f), new Vector3(0, 0, 30), new Vector3(0.55f, 0.03f, 0.005f), Light);
                Box(t, feed + new Vector3(0, 0, -0.01f), new Vector3(0, 0, -30), new Vector3(0.55f, 0.03f, 0.005f), Light);
            }
            else
                Box(t, feed + new Vector3(-0.1f, -0.06f, -0.01f), Vector3.zero, new Vector3(0.25f, 0.18f, 0.005f), Deep); // Corridor shape
        }
        Box(t, new Vector3(0.5f, 0.0f, -0.23f), Vector3.zero, new Vector3(0.5f, 0.1f, 0.01f), White); // "OFF 22:30"
        Box(t, new Vector3(0.5f, 0.0f, -0.24f), Vector3.zero, new Vector3(0.36f, 0.035f, 0.005f), Ink);
        Sph(t, new Vector3(0.75f, -0.08f, -0.21f), Vector3.zero, Vector3.one * 0.06f, Pale); // Power light
        Box(t, new Vector3(0, -0.2f, 0.1f), Vector3.zero, new Vector3(0.2f, 0.12f, 0.2f), Deep);
        Box(t, new Vector3(0, -0.27f, 0.1f), Vector3.zero, new Vector3(0.8f, 0.04f, 0.55f), Deep);
        var board = Group(t, new Vector3(0, -0.28f, -0.6f), new Vector3(-6, 0, 0));
        Box(board, Vector3.zero, Vector3.zero, new Vector3(1.5f, 0.06f, 0.45f), Deep);
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 12; c++)
                Box(board, new Vector3(-0.66f + c * 0.12f, 0.05f, 0.15f - r * 0.1f), Vector3.zero, new Vector3(0.09f, 0.04f, 0.075f), Light);
    }

    static void Trolley(Transform t)
    {
        // Autopsy trolley: steel bed with rails, folded sheet, frame, casters with street mud.
        Box(t, new Vector3(0, 0.45f, 0), Vector3.zero, new Vector3(2.3f, 0.08f, 0.85f), Light);
        foreach (int side in new[] { -1, 1 })
            Box(t, new Vector3(0, 0.53f, side * 0.42f), Vector3.zero, new Vector3(2.3f, 0.08f, 0.04f), Body); // Raised rails
        Box(t, new Vector3(1.2f, 0.72f, 0), Vector3.zero, new Vector3(0.05f, 0.05f, 0.8f), Body);        // Push handle
        foreach (int side in new[] { -1, 1 })
            Box(t, new Vector3(1.2f, 0.6f, side * 0.38f), Vector3.zero, new Vector3(0.05f, 0.25f, 0.05f), Body);
        Box(t, new Vector3(-0.35f, 0.55f, 0), new Vector3(0, 0, 2), new Vector3(1.2f, 0.08f, 0.72f), White); // Folded sheet
        Box(t, new Vector3(-0.35f, 0.6f, 0), Vector3.zero, new Vector3(1.0f, 0.02f, 0.02f), Mid);
        Box(t, new Vector3(0, -0.05f, 0), Vector3.zero, new Vector3(2.0f, 0.05f, 0.7f), Mid);          // Lower shelf
        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -1.0f : 1.0f, z = i % 2 == 0 ? -0.35f : 0.35f;
            Cyl(t, new Vector3(x, 0.05f, z), Vector3.zero, new Vector3(0.06f, 0.4f, 0.06f), Light);
            Box(t, new Vector3(x, -0.37f, z), Vector3.zero, new Vector3(0.08f, 0.08f, 0.08f), Dark);
            Cyl(t, new Vector3(x, -0.47f, z), new Vector3(90, 0, 0), new Vector3(0.16f, 0.04f, 0.16f), Ink); // Thin wheels
            Sph(t, new Vector3(x + 0.04f, -0.52f, z - 0.03f), Vector3.zero, new Vector3(0.1f, 0.06f, 0.07f), Deep); // Mud
        }
        t.localRotation = Quaternion.Euler(-10, 28, 0);
    }

    static void Phone(Transform t)
    {
        // The victim's phone with the unsent complaint on screen.
        Box(t, Vector3.zero, Vector3.zero, new Vector3(0.9f, 1.75f, 0.1f), Dark);
        Box(t, new Vector3(0, 0.02f, -0.055f), Vector3.zero, new Vector3(0.8f, 1.55f, 0.01f), Pale);
        Box(t, new Vector3(0, 0.66f, -0.062f), Vector3.zero, new Vector3(0.8f, 0.18f, 0.005f), Deep); // App bar
        Box(t, new Vector3(-0.18f, 0.66f, -0.066f), Vector3.zero, new Vector3(0.35f, 0.05f, 0.004f), Pale);
        for (int i = 0; i < 9; i++)
            Box(t, new Vector3(-0.03f + (i % 3) * 0.03f, 0.42f - i * 0.11f, -0.062f), Vector3.zero, new Vector3(0.65f - (i % 3) * 0.08f, 0.03f, 0.004f), Ink);
        Box(t, new Vector3(0.18f, -0.62f, -0.062f), Vector3.zero, new Vector3(0.32f, 0.12f, 0.005f), Deep); // Unsent "Send" button
        Cyl(t, new Vector3(0, 0.82f, -0.06f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.005f, 0.05f), Ink); // Camera
        Box(t, new Vector3(0.12f, 0.1f, -0.07f), new Vector3(0, 0, 50), new Vector3(0.9f, 0.012f, 0.004f), Mid); // Crack
        Box(t, new Vector3(-0.08f, 0.32f, -0.07f), new Vector3(0, 0, -25), new Vector3(0.5f, 0.012f, 0.004f), Mid);
        t.localRotation = Quaternion.Euler(-12, 0, -8);
    }

    static void FingerprintCard(Transform t, bool sachet)
    {
        // Fingerprint card: ten boxes, one print lifted and circled, and the matching evidence photo.
        Box(t, Vector3.zero, Vector3.zero, new Vector3(1.9f, 0.02f, 1.3f), Pale);
        Box(t, new Vector3(0, 0.012f, 0.52f), Vector3.zero, new Vector3(1.8f, 0.006f, 0.16f), Deep);
        for (int i = 0; i < 10; i++)
        {
            var cell = new Vector3(-0.72f + (i % 5) * 0.36f, 0.012f, 0.22f - (i / 5) * 0.42f);
            Box(t, cell, Vector3.zero, new Vector3(0.33f, 0.004f, 0.38f), Light);
            Print(t, cell + new Vector3(0, 0.004f, 0), i == 2 ? 0.13f : 0.1f, i == 2 ? Ink : Mid);
        }
        Ring(t, new Vector3(0.0f, 0.02f, 0.22f), new Vector3(90, 0, 0), 0.22f, 0.02f, Ink); // Circled match
        var photo = Group(t, new Vector3(0.85f, 0.06f, -0.55f), new Vector3(0, -18, 0));
        Box(photo, Vector3.zero, Vector3.zero, new Vector3(0.6f, 0.02f, 0.5f), White);
        Box(photo, new Vector3(0, 0.012f, 0.03f), Vector3.zero, new Vector3(0.5f, 0.005f, 0.36f), Deep);
        if (sachet) Box(photo, new Vector3(0, 0.02f, 0.03f), new Vector3(0, 20, 0), new Vector3(0.18f, 0.01f, 0.26f), Light);
        else Box(photo, new Vector3(0, 0.02f, 0.03f), new Vector3(0, 35, 0), new Vector3(0.4f, 0.01f, 0.06f), Light); // Syringe
        t.localRotation = Quaternion.Euler(-48, 0, 0);
    }

    static void Statement(Transform t)
    {
        // Bank statement with the payment highlighted, and two banknotes on top.
        Box(t, Vector3.zero, new Vector3(0, 4, 0), new Vector3(1.3f, 0.02f, 1.8f), Pale);
        Box(t, new Vector3(0, 0.012f, 0.72f), new Vector3(0, 4, 0), new Vector3(1.2f, 0.006f, 0.2f), Deep); // Bank header
        for (int i = 0; i < 9; i++)
        {
            var row = Quaternion.Euler(0, 4, 0) * new Vector3(0, 0.012f, 0.45f - i * 0.14f);
            if (i == 4) Box(t, row, new Vector3(0, 4, 0), new Vector3(1.15f, 0.006f, 0.1f), Light);
            Box(t, row + Quaternion.Euler(0, 4, 0) * new Vector3(-0.25f, 0.002f, 0), new Vector3(0, 4, 0), new Vector3(0.55f, 0.005f, 0.03f), Ink);
            Box(t, row + Quaternion.Euler(0, 4, 0) * new Vector3(0.4f, 0.002f, 0), new Vector3(0, 4, 0), new Vector3(0.25f, 0.005f, 0.03f), i == 4 ? Ink : Mid);
        }
        Ring(t, Quaternion.Euler(0, 4, 0) * new Vector3(0.4f, 0.02f, -0.11f), new Vector3(90, 0, 0), 0.2f, 0.018f, Ink);
        for (int n = 0; n < 2; n++)
        {
            var note = Group(t, new Vector3(-0.35f + n * 0.12f, 0.04f + n * 0.015f, -0.55f), new Vector3(0, -20 + n * 14, 0));
            Box(note, Vector3.zero, Vector3.zero, new Vector3(0.9f, 0.012f, 0.45f), Body);
            Box(note, new Vector3(0, 0.008f, 0), Vector3.zero, new Vector3(0.78f, 0.004f, 0.35f), Mid);
            Cyl(note, new Vector3(-0.2f, 0.01f, 0), Vector3.zero, new Vector3(0.2f, 0.004f, 0.2f), Light);
        }
        t.localRotation = Quaternion.Euler(-45, 0, 0);
    }

    // ---------- Case 3 ----------

    static void Mug(Transform t)
    {
        // Mug like the drawing: thick walls, coffee inside, drips down the side, loop handle.
        Cyl(t, Vector3.zero, Vector3.zero, new Vector3(1.05f, 0.62f, 1.05f), Body);
        Cyl(t, new Vector3(0, 0.6f, 0), Vector3.zero, new Vector3(0.9f, 0.03f, 0.9f), Ink);      // Inside
        Cyl(t, new Vector3(0, 0.5f, 0), Vector3.zero, new Vector3(0.88f, 0.02f, 0.88f), Dark);   // Coffee surface
        Sph(t, new Vector3(-0.15f, 0.52f, 0.12f), Vector3.zero, new Vector3(0.25f, 0.01f, 0.12f), Mid); // Glint on the coffee
        Ring(t, new Vector3(0, 0.62f, 0), new Vector3(90, 0, 0), 0.5f, 0.06f, Light, 28);          // Rounded lip
        Cyl(t, new Vector3(0, -0.62f, 0), Vector3.zero, new Vector3(0.95f, 0.02f, 0.95f), Deep); // Foot
        var random = new System.Random(11);
        for (int i = 0; i < 7; i++)
        {
            float a = (-40 + i * 22) * Mathf.Deg2Rad;
            float length = 0.15f + (float)random.NextDouble() * 0.4f;
            var drip = new Vector3(Mathf.Sin(a) * 0.52f, 0.55f - length * 0.5f, -Mathf.Cos(a) * 0.52f);
            Box(t, drip, new Vector3(0, -a * Mathf.Rad2Deg, 0), new Vector3(0.06f, length, 0.02f), Dark);
            Sph(t, drip + new Vector3(0, -length * 0.5f, 0), Vector3.zero, new Vector3(0.08f, 0.08f, 0.04f), Dark);
        }
        Sph(t, new Vector3(-0.3f, -0.1f, -0.47f), Vector3.zero, new Vector3(0.12f, 0.1f, 0.05f), Dark); // Splashes
        Sph(t, new Vector3(-0.15f, -0.32f, -0.5f), Vector3.zero, new Vector3(0.07f, 0.06f, 0.04f), Dark);
        // Rounded loop handle and the bakery logo.
        Cap(t, new Vector3(0.68f, 0.3f, 0), new Vector3(0, 0, 90), new Vector3(0.14f, 0.2f, 0.14f), Body);
        Cap(t, new Vector3(0.68f, -0.26f, 0), new Vector3(0, 0, 90), new Vector3(0.14f, 0.2f, 0.14f), Body);
        Cap(t, new Vector3(0.84f, 0.02f, 0), Vector3.zero, new Vector3(0.14f, 0.34f, 0.14f), Body);
        Box(t, new Vector3(0, -0.2f, -0.53f), Vector3.zero, new Vector3(0.35f, 0.15f, 0.01f), Light);
        t.localRotation = Quaternion.Euler(0, -25, 0);
    }

    static void SecurityCamera(Transform t)
    {
        // Wall camera with hood, lens, REC light and bracket, and a frozen frame of the footage.
        var cam = Group(t, new Vector3(0, 0.35f, 0), new Vector3(0, 0, -12));
        Box(cam, Vector3.zero, Vector3.zero, new Vector3(1.3f, 0.45f, 0.5f), Light);
        Box(cam, new Vector3(0.1f, 0.27f, 0), Vector3.zero, new Vector3(1.45f, 0.05f, 0.62f), Body); // Sun hood
        Cyl(cam, new Vector3(0.67f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.36f, 0.05f, 0.36f), Dark);
        Cyl(cam, new Vector3(0.71f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.24f, 0.02f, 0.24f), Ink);
        Sph(cam, new Vector3(0.72f, 0.05f, -0.05f), Vector3.zero, Vector3.one * 0.06f, White); // Glint
        Sph(cam, new Vector3(0.45f, 0.12f, -0.26f), Vector3.zero, Vector3.one * 0.07f, Pale);  // REC light
        for (int i = 0; i < 3; i++)
            Box(cam, new Vector3(-0.45f + i * 0.08f, 0, -0.255f), Vector3.zero, new Vector3(0.025f, 0.3f, 0.01f), Mid); // Vents
        Box(t, new Vector3(-0.45f, 0.85f, 0), Vector3.zero, new Vector3(0.12f, 0.55f, 0.12f), Body);
        Box(t, new Vector3(-0.45f, 1.12f, 0), Vector3.zero, new Vector3(0.55f, 0.06f, 0.55f), Body);
        // Printed still: the officer leaning over the cup.
        var still = Group(t, new Vector3(0.1f, -0.55f, -0.2f), new Vector3(10, 0, 4));
        Box(still, Vector3.zero, Vector3.zero, new Vector3(1.3f, 0.85f, 0.02f), White);
        Box(still, new Vector3(0, 0.02f, -0.012f), Vector3.zero, new Vector3(1.18f, 0.7f, 0.005f), Deep);
        Box(still, new Vector3(-0.3f, -0.1f, -0.016f), Vector3.zero, new Vector3(0.18f, 0.3f, 0.004f), Ink); // Figure
        Sph(still, new Vector3(-0.22f, 0.12f, -0.016f), Vector3.zero, new Vector3(0.14f, 0.14f, 0.004f), Ink);
        Box(still, new Vector3(0.1f, -0.22f, -0.016f), Vector3.zero, new Vector3(0.5f, 0.04f, 0.004f), Mid); // Table
        Cyl(still, new Vector3(0.05f, -0.15f, -0.016f), new Vector3(90, 0, 0), new Vector3(0.08f, 0.002f, 0.08f), Light); // Cup
        Box(still, new Vector3(0.38f, 0.3f, -0.016f), Vector3.zero, new Vector3(0.3f, 0.06f, 0.004f), Light);  // Timestamp
    }

    static void Sachet(Transform t)
    {
        // Paper sachet as in the drawing: crimped ends, printed band, torn open, white powder spilling.
        Box(t, Vector3.zero, new Vector3(0, 0, -28), new Vector3(1.3f, 0.12f, 0.75f), Body);
        Sph(t, new Vector3(0, 0.03f, 0), new Vector3(0, 0, -28), new Vector3(1.15f, 0.16f, 0.65f), Body); // Puffed middle
        Box(t, new Vector3(0.05f, 0.11f, -0.02f), new Vector3(0, 0, -28), new Vector3(0.62f, 0.01f, 0.24f), Light); // "AÇÚCAR" band
        Box(t, new Vector3(0.05f, 0.118f, -0.02f), new Vector3(0, 0, -28), new Vector3(0.42f, 0.006f, 0.06f), Deep);
        var end = Quaternion.Euler(0, 0, -28) * new Vector3(0.62f, 0, 0);
        for (int i = 0; i < 6; i++) // Crimped end
            Box(t, end + new Vector3(0, 0, -0.3f + i * 0.12f), new Vector3(0, 0, -28), new Vector3(0.1f, 0.06f, 0.1f), i % 2 == 0 ? Body : Mid);
        var torn = Quaternion.Euler(0, 0, -28) * new Vector3(-0.66f, 0, 0);
        for (int i = 0; i < 5; i++) // Torn edge
            Box(t, torn + new Vector3(0, 0.02f * (i % 2), -0.28f + i * 0.14f), new Vector3(0, i * 25, -28 + (i % 2) * 30), new Vector3(0.1f, 0.05f, 0.1f), Body);
        var random = new System.Random(4);
        Sph(t, torn + new Vector3(-0.25f, -0.25f, 0), Vector3.zero, new Vector3(0.5f, 0.12f, 0.45f), White); // Spilled pile
        for (int i = 0; i < 16; i++)
        {
            float s = 0.04f + (float)random.NextDouble() * 0.05f;
            Box(t, torn + new Vector3(-0.25f - (float)random.NextDouble() * 0.6f, -0.3f, (float)random.NextDouble() * 0.7f - 0.35f),
                new Vector3(0, (float)random.NextDouble() * 90, 0), Vector3.one * s, White);
        }
        t.localRotation = Quaternion.Euler(-30, 0, 0);
    }

    static void OrderSlip(Transform t)
    {
        // Order slip impaled on the counter spike: "black coffee, no sugar", sugar crossed out.
        Cyl(t, new Vector3(0, -0.95f, 0), Vector3.zero, new Vector3(0.6f, 0.05f, 0.6f), Deep);
        Cyl(t, new Vector3(0, -0.2f, 0), Vector3.zero, new Vector3(0.04f, 0.75f, 0.04f), Light);
        var slip = Group(t, new Vector3(0, -0.15f, 0), new Vector3(0, 0, 4));
        Box(slip, Vector3.zero, Vector3.zero, new Vector3(0.8f, 1.2f, 0.01f), Pale);
        Box(slip, new Vector3(0, 0.48f, -0.008f), Vector3.zero, new Vector3(0.6f, 0.08f, 0.004f), Deep); // Bakery name
        for (int i = 0; i < 5; i++)
            Box(slip, new Vector3(-0.08f + (i % 2) * 0.04f, 0.28f - i * 0.15f, -0.008f), Vector3.zero, new Vector3(0.5f - (i % 3) * 0.06f, 0.035f, 0.004f), Ink);
        Box(slip, new Vector3(-0.06f, -0.02f, -0.012f), Vector3.zero, new Vector3(0.6f, 0.025f, 0.004f), Dark); // Strike-through
        Box(slip, new Vector3(0.15f, -0.45f, -0.008f), Vector3.zero, new Vector3(0.35f, 0.05f, 0.004f), Ink);   // Total
        for (int i = 0; i < 6; i++) // Torn perforation along the bottom
            Box(slip, new Vector3(-0.33f + i * 0.13f, -0.6f, 0), new Vector3(0, 0, 45), new Vector3(0.07f, 0.07f, 0.01f), Pale);
        t.localRotation = Quaternion.Euler(-8, 15, 0);
    }

    static void Envelope(Transform t)
    {
        // Envelope with the blackmail photos sliding out: the dented patrol car.
        Box(t, Vector3.zero, Vector3.zero, new Vector3(1.8f, 0.04f, 1.15f), Light);
        Box(t, new Vector3(0, 0.025f, 0.32f), new Vector3(-6, 0, 0), new Vector3(1.75f, 0.01f, 0.55f), Body); // Flap
        Box(t, new Vector3(0, 0.03f, -0.1f), new Vector3(0, 0, 0), new Vector3(1.7f, 0.006f, 0.02f), Mid);
        for (int i = 0; i < 2; i++)
        {
            var photo = Group(t, new Vector3(-0.25f + i * 0.45f, 0.06f + i * 0.02f, -0.55f - i * 0.05f), new Vector3(0, -12 + i * 20, 0));
            Box(photo, Vector3.zero, Vector3.zero, new Vector3(0.9f, 0.015f, 0.65f), White);
            Box(photo, new Vector3(0, 0.01f, 0.04f), Vector3.zero, new Vector3(0.8f, 0.006f, 0.48f), Dark);
            Box(photo, new Vector3(0, 0.016f, 0.02f), Vector3.zero, new Vector3(0.55f, 0.006f, 0.16f), Light); // Car body
            Box(photo, new Vector3(0, 0.018f, 0.12f), Vector3.zero, new Vector3(0.3f, 0.006f, 0.1f), Light);    // Cabin
            Box(photo, new Vector3(0, 0.02f, 0.13f), Vector3.zero, new Vector3(0.12f, 0.006f, 0.03f), Pale);    // Light bar
            Box(photo, new Vector3(-0.28f, 0.02f, -0.04f), new Vector3(0, 25, 0), new Vector3(0.08f, 0.006f, 0.1f), Ink); // Dent
            Cyl(photo, new Vector3(-0.18f, 0.02f, -0.1f), Vector3.zero, new Vector3(0.09f, 0.004f, 0.09f), Ink);
            Cyl(photo, new Vector3(0.18f, 0.02f, -0.1f), Vector3.zero, new Vector3(0.09f, 0.004f, 0.09f), Ink);
        }
        // Handwriting on the back of the top photo.
        Box(t, new Vector3(0.55f, 0.03f, 0.1f), new Vector3(0, 8, 0), new Vector3(0.6f, 0.005f, 0.04f), Ink);
        Box(t, new Vector3(0.5f, 0.03f, -0.02f), new Vector3(0, 8, 0), new Vector3(0.45f, 0.005f, 0.04f), Ink);
        t.localRotation = Quaternion.Euler(-45, 0, 0);
    }

    static void Notebook(Transform t)
    {
        // Spiral notebook of monthly payments, each line tagged with the car number.
        Box(t, Vector3.zero, Vector3.zero, new Vector3(1.25f, 0.08f, 1.65f), Pale);
        Box(t, new Vector3(0, -0.05f, 0), Vector3.zero, new Vector3(1.28f, 0.03f, 1.68f), Deep);
        for (int i = 0; i < 11; i++)
            Ring(t, new Vector3(-0.66f, 0.0f, 0.72f - i * 0.144f), new Vector3(0, 90, 0), 0.07f, 0.018f, Light);
        Box(t, new Vector3(-0.4f, 0.045f, 0), Vector3.zero, new Vector3(0.01f, 0.004f, 1.55f), Mid); // Margin
        for (int i = 0; i < 9; i++)
        {
            float z = 0.6f - i * 0.15f;
            Box(t, new Vector3(0.05f, 0.044f, z), Vector3.zero, new Vector3(1.1f, 0.003f, 0.01f), Mid);
            Box(t, new Vector3(-0.15f, 0.048f, z + 0.05f), Vector3.zero, new Vector3(0.35f, 0.005f, 0.035f), Ink); // Date
            Box(t, new Vector3(0.25f, 0.048f, z + 0.05f), Vector3.zero, new Vector3(0.18f, 0.005f, 0.035f), Ink);  // Amount
            Box(t, new Vector3(0.45f, 0.048f, z + 0.05f), Vector3.zero, new Vector3(0.12f, 0.005f, 0.05f), Dark);  // Car number
        }
        t.localRotation = Quaternion.Euler(-40, 0, 8);
    }

    static void Newspaper(Transform t)
    {
        // Clipping: headline, photo of the parking lot and columns of text, torn edges.
        Box(t, Vector3.zero, new Vector3(0, -6, 0), new Vector3(1.5f, 0.02f, 1.9f), Light);
        var rot = Quaternion.Euler(0, -6, 0);
        Box(t, rot * new Vector3(0, 0.012f, 0.72f), new Vector3(0, -6, 0), new Vector3(1.3f, 0.006f, 0.2f), Ink);  // Headline
        Box(t, rot * new Vector3(0, 0.012f, 0.5f), new Vector3(0, -6, 0), new Vector3(1.0f, 0.006f, 0.08f), Deep);
        Box(t, rot * new Vector3(-0.3f, 0.012f, 0.05f), new Vector3(0, -6, 0), new Vector3(0.7f, 0.006f, 0.6f), Dark); // Photo
        Box(t, rot * new Vector3(-0.3f, 0.016f, -0.02f), new Vector3(0, -6, 30), new Vector3(0.45f, 0.006f, 0.05f), Light); // Bicycle frame
        Cyl(t, rot * new Vector3(-0.48f, 0.016f, -0.12f), Vector3.zero, new Vector3(0.18f, 0.003f, 0.18f), Light);
        Cyl(t, rot * new Vector3(-0.12f, 0.016f, -0.12f), Vector3.zero, new Vector3(0.18f, 0.003f, 0.18f), Light);
        for (int i = 0; i < 4; i++)
            Box(t, rot * new Vector3(0.42f, 0.012f, 0.3f - i * 0.12f), new Vector3(0, -6, 0), new Vector3(0.45f, 0.005f, 0.035f), Mid);
        for (int col = 0; col < 2; col++)
            for (int i = 0; i < 6; i++)
                Box(t, rot * new Vector3(-0.35f + col * 0.7f, 0.012f, -0.4f - i * 0.1f), new Vector3(0, -6, 0), new Vector3(0.58f, 0.005f, 0.03f), Mid);
        for (int i = 0; i < 7; i++) // Torn bottom edge
            Box(t, rot * new Vector3(-0.66f + i * 0.22f, 0, -0.97f), new Vector3(0, 45 - 6, 0), new Vector3(0.12f, 0.02f, 0.12f), Light);
        t.localRotation = Quaternion.Euler(-45, 0, 0);
    }

    // ---------- Reports (the red folder from the drawing) ----------

    enum Sheet { Autopsy, Toxicology, Repair }

    static void Folder(Transform t, Sheet sheet)
    {
        // Closed folder as in the drawing (tab, label, papers inside); the key page slides out to the right.
        Box(t, new Vector3(-0.3f, -0.05f, 0), Vector3.zero, new Vector3(1.6f, 0.035f, 1.2f), Deep);        // Back cover
        Box(t, new Vector3(-0.27f, -0.015f, -0.02f), Vector3.zero, new Vector3(1.52f, 0.05f, 1.12f), Pale); // Papers
        for (int i = 0; i < 4; i++)
            Box(t, new Vector3(-0.27f, -0.035f + i * 0.012f, -0.585f), Vector3.zero, new Vector3(1.5f, 0.004f, 0.01f), Mid);
        Box(t, new Vector3(-0.3f, 0.05f, 0), Vector3.zero, new Vector3(1.6f, 0.035f, 1.2f), Body);          // Front cover
        Box(t, new Vector3(-0.7f, 0.05f, 0.66f), Vector3.zero, new Vector3(0.42f, 0.035f, 0.14f), Body);    // Tab
        Box(t, new Vector3(-0.3f, 0.072f, 0.28f), Vector3.zero, new Vector3(0.5f, 0.01f, 0.24f), Light);    // Label
        Box(t, new Vector3(-0.3f, 0.08f, 0.28f), Vector3.zero, new Vector3(0.36f, 0.006f, 0.05f), Dark);
        Box(t, new Vector3(-0.3f, 0.07f, -0.35f), Vector3.zero, new Vector3(1.3f, 0.006f, 0.02f), Deep);    // Crease
        var sheetRoot = Group(t, new Vector3(0.75f, 0.1f, -0.05f), new Vector3(0, 10, 0));
        Box(sheetRoot, Vector3.zero, Vector3.zero, new Vector3(1.2f, 0.01f, 1.0f), White);
        var page = Group(sheetRoot, new Vector3(0.05f, 0.008f, 0), Vector3.zero);
        Box(page, new Vector3(0, 0, 0.42f), Vector3.zero, new Vector3(0.8f, 0.006f, 0.08f), Ink); // Title
        for (int i = 0; i < 5; i++)
            Box(page, new Vector3(0.25f, 0, 0.22f - i * 0.12f), Vector3.zero, new Vector3(0.45f - (i % 2) * 0.1f, 0.005f, 0.03f), Mid);
        switch (sheet)
        {
            case Sheet.Autopsy: // Body chart with the injection mark at the neck.
                Sph(page, new Vector3(-0.35f, 0.004f, 0.18f), Vector3.zero, new Vector3(0.14f, 0.005f, 0.14f), Dark);
                Box(page, new Vector3(-0.35f, 0.004f, -0.08f), Vector3.zero, new Vector3(0.2f, 0.005f, 0.36f), Dark);
                Box(page, new Vector3(-0.35f, 0.004f, -0.38f), Vector3.zero, new Vector3(0.16f, 0.005f, 0.25f), Dark);
                Ring(page, new Vector3(-0.3f, 0.01f, 0.09f), new Vector3(90, 0, 0), 0.07f, 0.015f, Ink);
                break;
            case Sheet.Toxicology: // Chart with a spike, and a sample vial.
                for (int i = 0; i < 6; i++)
                    Box(page, new Vector3(-0.5f + i * 0.08f, 0.004f, -0.35f + (i == 3 ? 0.18f : 0.05f)), Vector3.zero, new Vector3(0.05f, 0.005f, i == 3 ? 0.4f : 0.12f), i == 3 ? Ink : Dark);
                Cyl(t, new Vector3(-0.85f, 0.32f, -0.3f), Vector3.zero, new Vector3(0.16f, 0.25f, 0.16f), Pale);
                Cyl(t, new Vector3(-0.85f, 0.24f, -0.3f), Vector3.zero, new Vector3(0.14f, 0.15f, 0.14f), Deep);
                Cyl(t, new Vector3(-0.85f, 0.59f, -0.3f), Vector3.zero, new Vector3(0.18f, 0.05f, 0.18f), Dark);
                break;
            case Sheet.Repair: // Invoice with the front bumper sketched and the cash total circled.
                Box(page, new Vector3(-0.32f, 0.004f, -0.1f), Vector3.zero, new Vector3(0.45f, 0.005f, 0.14f), Dark);
                Box(page, new Vector3(-0.32f, 0.006f, -0.1f), Vector3.zero, new Vector3(0.35f, 0.005f, 0.05f), Light);
                Ring(page, new Vector3(0.3f, 0.01f, -0.38f), new Vector3(90, 0, 0), 0.15f, 0.015f, Ink);
                Box(page, new Vector3(0.3f, 0.004f, -0.38f), Vector3.zero, new Vector3(0.18f, 0.005f, 0.04f), Ink);
                break;
        }
        // Paper clip holding the page.
        Box(sheetRoot, new Vector3(0.1f, 0.02f, 0.45f), Vector3.zero, new Vector3(0.05f, 0.03f, 0.3f), Light);
        Box(sheetRoot, new Vector3(0.18f, 0.02f, 0.45f), Vector3.zero, new Vector3(0.05f, 0.03f, 0.24f), Light);
        t.localRotation = Quaternion.Euler(-42, 0, 0);
    }

    static void EvidenceBag(Transform t)
    {
        Box(t, Vector3.zero, Vector3.zero, new Vector3(1.2f, 1.6f, 0.08f), Pale);
        Box(t, new Vector3(0, 0.55f, -0.05f), Vector3.zero, new Vector3(1.1f, 0.3f, 0.01f), Body);
        Box(t, new Vector3(0, 0.72f, -0.05f), Vector3.zero, new Vector3(1.2f, 0.04f, 0.02f), Dark); // Seal strip
    }

    // ---------- Helpers ----------

    static void Print(Transform parent, Vector3 centre, float radius, Color color)
    {
        // A fingerprint: three nested loops, flattened onto the card.
        for (int i = 0; i < 3; i++)
            Ring(parent, centre + new Vector3(0, 0.001f * i, 0), new Vector3(90, 0, 0), radius * (1 - i * 0.3f), 0.008f, color, 14);
    }

    static Transform Group(Transform parent, Vector3 position, Vector3 euler = default)
    {
        var group = new GameObject("Grupo").transform;
        group.SetParent(parent, false);
        group.localPosition = position;
        group.localRotation = Quaternion.Euler(euler);
        return group;
    }

    /// A ring in the group's XY plane (rotate with euler), made of short cylinders.
    static void Ring(Transform parent, Vector3 centre, Vector3 euler, float radius, float thickness, Color color, int segments = 20)
    {
        var ring = Group(parent, centre, euler);
        float step = Mathf.PI * 2 / segments;
        float length = 2 * radius * Mathf.Sin(step / 2) * 1.08f;
        for (int i = 0; i < segments; i++)
        {
            float a = (i + 0.5f) * step;
            Cyl(ring, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0), new Vector3(0, 0, a * Mathf.Rad2Deg),
                new Vector3(thickness, length * 0.5f, thickness), color, false);
        }
    }

    static void Box(Transform p, Vector3 pos, Vector3 euler, Vector3 scale, Color color) => Part(p, PrimitiveType.Cube, pos, euler, scale, color, true);
    static void Sph(Transform p, Vector3 pos, Vector3 euler, Vector3 scale, Color color) => Part(p, PrimitiveType.Sphere, pos, euler, scale, color, true);
    static void Cap(Transform p, Vector3 pos, Vector3 euler, Vector3 scale, Color color) => Part(p, PrimitiveType.Capsule, pos, euler, scale, color, true);
    static void Cyl(Transform p, Vector3 pos, Vector3 euler, Vector3 scale, Color color, bool outlined = true) => Part(p, PrimitiveType.Cylinder, pos, euler, scale, color, outlined);

    static void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 euler, Vector3 scale, Color color, bool outlined)
    {
        var part = GameObject.CreatePrimitive(type);
        Object.Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(euler);
        part.transform.localScale = scale;
        var renderer = part.GetComponent<MeshRenderer>();
        var outlineMaterial = outlined ? Outline() : null;
        // A second material on a single-submesh mesh draws the mesh again: that pass is the ink outline.
        renderer.sharedMaterials = outlineMaterial != null ? new[] { MaterialFor(color), outlineMaterial } : new[] { MaterialFor(color) };
    }

    static Material Outline()
    {
        if (outline != null) return outline;
        if (outlineShader == null) outlineShader = Shader.Find("CaseZero/Evidence3DOutline");
        if (outlineShader == null) return null;
        outline = new Material(outlineShader) { name = "Pista 3D • contorno" };
        return outline;
    }

    public static Material MaterialFor(Color color)
    {
        if (materials.TryGetValue(color, out var material) && material != null) return material;
        if (shader == null) shader = Shader.Find("CaseZero/Evidence3D");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        material = new Material(shader) { name = "Pista 3D" };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Shine", color.r > 0.85f ? 0.45f : color.r < 0.25f ? 0f : 0.18f); // No glare on dark ink and blood.
        materials[color] = material;
        return material;
    }
}

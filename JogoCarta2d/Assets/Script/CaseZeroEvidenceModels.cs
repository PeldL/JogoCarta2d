using System.Collections.Generic;
using UnityEngine;

// Placeholder 3D models for each clue, built from primitives. Replace any of them by
// assigning a real model in ArtesCasoZero ▸ "Modelos 3D das pistas".
public static class CaseZeroEvidenceModels
{
    static Shader shader;
    static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    static readonly Color Metal = new Color(0.22f, 0.23f, 0.25f);
    static readonly Color DarkMetal = new Color(0.1f, 0.1f, 0.11f);
    static readonly Color Steel = new Color(0.72f, 0.74f, 0.78f);
    static readonly Color Brass = new Color(0.85f, 0.64f, 0.24f);
    static readonly Color Blood = new Color(0.45f, 0.03f, 0.03f);
    static readonly Color Paper = new Color(0.94f, 0.92f, 0.85f);
    static readonly Color Ink = new Color(0.15f, 0.15f, 0.2f);
    static readonly Color Wood = new Color(0.42f, 0.27f, 0.15f);
    static readonly Color Leather = new Color(0.32f, 0.18f, 0.1f);
    static readonly Color Floor = new Color(0.36f, 0.33f, 0.3f);
    static readonly Color Porcelain = new Color(0.95f, 0.95f, 0.93f);
    static readonly Color Coffee = new Color(0.25f, 0.13f, 0.06f);
    static readonly Color Uniform = new Color(0.12f, 0.14f, 0.2f);
    static readonly Color Gray = new Color(0.45f, 0.46f, 0.47f);
    static readonly Color Yellow = new Color(0.95f, 0.78f, 0.15f);
    static readonly Color Red = new Color(0.65f, 0.12f, 0.1f);
    static readonly Color Green = new Color(0.15f, 0.55f, 0.3f);

    /// Builds the model for a clue key, or null when the clue has no physical object.
    public static GameObject Build(string key)
    {
        var root = new GameObject("Modelo 3D • " + key);
        switch (key)
        {
            // Case 1 • The Last Witness
            case "registro": Book(root.transform, Green, false); break;
            case "pistola": Pistol(root.transform); break;
            case "capsula": CartridgeCase(root.transform); break;
            case "sangue": BloodOnFloor(root.transform, Floor); break;
            // Case 2 • The Second Opinion
            case "c2_rua_lixeira": Dumpster(root.transform); break;
            case "c2_rua_cracha": KeyRing(root.transform); break;
            case "c2_rua_marcas": WheelTracks(root.transform); break;
            case "c2_lab_seringa": Syringe(root.transform); break;
            case "c2_lab_estoque": Book(root.transform, Leather, true); break;
            case "c2_lab_camera": Terminal(root.transform); break;
            case "c2_lab_maca": Trolley(root.transform); break;
            case "c2_police_autopsia": Report(root.transform, false); break;
            case "c2_police_rascunho": Letter(root.transform); break;
            case "c2_police_digitais": Report(root.transform, false); break;
            case "c2_police_transferencia": Receipt(root.transform); break;
            // Case 3 • The Last Coffee
            case "c3_padaria_xicara": Cup(root.transform); break;
            case "c3_padaria_camera": SecurityCamera(root.transform); break;
            case "c3_padaria_sache": Sachet(root.transform); break;
            case "c3_padaria_comanda": Receipt(root.transform); break;
            case "c3_est_envelope": Letter(root.transform); break;
            case "c3_est_caderno": Book(root.transform, Leather, false); break;
            case "c3_est_jornal": Report(root.transform, false); break;
            case "c3_police_tox": Report(root.transform, true); break;
            case "c3_police_livro": Book(root.transform, Green, true); break;
            case "c3_police_digitais": Report(root.transform, false); break;
            case "c3_police_viatura": Report(root.transform, false); break;
            case "depoimento": Object.Destroy(root); return null;
            default: EvidenceBag(root.transform); break;
        }
        return root;
    }

    // ---------- Models ----------

    static void Pistol(Transform t)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0.15f, 0.32f, 0), Vector3.zero, new Vector3(1.9f, 0.34f, 0.3f), DarkMetal); // Slide
        Part(t, PrimitiveType.Cube, new Vector3(0.1f, 0.08f, 0), Vector3.zero, new Vector3(1.5f, 0.18f, 0.27f), Metal);     // Frame
        Part(t, PrimitiveType.Cube, new Vector3(-0.45f, -0.4f, 0), new Vector3(0, 0, -15), new Vector3(0.42f, 0.9f, 0.3f), Metal); // Grip
        Part(t, PrimitiveType.Cylinder, new Vector3(1.12f, 0.32f, 0), new Vector3(0, 0, 90), new Vector3(0.09f, 0.04f, 0.09f), Color.black); // Muzzle
        Part(t, PrimitiveType.Cube, new Vector3(0.05f, -0.12f, 0), Vector3.zero, new Vector3(0.45f, 0.05f, 0.12f), Metal);  // Trigger guard
        Part(t, PrimitiveType.Cube, new Vector3(0.25f, -0.07f, 0), Vector3.zero, new Vector3(0.05f, 0.14f, 0.12f), Metal);
        Part(t, PrimitiveType.Cube, new Vector3(-0.02f, -0.05f, 0), new Vector3(0, 0, 15), new Vector3(0.05f, 0.14f, 0.06f), Steel); // Trigger
        for (int i = 0; i < 6; i++)
            Part(t, PrimitiveType.Cube, new Vector3(-0.55f + i * 0.07f, 0.32f, 0), Vector3.zero, new Vector3(0.025f, 0.3f, 0.31f), Color.black); // Serrations
    }

    static void CartridgeCase(Transform t)
    {
        Part(t, PrimitiveType.Cylinder, new Vector3(0, 0, 0), Vector3.zero, new Vector3(0.45f, 0.65f, 0.45f), Brass);
        Part(t, PrimitiveType.Cylinder, new Vector3(0, -0.66f, 0), Vector3.zero, new Vector3(0.5f, 0.05f, 0.5f), Brass);
        Part(t, PrimitiveType.Cylinder, new Vector3(0, -0.71f, 0), Vector3.zero, new Vector3(0.14f, 0.01f, 0.14f), Steel); // Primer
        Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.66f, 0), Vector3.zero, new Vector3(0.38f, 0.01f, 0.38f), Color.black); // Open mouth
        t.localRotation = Quaternion.Euler(0, 0, 70);
    }

    static void BloodOnFloor(Transform t, Color floor)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0, -0.1f, 0), Vector3.zero, new Vector3(2.2f, 0.12f, 1.6f), floor);
        var random = new System.Random(5);
        for (int i = 0; i < 9; i++)
        {
            float s = 0.15f + (float)random.NextDouble() * 0.45f;
            Part(t, PrimitiveType.Sphere, new Vector3((float)random.NextDouble() * 1.4f - 0.7f, -0.03f, (float)random.NextDouble() * 1f - 0.5f),
                Vector3.zero, new Vector3(s, 0.03f, s * 0.8f), Blood);
        }
        t.localRotation = Quaternion.Euler(-35, 0, 0); // Tilted toward the viewer: the stains face the camera.
    }

    static void Rug(Transform t)
    {
        Part(t, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(2.4f, 0.05f, 1.5f), new Color(0.5f, 0.18f, 0.16f));
        for (int i = 0; i < 4; i++)
            Part(t, PrimitiveType.Cube, new Vector3(-0.9f + i * 0.6f, 0.03f, 0), Vector3.zero, new Vector3(0.1f, 0.02f, 1.45f), new Color(0.85f, 0.7f, 0.45f));
        Part(t, PrimitiveType.Sphere, new Vector3(0.35f, 0.04f, 0.2f), Vector3.zero, new Vector3(0.22f, 0.02f, 0.16f), Blood); // Little blood
        t.localRotation = Quaternion.Euler(-35, 0, 0);
    }

    static void Book(Transform t, Color cover, bool ribbon)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0, -0.1f, 0), Vector3.zero, new Vector3(1.2f, 0.04f, 1.6f), cover);
        Part(t, PrimitiveType.Cube, new Vector3(0.02f, 0, 0), Vector3.zero, new Vector3(1.12f, 0.18f, 1.52f), Paper);
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.1f, 0), Vector3.zero, new Vector3(1.2f, 0.04f, 1.6f), cover);
        Part(t, PrimitiveType.Cube, new Vector3(-0.6f, 0, 0), Vector3.zero, new Vector3(0.06f, 0.24f, 1.6f), cover);
        if (ribbon) Part(t, PrimitiveType.Cube, new Vector3(0.2f, 0.02f, -0.85f), Vector3.zero, new Vector3(0.06f, 0.01f, 0.3f), Red);
        t.localRotation = Quaternion.Euler(-35, 0, 0);
    }

    static void KeyRing(Transform t)
    {
        for (int i = 0; i < 20; i++)
        {
            float a = i / 20f * Mathf.PI * 2;
            Part(t, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.35f, 0), Vector3.zero, Vector3.one * 0.08f, Steel);
        }
        for (int k = 0; k < 3; k++)
        {
            float a = -1.2f - k * 0.45f;
            var key = new GameObject("Chave").transform;
            key.SetParent(t, false);
            key.localPosition = new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.35f, 0);
            key.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
            var color = k == 1 ? Brass : Steel;
            Part(key, PrimitiveType.Cylinder, new Vector3(0.18f, 0, 0), new Vector3(90, 0, 0), new Vector3(0.28f, 0.02f, 0.28f), color); // Head
            Part(key, PrimitiveType.Cube, new Vector3(0.62f, 0, 0), Vector3.zero, new Vector3(0.6f, 0.08f, 0.03f), color);              // Blade
            Part(key, PrimitiveType.Cube, new Vector3(0.75f, -0.06f, 0), Vector3.zero, new Vector3(0.08f, 0.06f, 0.03f), color);        // Teeth
            Part(key, PrimitiveType.Cube, new Vector3(0.88f, -0.05f, 0), Vector3.zero, new Vector3(0.06f, 0.05f, 0.03f), color);
        }
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.55f, 0), Vector3.zero, new Vector3(0.35f, 0.22f, 0.04f), Red); // Tag
    }

    static void Bench(Transform t)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.4f, 0), Vector3.zero, new Vector3(2.2f, 0.1f, 0.6f), Wood);
        foreach (var x in new[] { -0.95f, 0.95f })
            Part(t, PrimitiveType.Cube, new Vector3(x, 0, 0), Vector3.zero, new Vector3(0.1f, 0.8f, 0.5f), Wood);
        Part(t, PrimitiveType.Cube, new Vector3(0, -0.42f, 0), Vector3.zero, new Vector3(2.6f, 0.05f, 1.4f), Floor);
        Part(t, PrimitiveType.Sphere, new Vector3(0.2f, -0.39f, 0.1f), Vector3.zero, new Vector3(0.8f, 0.02f, 0.55f), Blood);
        Part(t, PrimitiveType.Sphere, new Vector3(-0.35f, -0.39f, -0.25f), Vector3.zero, new Vector3(0.25f, 0.02f, 0.2f), Blood);
    }

    static void UniformButton(Transform t)
    {
        Part(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(90, 0, 0), new Vector3(1f, 0.08f, 1f), DarkMetal);
        Part(t, PrimitiveType.Cylinder, new Vector3(0, 0, -0.07f), new Vector3(90, 0, 0), new Vector3(0.8f, 0.02f, 0.8f), Metal);
        foreach (var p in new[] { new Vector2(-0.14f, 0.14f), new Vector2(0.14f, 0.14f), new Vector2(-0.14f, -0.14f), new Vector2(0.14f, -0.14f) })
            Part(t, PrimitiveType.Cylinder, new Vector3(p.x, p.y, -0.09f), new Vector3(90, 0, 0), new Vector3(0.1f, 0.01f, 0.1f), Color.black);
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.3f, -0.09f), Vector3.zero, new Vector3(0.36f, 0.08f, 0.01f), Yellow); // Company logo
        Part(t, PrimitiveType.Cube, new Vector3(0.05f, -0.05f, 0.15f), new Vector3(0, 0, 30), new Vector3(0.5f, 0.02f, 0.02f), Uniform); // Torn thread
    }

    static void Terminal(Transform t)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), Vector3.zero, new Vector3(1.6f, 1.05f, 0.15f), DarkMetal);
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.47f, -0.08f), Vector3.zero, new Vector3(1.42f, 0.86f, 0.01f), new Color(0.05f, 0.25f, 0.12f));
        for (int i = 0; i < 4; i++)
            Part(t, PrimitiveType.Cube, new Vector3(-0.35f + i * 0.04f, 0.72f - i * 0.15f, -0.09f), Vector3.zero, new Vector3(0.6f + (i % 2) * 0.3f, 0.05f, 0.005f), new Color(0.3f, 0.95f, 0.45f));
        Part(t, PrimitiveType.Cube, new Vector3(0.35f, 0.2f, -0.09f), Vector3.zero, new Vector3(0.5f, 0.1f, 0.005f), Red); // "CAMERAS OFF"
        Part(t, PrimitiveType.Cube, new Vector3(0, -0.15f, 0), Vector3.zero, new Vector3(0.15f, 0.3f, 0.12f), DarkMetal);
        Part(t, PrimitiveType.Cube, new Vector3(0, -0.3f, 0), Vector3.zero, new Vector3(0.7f, 0.04f, 0.5f), DarkMetal);
        Part(t, PrimitiveType.Cube, new Vector3(0, -0.3f, -0.6f), Vector3.zero, new Vector3(1.4f, 0.06f, 0.45f), Metal); // Keyboard
    }

    static void Report(Transform t, bool vial)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0, -0.03f, 0), Vector3.zero, new Vector3(1.3f, 0.04f, 1.75f), Wood); // Clipboard
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.01f, -0.05f), Vector3.zero, new Vector3(1.15f, 0.01f, 1.5f), Paper);
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.03f, 0.82f), Vector3.zero, new Vector3(0.5f, 0.06f, 0.14f), Steel); // Clip
        for (int i = 0; i < 8; i++)
            Part(t, PrimitiveType.Cube, new Vector3(-0.1f + (i % 3) * 0.05f, 0.02f, 0.55f - i * 0.15f), Vector3.zero, new Vector3(0.8f - (i % 3) * 0.1f, 0.005f, 0.04f), Ink);
        Part(t, PrimitiveType.Cylinder, new Vector3(0.35f, 0.02f, -0.6f), Vector3.zero, new Vector3(0.25f, 0.005f, 0.25f), Red); // Stamp
        if (vial)
        {
            Part(t, PrimitiveType.Cylinder, new Vector3(0.85f, 0.35f, 0), Vector3.zero, new Vector3(0.2f, 0.35f, 0.2f), new Color(0.75f, 0.9f, 0.95f));
            Part(t, PrimitiveType.Cylinder, new Vector3(0.85f, 0.2f, 0), Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), new Color(0.35f, 0.75f, 0.2f));
            Part(t, PrimitiveType.Cylinder, new Vector3(0.85f, 0.75f, 0), Vector3.zero, new Vector3(0.22f, 0.06f, 0.22f), Red);
        }
        t.localRotation = Quaternion.Euler(-40, 0, 0);
    }

    static void Shirt(Transform t, Color cloth, Color seam, bool missingButton)
    {
        Part(t, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(1.2f, 1.4f, 0.35f), cloth);
        Part(t, PrimitiveType.Cube, new Vector3(-0.8f, 0.35f, 0), new Vector3(0, 0, 35), new Vector3(0.38f, 0.8f, 0.3f), cloth);
        Part(t, PrimitiveType.Cube, new Vector3(0.8f, 0.35f, 0), new Vector3(0, 0, -35), new Vector3(0.38f, 0.8f, 0.3f), cloth);
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.72f, -0.05f), Vector3.zero, new Vector3(0.6f, 0.12f, 0.3f), cloth); // Collar
        Part(t, PrimitiveType.Cube, new Vector3(0, 0, -0.18f), Vector3.zero, new Vector3(0.02f, 1.35f, 0.01f), seam);
        for (int i = 0; i < 5; i++)
        {
            if (missingButton && i == 2)
            {
                Part(t, PrimitiveType.Cube, new Vector3(0.08f, 0.45f - i * 0.25f, -0.185f), new Vector3(0, 0, 25), new Vector3(0.12f, 0.015f, 0.01f), Yellow); // Loose threads
                continue;
            }
            Part(t, PrimitiveType.Cylinder, new Vector3(0.08f, 0.45f - i * 0.25f, -0.19f), new Vector3(90, 0, 0), new Vector3(0.1f, 0.01f, 0.1f), DarkMetal);
        }
        if (!missingButton) Part(t, PrimitiveType.Cube, new Vector3(-0.35f, -0.4f, -0.18f), new Vector3(0, 0, -20), new Vector3(0.3f, 0.18f, 0.01f), Color.black); // Tear
        Part(t, PrimitiveType.Cube, new Vector3(-0.35f, 0.4f, -0.18f), Vector3.zero, new Vector3(0.25f, 0.12f, 0.01f), Yellow); // Badge
    }

    static void Knife(Transform t, bool bloody)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0.55f, 0, 0), Vector3.zero, new Vector3(1.3f, 0.26f, 0.03f), Steel);
        Part(t, PrimitiveType.Cube, new Vector3(1.25f, -0.05f, 0), new Vector3(0, 0, 25), new Vector3(0.25f, 0.18f, 0.03f), Steel); // Tip
        Part(t, PrimitiveType.Cube, new Vector3(-0.15f, 0, 0), Vector3.zero, new Vector3(0.08f, 0.38f, 0.12f), DarkMetal); // Guard
        Part(t, PrimitiveType.Cube, new Vector3(-0.6f, 0, 0), Vector3.zero, new Vector3(0.85f, 0.22f, 0.16f), Wood);
        if (bloody)
        {
            Part(t, PrimitiveType.Cube, new Vector3(0.8f, 0.02f, -0.02f), Vector3.zero, new Vector3(0.6f, 0.15f, 0.005f), Blood);
            Part(t, PrimitiveType.Cube, new Vector3(0.8f, 0.02f, 0.02f), Vector3.zero, new Vector3(0.5f, 0.12f, 0.005f), Blood);
        }
        t.localRotation = Quaternion.Euler(0, 0, 15);
    }

    static void Receipt(Transform t)
    {
        Part(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0, 0, 3), new Vector3(0.7f, 1.7f, 0.01f), Paper);
        for (int i = 0; i < 9; i++)
            Part(t, PrimitiveType.Cube, new Vector3(-0.05f + (i % 2) * 0.05f, 0.65f - i * 0.15f, -0.01f), new Vector3(0, 0, 3), new Vector3(0.45f + (i % 3) * 0.05f, 0.03f, 0.005f), Ink);
        Part(t, PrimitiveType.Sphere, new Vector3(0.15f, -0.55f, -0.01f), Vector3.zero, new Vector3(0.25f, 0.18f, 0.005f), Coffee); // Stain
    }

    static void Cloth(Transform t)
    {
        Part(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0, 0, 0), new Vector3(1.8f, 0.04f, 1.3f), Porcelain);
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.03f, -0.5f), Vector3.zero, new Vector3(1.6f, 0.01f, 0.12f), Red); // Bakery stripe
        Part(t, PrimitiveType.Sphere, new Vector3(0.3f, 0.03f, 0.2f), Vector3.zero, new Vector3(0.5f, 0.02f, 0.4f), Coffee);
        Part(t, PrimitiveType.Sphere, new Vector3(-0.4f, 0.03f, 0.25f), Vector3.zero, new Vector3(0.2f, 0.02f, 0.15f), Coffee);
        t.localRotation = Quaternion.Euler(-30, 10, 8);
    }

    static void Cup(Transform t)
    {
        Part(t, PrimitiveType.Cylinder, new Vector3(0, -0.45f, 0), Vector3.zero, new Vector3(1.2f, 0.03f, 1.2f), Porcelain); // Saucer
        Part(t, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.7f, 0.42f, 0.7f), Porcelain);
        Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.42f, 0), Vector3.zero, new Vector3(0.6f, 0.005f, 0.6f), Coffee);
        Part(t, PrimitiveType.Cylinder, new Vector3(0.42f, 0.05f, 0), new Vector3(90, 0, 0), new Vector3(0.32f, 0.03f, 0.32f), Porcelain); // Handle
        Part(t, PrimitiveType.Cylinder, new Vector3(0.42f, 0.05f, 0), new Vector3(90, 0, 0), new Vector3(0.18f, 0.04f, 0.18f), Coffee);
        Part(t, PrimitiveType.Sphere, new Vector3(-0.2f, -0.43f, 0.35f), Vector3.zero, new Vector3(0.3f, 0.01f, 0.2f), Coffee); // Spill
    }

    static void SecurityCamera(Transform t)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(0, 0, -10), new Vector3(1.3f, 0.45f, 0.5f), Porcelain);
        Part(t, PrimitiveType.Cylinder, new Vector3(0.72f, -0.1f, 0), new Vector3(0, 0, 80), new Vector3(0.32f, 0.08f, 0.32f), DarkMetal);
        Part(t, PrimitiveType.Cylinder, new Vector3(0.8f, -0.11f, 0), new Vector3(0, 0, 80), new Vector3(0.2f, 0.02f, 0.2f), new Color(0.2f, 0.3f, 0.6f));
        Part(t, PrimitiveType.Sphere, new Vector3(0.35f, 0.18f, -0.26f), Vector3.zero, Vector3.one * 0.06f, Red); // REC light
        Part(t, PrimitiveType.Cube, new Vector3(-0.4f, 0.45f, 0), Vector3.zero, new Vector3(0.12f, 0.6f, 0.12f), Metal);
        Part(t, PrimitiveType.Cube, new Vector3(-0.4f, 0.78f, 0), Vector3.zero, new Vector3(0.6f, 0.08f, 0.6f), Metal);
    }

    static void FabricScrap(Transform t)
    {
        Part(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0, 0, 12), new Vector3(1.2f, 0.8f, 0.03f), Gray);
        Part(t, PrimitiveType.Cube, new Vector3(0.45f, -0.25f, 0), new Vector3(0, 0, -30), new Vector3(0.5f, 0.4f, 0.03f), Gray);
        for (int i = 0; i < 6; i++)
            Part(t, PrimitiveType.Cube, new Vector3(-0.45f + i * 0.17f, 0.05f + i * 0.035f, -0.02f), new Vector3(0, 0, 12), new Vector3(0.1f, 0.025f, 0.01f), Yellow); // Stitching
        Part(t, PrimitiveType.Cube, new Vector3(0.62f, 0.25f, 0), new Vector3(0, 0, 70), new Vector3(0.25f, 0.015f, 0.01f), Gray); // Frayed thread
    }

    static void Letter(Transform t)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0.2f, -0.05f, 0.15f), new Vector3(0, 10, 0), new Vector3(1.5f, 0.03f, 0.95f), new Color(0.88f, 0.8f, 0.62f)); // Envelope
        Part(t, PrimitiveType.Cube, new Vector3(-0.1f, 0.02f, -0.1f), new Vector3(0, -8, 0), new Vector3(1.1f, 0.01f, 1.5f), Paper);
        for (int i = 0; i < 7; i++)
            Part(t, PrimitiveType.Cube, new Vector3(-0.1f, 0.03f, 0.45f - i * 0.15f), new Vector3(0, -8, 0), new Vector3(0.75f, 0.005f, 0.035f), Ink);
        Part(t, PrimitiveType.Cylinder, new Vector3(0.6f, 0, 0.3f), Vector3.zero, new Vector3(0.22f, 0.04f, 0.22f), Red); // Seal
        t.localRotation = Quaternion.Euler(-45, 0, 0);
    }

    static void Syringe(Transform t)
    {
        Part(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0, 0, 90), new Vector3(0.22f, 0.6f, 0.22f), new Color(0.85f, 0.9f, 0.92f)); // Barrel
        Part(t, PrimitiveType.Cylinder, new Vector3(-0.2f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.18f, 0.35f, 0.18f), new Color(0.55f, 0.75f, 0.35f)); // Liquid
        Part(t, PrimitiveType.Cylinder, new Vector3(-0.8f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.06f, 0.25f, 0.06f), Gray);   // Plunger rod
        Part(t, PrimitiveType.Cylinder, new Vector3(-1.06f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.26f, 0.02f, 0.26f), Gray);  // Thumb rest
        Part(t, PrimitiveType.Cube, new Vector3(-0.62f, 0, 0), Vector3.zero, new Vector3(0.04f, 0.4f, 0.12f), Gray);                 // Finger flange
        Part(t, PrimitiveType.Cylinder, new Vector3(0.95f, 0, 0), new Vector3(0, 0, 90), new Vector3(0.015f, 0.35f, 0.015f), Steel); // Needle
        t.localRotation = Quaternion.Euler(0, 0, 20);
    }

    static void Sachet(Transform t)
    {
        Part(t, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.7f, 1.1f, 0.06f), Porcelain);
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.1f, -0.035f), Vector3.zero, new Vector3(0.5f, 0.3f, 0.01f), Red); // Printed label
        // Torn top edge.
        for (int i = 0; i < 6; i++)
            Part(t, PrimitiveType.Cube, new Vector3(-0.3f + i * 0.12f, 0.56f + (i % 2) * 0.03f, 0), new Vector3(0, 0, i % 2 == 0 ? 25 : -25), new Vector3(0.1f, 0.06f, 0.06f), Porcelain);
        Part(t, PrimitiveType.Sphere, new Vector3(0.1f, -0.75f, 0), Vector3.zero, new Vector3(0.25f, 0.06f, 0.2f), Paper); // Spilled grains
    }

    static void Dumpster(Transform t)
    {
        Part(t, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(2f, 1.1f, 1.1f), Green);
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.62f, -0.25f), new Vector3(-30, 0, 0), new Vector3(2.05f, 0.06f, 1.1f), new Color(0.1f, 0.35f, 0.2f)); // Open lid
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.45f, 0.1f), Vector3.zero, new Vector3(1.2f, 0.25f, 0.5f), Porcelain); // Lab coat inside
        for (int i = 0; i < 4; i++)
            Part(t, PrimitiveType.Cylinder, new Vector3(i < 2 ? -0.8f : 0.8f, -0.62f, i % 2 == 0 ? -0.4f : 0.4f), new Vector3(90, 0, 0), new Vector3(0.18f, 0.04f, 0.18f), DarkMetal);
        t.localRotation = Quaternion.Euler(-15, 25, 0);
    }

    static void WheelTracks(Transform t)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0, -0.1f, 0), Vector3.zero, new Vector3(2.2f, 0.1f, 1.6f), Floor);
        for (int side = -1; side <= 1; side += 2)
            Part(t, PrimitiveType.Cube, new Vector3(0, -0.04f, side * 0.35f), new Vector3(0, side * 4, 0), new Vector3(2.1f, 0.02f, 0.05f), Ink);
        t.localRotation = Quaternion.Euler(-35, 0, 0);
    }

    static void Trolley(Transform t)
    {
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.4f, 0), Vector3.zero, new Vector3(2.2f, 0.08f, 0.8f), Steel); // Bed
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.47f, 0), Vector3.zero, new Vector3(2f, 0.06f, 0.7f), Porcelain); // Sheet
        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -0.95f : 0.95f, z = i % 2 == 0 ? -0.32f : 0.32f;
            Part(t, PrimitiveType.Cylinder, new Vector3(x, 0, z), Vector3.zero, new Vector3(0.05f, 0.4f, 0.05f), Gray); // Legs
            Part(t, PrimitiveType.Sphere, new Vector3(x, -0.42f, z), Vector3.zero, Vector3.one * 0.12f, DarkMetal);      // Wheels
        }
        t.localRotation = Quaternion.Euler(-10, 30, 0);
    }

    static void EvidenceBag(Transform t)
    {
        Part(t, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(1.2f, 1.6f, 0.08f), new Color(0.85f, 0.88f, 0.9f));
        Part(t, PrimitiveType.Cube, new Vector3(0, 0.55f, -0.05f), Vector3.zero, new Vector3(1.1f, 0.3f, 0.01f), Red);
    }

    // ---------- Helpers ----------

    static void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 euler, Vector3 scale, Color color)
    {
        var part = GameObject.CreatePrimitive(type);
        Object.Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(euler);
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(color);
    }

    public static Material MaterialFor(Color color)
    {
        if (materials.TryGetValue(color, out var material) && material != null) return material;
        if (shader == null) shader = Shader.Find("CaseZero/Evidence3D");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        material = new Material(shader) { name = "Pista 3D" };
        material.SetColor("_BaseColor", color);
        materials[color] = material;
        return material;
    }
}

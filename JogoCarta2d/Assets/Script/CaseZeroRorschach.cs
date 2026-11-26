// Rorschach test content. Each interviewee has a drawing with hidden details (found with the
// magnifier) and answers to the three fixed questions. A psychopath's answers contradict the
// drawing; once the player compares them, the room reveals the hint.
public sealed class RorschachDetail
{
    public string text;
    public float x, y;  // Normalized position on the paper (0..1, origin bottom-left).
    public int glyph;   // 0 line, 1 ring, 2 crossed box, 3 hatching, 4 lying figure.
}

public sealed class RorschachProfile
{
    public bool psychopath;
    public RorschachDetail[] details;
    public string[] answers;  // One per question in CaseZeroRorschach.Questions.
    public string hint;       // Shown only for psychopaths.
    public int seed;
}

public static class CaseZeroRorschach
{
    public static readonly string[] Questions = {
        "What do you see in the drawing?",
        "What is this shape in the middle?",
        "How did you feel while drawing?"
    };

    static RorschachDetail D(string text, float x, float y, int glyph) => new RorschachDetail { text = text, x = x, y = y, glyph = glyph };

    public static RorschachProfile Get(string caseId, int index)
    {
        switch (caseId + ":" + index)
        {
            // Case 1 • the man behind the counter (robbed the bar and shot the only witness).
            case "ultima-testemunha:0": return new RorschachProfile {
                psychopath = true, seed = 11,
                details = new[] {
                    D("A door forced open, the lock broken", 0.36f, 0.58f, 2),
                    D("A pair of wide eyes staring back", 0.62f, 0.7f, 1),
                    D("A figure lying at the end of a counter", 0.5f, 0.3f, 4) },
                answers = new[] {
                    "A quiet bar at closing time. Glasses on a shelf. Nothing happening.",
                    "A bottle. I was pouring drinks, that's all.",
                    "Calm. Just another shift." },
                hint = "HINT • He describes a quiet shift, but the drawing shows a door forced open, a pair of eyes watching him and a body by the counter. Check the storeroom, and where the woman was standing."
            };
            // Case 2 • the medical examiner (killer), the security guard (paid accomplice), the caretaker (found the body).
            case "apartamento-302:0": return new RorschachProfile {
                psychopath = true, seed = 23,
                details = new[] {
                    D("A long needle-shaped stroke", 0.36f, 0.62f, 0),
                    D("A figure lying on a trolley", 0.5f, 0.3f, 4),
                    D("A form with a signature crossed over it", 0.64f, 0.7f, 2) },
                answers = new[] {
                    "Two butterflies over a field. Something peaceful.",
                    "A flower stem. Nothing more.",
                    "Relaxed, actually. I enjoyed it." },
                hint = "HINT • The examiner saw butterflies and flowers, but the drawing shows a needle, a body on a trolley and a signed form. Compare the second autopsy with the syringe and its fingerprints."
            };
            case "apartamento-302:1": return new RorschachProfile {
                seed = 31,
                details = new[] {
                    D("A camera with an X over it", 0.62f, 0.72f, 2),
                    D("Coins piled on a table", 0.38f, 0.4f, 1),
                    D("A back door left ajar", 0.58f, 0.34f, 2) },
                answers = new[] {
                    "A camera... switched off. I don't know why I drew that.",
                    "Money. Everyone needs money, detective.",
                    "Nervous. I keep thinking about that night shift." }
            };
            case "apartamento-302:2": return new RorschachProfile {
                seed = 37,
                details = new[] {
                    D("A dumpster with its lid open", 0.5f, 0.42f, 2),
                    D("A broom leaning on a wall", 0.34f, 0.6f, 0),
                    D("Two thin wheel tracks", 0.62f, 0.3f, 0) },
                answers = new[] {
                    "The bins on my street. And the tracks I saw that morning.",
                    "The dumpster. That's where I found the poor kid.",
                    "Shaken. I still see it when I take the rubbish out." }
            };
            // Case 3 • the police officer (poisoner), the baker, the barista, the nurse (witness).
            case "arquivo-vertice:0": return new RorschachProfile {
                psychopath = true, seed = 53,
                details = new[] {
                    D("A coffee cup with a skull-shaped stain", 0.42f, 0.6f, 1),
                    D("A torn little sachet", 0.64f, 0.66f, 2),
                    D("A car with a dented bumper", 0.5f, 0.3f, 3) },
                answers = new[] {
                    "A badge. Order. Somebody keeping the street safe.",
                    "A sun coming up over the city.",
                    "Proud. I always do my duty." },
                hint = "HINT • The officer talks about badges and sunrises, but the drawing shows a poisoned cup, a torn sachet and a dented car. Compare the evidence-room log and the fingerprints on the sachet."
            };
            case "arquivo-vertice:1": return new RorschachProfile {
                seed = 59,
                details = new[] {
                    D("A loaf of bread", 0.42f, 0.4f, 1),
                    D("Steam rising from an oven", 0.6f, 0.68f, 3),
                    D("A clock showing early morning", 0.34f, 0.72f, 1) },
                answers = new[] {
                    "An oven and bread. My whole life is the bakery.",
                    "A loaf. Round, like the ones I make every morning.",
                    "Tired. I had my back to the tables when it happened." }
            };
            case "arquivo-vertice:2": return new RorschachProfile {
                seed = 61,
                details = new[] {
                    D("A coffee machine", 0.4f, 0.58f, 2),
                    D("Two cups side by side", 0.62f, 0.4f, 1),
                    D("An order slip pinned to a board", 0.34f, 0.32f, 2) },
                answers = new[] {
                    "The machine and the cups. Every morning is the same.",
                    "The order slip. I wrote his coffee down like always.",
                    "Sad. He was a regular." }
            };
            case "arquivo-vertice:3": return new RorschachProfile {
                seed = 67,
                details = new[] {
                    D("A person collapsing", 0.5f, 0.34f, 4),
                    D("Hands reaching out to help", 0.36f, 0.6f, 3),
                    D("A fallen cup", 0.64f, 0.56f, 1) },
                answers = new[] {
                    "Someone falling, and hands trying to catch them.",
                    "The cup. It fell when he collapsed.",
                    "Helpless. I tried to help and it was not enough." }
            };
            default: return new RorschachProfile {
                seed = 7,
                details = new[] { D("An irregular ink stain", 0.5f, 0.5f, 3) },
                answers = new[] { "Just a stain.", "I do not know.", "Nothing special." }
            };
        }
    }
}

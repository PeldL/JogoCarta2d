// Everything a case needs lives in its CaseZeroCase entry: clues and where they are,
// interviews, forensic analyses, the radio briefing, the deduction and the victim.
// The game code reads this data and has no per-case special cases.

/// <summary>One line of an interview: what the person says and the questions it offers.</summary>
public sealed class CaseZeroTalk
{
    public string id, text;
    public CaseZeroAsk[] asks;
}

/// <summary>A question in an interview. <c>next</c> is the node it leads to ("" ends the
/// conversation, "confronto" is the challenge); <c>requires</c> is an optional clue key.</summary>
public sealed class CaseZeroAsk
{
    public string text, next, requires;
}

public sealed class CaseZeroCase
{
    public string id, title, location, introduction, ending;
    public string[] locations, evidenceKeys, evidenceNames, evidenceDescriptions, evidenceLocations;
    public int[] evidenceUnlockStages;
    public string testimonyEvidenceName, testimonyEvidenceDescription;
    public string[] suspects, objects, places;
    public int culprit, weaponSolution, placeSolution, analysisCount;
    // Interview whose challenge produces the testimony (may differ from the culprit
    // when the deduction answer names more than one person).
    public int confrontInterview;
    public string objectQuestion, placeQuestion;
    public string[] interviewNames;
    // interviews[i] is the conversation with interviewNames[i]; its first node is "start".
    public CaseZeroTalk[][] interviews;
    public string[][] analysisPrerequisites;
    public string[] analysisMessages;
    // Clues needed to challenge the culprit's account in Interviews.
    public string[] confrontRequirements;
    // Clues needed before Deduction opens; other clues are optional context.
    public string[] keyEvidence;
    // The challenge also needs a recorded forensic report (first case: ballistics).
    public bool confrontNeedsReport;
    // Deduction shows suspects in this order (slot -> suspect index), so the
    // culprit is not always the first option. Suspect indices stay tied to
    // interviews and portraits.
    public int[] suspectOrder;
    // Radio conversation before the case. The tutorial case also asks the player to
    // tune the radio and shows the one-time control hints.
    public string[] radioLines;
    public bool tutorial;
    // Victim portrait (a name in the art library) and the caption under it.
    public string victimArt, victimLabel;

    public int SuspectAtSlot(int slot)
    {
        if (suspectOrder == null || suspectOrder.Length != suspects.Length) return slot;
        return slot >= 0 && slot < suspectOrder.Length ? suspectOrder[slot] : -1;
    }

    public int SlotOfSuspect(int suspect)
    {
        if (suspect < 0) return -1;
        if (suspectOrder == null || suspectOrder.Length != suspects.Length) return suspect;
        return System.Array.IndexOf(suspectOrder, suspect);
    }

    public string EvidenceName(string key)
    {
        int index = EvidenceIndex(key);
        return index >= 0 ? evidenceNames[index] : key == "depoimento" ? testimonyEvidenceName : key;
    }

    public int UnlockStage(string key)
    {
        int index = EvidenceIndex(key);
        return index >= 0 && evidenceUnlockStages != null && index < evidenceUnlockStages.Length ? evidenceUnlockStages[index] : 0;
    }

    /// <summary>Where forensic results are collected, or null when they only arrive by phone.</summary>
    public string ReportsLocation
    {
        get
        {
            for (int i = 0; i < evidenceKeys.Length; i++)
                if (UnlockStage(evidenceKeys[i]) > 0) return evidenceLocations[i];
            return null;
        }
    }

    public int EvidenceIndex(string key)
    {
        for (int i = 0; i < evidenceKeys.Length; i++)
            if (evidenceKeys[i] == key) return i;
        return -1;
    }

    public string[] EvidenceAt(string locationName, int availableStage)
    {
        var result = new System.Collections.Generic.List<string>();
        for (int i = 0; i < evidenceKeys.Length; i++)
        {
            if (evidenceLocations[i] != locationName) continue;
            if (availableStage >= UnlockStage(evidenceKeys[i])) result.Add(evidenceKeys[i]);
        }
        return result.ToArray();
    }
}

public static class CaseZeroCases
{
    static CaseZeroTalk Talk(string id, string text, params CaseZeroAsk[] asks) => new CaseZeroTalk { id = id, text = text, asks = asks };
    static CaseZeroAsk Ask(string text, string next, string requires = null) => new CaseZeroAsk { text = text, next = next, requires = requires };
    static CaseZeroAsk BackToQuestions => Ask("Back to questions", "start");
    static CaseZeroAsk EndConversation => Ask("End conversation", "");
    static CaseZeroAsk EndInterview => Ask("End interview", "");

    public static readonly CaseZeroCase[] All = {
        // Case 1 (tutorial): a robber shoots a woman who walks in on him at the bar.
        new CaseZeroCase {
            id = "ultima-testemunha", title = "The Last Witness", location = "Bar", tutorial = true,
            locations = new[] { "Bar" }, culprit = 0, confrontInterview = 0, weaponSolution = 1, placeSolution = 2, analysisCount = 1,
            suspectOrder = new[] { 1, 2, 0 },
            victimArt = "Vítima 1 - Mulher no bar", victimLabel = "Woman from the bar",
            introduction = "A woman entered the bar and was shot dead. Gather evidence, question the man behind the counter, and establish the culprit, weapon, and crime scene.",
            evidenceKeys = new[] { "registro", "pistola", "capsula", "sangue" },
            evidenceNames = new[] { "Employee records", "Pistol under the counter", "Cartridge case on the floor", "Crime scene traces" },
            evidenceDescriptions = new[] {
                "The schedule lists one employee on shift, who clocked out twenty minutes before the shot. The man found behind the counter is not on any list. The storeroom door was left open.",
                "A pistol was found hidden under the counter. Preserve it for ballistic comparison.",
                "A cartridge case was found in the service area behind the counter. Nearby, a customer's receipt was paid minutes before the shot; that customer has not been identified.",
                "Bloodstains in the service area behind the counter. A broken bottle and a kitchen knife lie nearby, but neither has any blood on it."
            },
            evidenceLocations = new[] { "Bar", "Bar", "Bar", "Bar" },
            evidenceUnlockStages = new[] { 0, 0, 0, 0 },
            testimonyEvidenceName = "Challenged testimony",
            testimonyEvidenceDescription = "Confronted with the employee records and the ballistic report, the man admits he does not work at the bar and came in to empty the till. He says the woman walked in and he panicked, but refuses to say more.",
            suspects = new[] { "Man behind the counter", "Bar employee", "Unidentified customer" },
            objects = new[] { "Kitchen knife", "Pistol", "Broken bottle" },
            places = new[] { "Storeroom", "Street outside", "Behind the counter" },
            objectQuestion = "WHICH WEAPON?", placeQuestion = "WHERE?",
            interviewNames = new[] { "Man behind the counter" },
            interviews = new[] {
                new[] {
                    Talk("start", "I was behind the counter when that woman came in. I have nothing else to say.",
                        Ask("What were you doing at the bar?", "trabalho"),
                        Ask("Did you know the victim?", "vitima"),
                        Ask("Challenge with the records and the ballistic report", "confronto", "registro")),
                    Talk("trabalho", "I... was helping with the service. I do not remember who called me.", BackToQuestions),
                    Talk("vitima", "I had never seen her before. She came in and ordered a drink. It all happened so fast.", BackToQuestions),
                    Talk("confronto", "...Fine. I don't work here. I came in through the storeroom to empty the till. Then she walked in and saw me. I panicked. That's all I'm saying.", EndInterview)
                }
            },
            analysisPrerequisites = new[] { new[] { "pistola", "capsula" } },
            analysisMessages = new[] {
                "The ballistic comparison is a match: the cartridge case recovered behind the counter was fired by the pistol hidden under it. Use this report and the employee records to challenge the man behind the counter."
            },
            confrontRequirements = new[] { "pistola", "registro" },
            confrontNeedsReport = true,
            keyEvidence = new[] { "registro", "pistola", "capsula", "sangue" },
            radioLines = new[] {
                "DISPATCH\nDetective, do you copy? A woman was shot dead in a bar. We need you at the scene.",
                "DETECTIVE\nOn my way. Where do I start?",
                "DISPATCH\nExamine the marked objects and send anything useful to forensics through your phone. Then challenge any account the evidence contradicts. Tab opens your case file."
            },
            ending = "The man behind the counter was a robber. He killed the woman with the pistol in the bar to stop her from reporting him."
        },
        // Case 2: the medical examiner, helped by the security guard, kills a lab assistant
        // who was about to expose his falsified autopsies. Injection in the laboratory;
        // the body is dumped in a street bin and the examiner signs it off as an overdose.
        new CaseZeroCase {
            id = "apartamento-302", title = "The Second Opinion", location = "Rua",
            locations = new[] { "Rua", "Laboratorio", "Delegacia" },
            culprit = 3, confrontInterview = 1, weaponSolution = 1, placeSolution = 2, analysisCount = 2,
            suspectOrder = new[] { 2, 3, 0, 1 },
            victimArt = "Vítima 2", victimLabel = "Forensics lab assistant",
            introduction = "A lab assistant from the forensics laboratory was found dead inside a street dumpster at dawn. The official autopsy, signed by the laboratory's own medical examiner, says overdose. The family insists the victim never used drugs.",
            evidenceKeys = new[] {
                "c2_rua_lixeira", "c2_rua_cracha", "c2_rua_marcas",
                "c2_lab_seringa", "c2_lab_estoque", "c2_lab_camera", "c2_lab_maca",
                "c2_police_autopsia", "c2_police_rascunho", "c2_police_digitais", "c2_police_transferencia"
            },
            evidenceNames = new[] {
                "Body in the dumpster", "Laboratory access badge", "Thin wheel tracks",
                "Syringe in the sharps bin", "Controlled drug log", "Camera control panel", "Body trolley",
                "Second autopsy", "Victim's draft complaint", "Fingerprints on the syringe", "Bank transfer"
            },
            evidenceDescriptions = new[] {
                "The victim lies in a street dumpster, still wearing a lab coat. No blood, no wounds, no sign of a struggle. The wallet and phone were not taken. This was no robbery.",
                "The victim's access badge, clipped to the coat. It was last used to enter the forensics laboratory at 22:40, the night before the body was found.",
                "Two thin, parallel wheel tracks run from the curb to the dumpster, where a white van was parked around midnight. Something heavy was rolled here.",
                "A recently used syringe in the laboratory's sharps bin, its label torn off. A drop of clear liquid remains inside. It can be sent to an independent lab.",
                "Two vials of a powerful sedative are missing from the controlled stock. The weekly check was signed as complete by the medical examiner the morning after the death.",
                "The laboratory cameras were switched off manually at 22:30 and back on at 01:10, using the security guard's account.",
                "The body trolley from the autopsy room has thin wheels caked in street mud, although it should never leave the building. Its tread matches the tracks by the dumpster.",
                "An independent examiner found a fresh injection mark on the back of the victim's neck and a lethal dose of the sedative missing from the laboratory. No sign of drug use: the official overdose report is false.",
                "Recovered from the victim's phone: an unsent complaint to the police, describing autopsy reports the medical examiner falsified for money. The victim planned to send it the next morning.",
                "Fingerprints on the syringe plunger belong to the medical examiner. The liquid inside is the same sedative found in the victim's body.",
                "Bank records show a cash transfer from the medical examiner to the security guard the morning after the murder."
            },
            evidenceLocations = new[] {
                "Rua", "Rua", "Rua",
                "Laboratorio", "Laboratorio", "Laboratorio", "Laboratorio",
                "Delegacia", "Delegacia", "Delegacia", "Delegacia"
            },
            evidenceUnlockStages = new[] { 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2 },
            testimonyEvidenceName = "Security guard's confession",
            testimonyEvidenceDescription = "Confronted with the camera log, the trolley and the second autopsy, the security guard admits the medical examiner paid him to switch off the cameras and open the back door, and that he helped wheel the body to the dumpster. He says the examiner gave the injection.",
            suspects = new[] { "Medical examiner", "Security guard", "Street caretaker", "Examiner and guard together" },
            objects = new[] { "Drug overdose", "Lethal injection", "Suffocation" },
            places = new[] { "Street dumpster", "Police station", "Forensics laboratory" },
            objectQuestion = "HOW WAS THE VICTIM KILLED?", placeQuestion = "WHERE DID THE MURDER HAPPEN?",
            interviewNames = new[] { "Medical examiner", "Security guard", "Caretaker • found the body" },
            interviews = new[] {
                new[] {
                    Talk("start", "My report is clear: an overdose. The assistant had problems, everyone at the laboratory knew it. I don't see what there is to investigate.",
                        Ask("Ask about the missing sedative", "stock", "c2_lab_estoque"),
                        Ask("Ask about the draft complaint", "complaint", "c2_police_rascunho"),
                        EndConversation),
                    Talk("stock", "Stock goes missing all the time in a busy laboratory. I sign the weekly check; I can't watch every shelf myself.", BackToQuestions),
                    Talk("complaint", "The assistant was confused, seeing conspiracies everywhere. Nobody would have believed a word of it. If you have more questions, talk to my lawyer.", BackToQuestions)
                },
                new[] {
                    Talk("start", "I was on duty all night. The cameras crashed around half past ten. That old system does it all the time.",
                        Ask("Ask about the camera shutdown", "camera", "c2_lab_camera"),
                        Ask("Ask about the body trolley", "trolley", "c2_lab_maca"),
                        Ask("Confront him with the evidence", "confronto")),
                    Talk("camera", "Crashed, switched off, whatever the log says. I'm a guard, not a technician. Anyone could have used my account.", BackToQuestions),
                    Talk("trolley", "Nobody moves a body at night without the examiner's orders. Ask him, not me.", BackToQuestions),
                    Talk("confronto", "...Okay. The examiner paid me to switch off the cameras and open the back door. When I came back, the assistant was on the trolley, in a bag. He told me to help him get it to the van. I swear I never touched that needle.", EndInterview)
                },
                new[] {
                    Talk("start", "I found the body when I came to empty the bins at six. I called the police straight away.",
                        Ask("Ask about the wheel tracks", "tracks", "c2_rua_marcas"),
                        Ask("Ask what they saw that night", "van"),
                        EndConversation),
                    Talk("tracks", "Those thin tracks weren't there yesterday. Something heavy was rolled from the curb to the bin.", BackToQuestions),
                    Talk("van", "A white van, the kind they use to move bodies. It stopped by the bins around midnight and left with its lights off.", BackToQuestions)
                }
            },
            analysisPrerequisites = new[] {
                new[] { "c2_rua_lixeira", "c2_lab_seringa" },
                new[] { "c2_lab_estoque", "depoimento" }
            },
            analysisMessages = new[] {
                "The independent autopsy is done: a fresh injection mark on the neck and a lethal dose of the laboratory's sedative. There is no sign of drug use; the official overdose report is false. We also unlocked the victim's phone. The second autopsy and the victim's draft complaint are ready at the police station.",
                "The fingerprints on the syringe belong to the medical examiner, and the liquid matches the sedative in the body. The guard's bank records show a payment from the examiner the morning after. Both reports are ready at the police station."
            },
            confrontRequirements = new[] { "c2_lab_camera", "c2_lab_maca", "c2_police_autopsia" },
            keyEvidence = new[] { "c2_rua_lixeira", "c2_lab_seringa", "c2_lab_estoque", "c2_lab_camera", "c2_lab_maca", "c2_police_autopsia", "c2_police_digitais", "c2_police_transferencia" },
            radioLines = new[] {
                "DISPATCH\nDetective, a body was found in a street dumpster at dawn. It's an assistant from the forensics laboratory.",
                "DETECTIVE\nCause of death?",
                "DISPATCH\nThe laboratory's own medical examiner already signed the autopsy: overdose. The family swears the victim never used drugs.",
                "DETECTIVE\nThen I want a second opinion. I'll start at the dumpster and then check the laboratory.",
                "DISPATCH\nSend anything suspicious to an independent lab through your phone. The results will be waiting at the police station."
            },
            ending = "The medical examiner had been falsifying autopsies for money, and the lab assistant found out and was about to report it. He kept the assistant late at the laboratory while the security guard, paid to help, switched off the cameras and opened the back door. The examiner killed the assistant with a lethal injection from the laboratory's own stock. Together they wheeled the body out on the autopsy trolley and dumped it in a street bin, and the examiner signed an autopsy calling it an overdose. The second autopsy, the fingerprints and the payment exposed them both."
        },
        // Case 3: a regular customer is poisoned at the bakery by the police officer
        // they had been blackmailing over a hit-and-run. The officer then takes charge
        // of the scene and calls it a heart attack.
        new CaseZeroCase {
            id = "arquivo-vertice", title = "The Last Coffee", location = "Padaria",
            locations = new[] { "Padaria", "Estacionamento", "Delegacia" },
            culprit = 0, confrontInterview = 0, weaponSolution = 2, placeSolution = 0, analysisCount = 2,
            suspectOrder = new[] { 1, 0, 2 },
            victimArt = "Vítima 3", victimLabel = "Bakery regular",
            introduction = "A regular customer collapsed and died at the bakery during breakfast. The first officer on the scene called it a heart attack, but a nurse who tried to help says the symptoms were wrong. The victim's car is still in the parking lot behind the bakery.",
            evidenceKeys = new[] {
                "c3_padaria_xicara", "c3_padaria_camera", "c3_padaria_sache", "c3_padaria_comanda",
                "c3_est_envelope", "c3_est_caderno", "c3_est_jornal",
                "c3_police_tox", "c3_police_livro", "c3_police_digitais", "c3_police_viatura"
            },
            evidenceNames = new[] {
                "Victim's coffee cup", "Bakery security footage", "Torn sugar sachet", "Order slip",
                "Envelope in the victim's car", "Payment notebook", "Newspaper clipping",
                "Toxicology report", "Evidence-room log", "Fingerprints on the sachet", "Patrol car repair record"
            },
            evidenceDescriptions = new[] {
                "The victim's cup, broken by the window table, with a little coffee left in the largest piece. It can be sent for toxicology.",
                "While the baker turns to the oven, the officer at the next table leans over the victim's cup for a few seconds. Moments later the victim drinks and collapses.",
                "A paper sugar sachet torn open under the window table, with a fine grey powder inside. The bakery does not use paper sachets, only sugar bowls.",
                "The victim's usual order, paid every morning at the same time: black coffee, no sugar.",
                "Photos of a patrol car with a dented front bumper, dated the night of last year's hit-and-run in this parking lot. On the back: \"He pays, or this goes to the papers.\"",
                "A notebook listing monthly cash payments over the last year. Every entry is marked with the same patrol car number.",
                "An article on last year's hit-and-run in the bakery parking lot: a cyclist was badly injured and the driver was never found.",
                "The coffee contained a lethal dose of an organophosphate pesticide. This was poisoning, not a heart attack.",
                "Two days before the death, the officer signed a bottle of seized pesticide out of the evidence room \"for destruction\". It was never destroyed and never returned.",
                "The officer's fingerprints are on the torn sachet, and the powder inside is the same pesticide found in the coffee.",
                "The officer's patrol car had its front bumper replaced, paid in cash, the day after last year's hit-and-run."
            },
            evidenceLocations = new[] {
                "Padaria", "Padaria", "Padaria", "Padaria",
                "Estacionamento", "Estacionamento", "Estacionamento",
                "Delegacia", "Delegacia", "Delegacia", "Delegacia"
            },
            evidenceUnlockStages = new[] { 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2 },
            testimonyEvidenceName = "Officer's admission",
            testimonyEvidenceDescription = "Confronted with the footage, the evidence-room log and his fingerprints on the sachet, the officer admits the victim had been blackmailing him for a year over the hit-and-run, and that he \"just wanted it to stop\".",
            suspects = new[] { "Police officer", "Baker", "Barista" },
            objects = new[] { "Heart attack", "Allergic reaction", "Pesticide poisoning" },
            places = new[] { "Bakery", "Parking lot", "Police station" },
            objectQuestion = "WHAT KILLED THE VICTIM?", placeQuestion = "WHERE WAS THE VICTIM POISONED?",
            interviewNames = new[] { "Police officer • first on the scene", "Baker", "Barista", "Nurse • witness" },
            interviews = new[] {
                new[] {
                    Talk("start", "I was having my usual coffee when the victim collapsed. I called it in and secured the scene myself. Looked like a heart attack to me.",
                        Ask("Ask about the footage", "footage", "c3_padaria_camera"),
                        Ask("Ask about the evidence-room log", "log", "c3_police_livro"),
                        Ask("Confront him with the evidence", "confronto")),
                    Talk("footage", "I leaned over to help with the tray. I was nowhere near that cup.", BackToQuestions),
                    Talk("log", "I signed that bottle out to send it for destruction. The paperwork must have gone astray.", BackToQuestions),
                    Talk("confronto", "...A year. A year of paying for one mistake on a rainy night. An envelope every month, and every month they wanted more. I just wanted it to stop. I'm done talking.", EndInterview)
                },
                new[] {
                    Talk("start", "I've served the victim the same black coffee every morning for years. I turned to the oven for a minute, and when I looked back they were on the floor.",
                        Ask("Ask about the sugar sachet", "sachet", "c3_padaria_sache"),
                        Ask("Ask who sat near the victim", "table"),
                        EndConversation),
                    Talk("sachet", "That isn't ours. We only have sugar bowls. And the victim never took sugar anyway.", BackToQuestions),
                    Talk("table", "Only the officer. He comes in every morning and always sits at the table next to the victim's.", BackToQuestions)
                },
                new[] {
                    Talk("start", "I was making orders in the back. I came out when I heard the cup break.",
                        Ask("Ask about the victim", "victim"),
                        Ask("Ask about the parking lot", "parking", "c3_est_caderno"),
                        EndConversation),
                    Talk("victim", "The victim always seemed nervous around the officer, but kept sitting at the next table. Strange, right?", BackToQuestions),
                    Talk("parking", "The victim parked behind the bakery every day and stayed a while in the car, writing in a little notebook.", BackToQuestions)
                },
                new[] {
                    Talk("start", "I was in line and tried to help. Tiny pupils, sweating, struggling to breathe... that was no heart attack.",
                        Ask("Ask about the symptoms", "symptoms"),
                        Ask("Ask about the officer", "officer", "c3_padaria_camera"),
                        EndConversation),
                    Talk("symptoms", "Those signs look like pesticide poisoning. I told the officer, but he said to leave it to the professionals.", BackToQuestions),
                    Talk("officer", "He picked up the broken cup himself and wanted to throw it away. I asked him to leave it where it was.", BackToQuestions)
                }
            },
            analysisPrerequisites = new[] {
                new[] { "c3_padaria_xicara" },
                new[] { "c3_padaria_sache" }
            },
            analysisMessages = new[] {
                "Toxicology found a lethal dose of an organophosphate pesticide in the victim's coffee. This was poisoning, not a heart attack. The toxicology report and the evidence-room log for seized pesticides are ready at the police station.",
                "The powder in the sugar sachet is the same pesticide, and the only fingerprints on the paper belong to the officer who secured the scene. The fingerprint report and the officer's patrol car records are ready at the police station."
            },
            confrontRequirements = new[] { "c3_padaria_camera", "c3_police_livro", "c3_police_digitais" },
            keyEvidence = new[] { "c3_padaria_xicara", "c3_padaria_camera", "c3_padaria_sache", "c3_est_envelope", "c3_police_tox", "c3_police_livro", "c3_police_digitais" },
            radioLines = new[] {
                "DISPATCH\nDetective, a customer collapsed and died at the bakery during breakfast. The officer on the scene says it was a heart attack.",
                "DETECTIVE\nBut you're calling me anyway.",
                "DISPATCH\nA nurse who tried to help says the symptoms looked like poisoning. The victim's car is still in the parking lot behind the bakery.",
                "DETECTIVE\nUnderstood. I'll start at the bakery. Keep that cup away from everyone."
            },
            ending = "The victim had discovered that the officer caused last year's hit-and-run and had been blackmailing him ever since. The officer signed a seized pesticide out of the evidence room, tipped it from a sugar sachet into the victim's coffee at the bakery while the baker's back was turned, then took charge of the scene and called it a heart attack. The footage, the evidence-room log and his fingerprints on the sachet proved otherwise."
        }
    };

    public static int Index(string id) => System.Array.FindIndex(All, c => c.id == id);
    public static CaseZeroCase Get(string id) => All[System.Math.Max(0, Index(id))];
}

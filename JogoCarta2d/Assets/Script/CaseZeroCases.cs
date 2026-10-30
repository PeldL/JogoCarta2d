// Case content is separate from scene objects. Evidence keys are unique to a case;
// locations describe where each clue can be examined and when it becomes available.
public sealed class CaseZeroCase
{
    public string id, title, location, introduction, ending;
    public string[] locations, evidenceKeys, evidenceNames, evidenceDescriptions, evidenceLocations;
    public int[] evidenceUnlockStages;
    public string testimonyEvidenceName, testimonyEvidenceDescription;
    public string[] suspects, objects, places;
    public int culprit, weaponSolution, placeSolution, analysisCount;
    public string objectQuestion, placeQuestion;
    public string[] interviewNames, statements, explanations;
    public string[][] analysisPrerequisites;
    public string[] analysisMessages;

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
            int unlock = evidenceUnlockStages != null && i < evidenceUnlockStages.Length ? evidenceUnlockStages[i] : 0;
            if (availableStage >= unlock) result.Add(evidenceKeys[i]);
        }
        return result.ToArray();
    }
}

public static class CaseZeroCases
{
    public static readonly CaseZeroCase[] All = {
        new CaseZeroCase {
            id = "ultima-testemunha", title = "The Last Witness", location = "Bar",
            locations = new[] { "Bar" }, culprit = 0, weaponSolution = 0, placeSolution = 0,
            introduction = "A woman entered the bar and was shot dead. Gather evidence, question the man behind the counter, and establish the culprit, weapon, and crime scene.",
            evidenceKeys = new[] { "registro", "pistola", "capsula", "sangue" },
            evidenceNames = new[] { "Employee records", "Pistol under the counter", "Cartridge case on the floor", "Crime scene traces" },
            evidenceDescriptions = new[] {
                "The schedule identifies the actual employee: he left before the crime. The man seen behind the counter does not work here.",
                "A pistol was found hidden under the counter. Preserve it for ballistic comparison.",
                "A cartridge case was found near the body in the bar service area. It indicates a shot was fired here.",
                "Bloodstains by the counter. Their position is consistent with the account of the victim's arrival."
            },
            evidenceLocations = new[] { "Bar", "Bar", "Bar", "Bar" },
            evidenceUnlockStages = new[] { 0, 0, 0, 0 },
            testimonyEvidenceName = "Challenged testimony",
            testimonyEvidenceDescription = "Confronted with the schedule and the pistol, the man admits he was robbing the bar. The victim entered during the robbery.",
            suspects = new[] { "Man behind the counter", "Bar employee", "Unknown customer" },
            objects = new[] { "Pistol", "Knife", "Poison" },
            places = new[] { "Bar", "Street", "Victim's home" },
            objectQuestion = "WHICH WEAPON?", placeQuestion = "WHERE?",
            interviewNames = new[] { "Man behind the counter" },
            statements = new[] { "I was behind the counter when that woman came in. I have nothing else to say." },
            explanations = new[] { "All right. I was not an employee. I was robbing the bar. She came in, saw my face... I shot her because I was afraid she would report me." },
            ending = "The man behind the counter was a robber. He killed the woman with the pistol in the bar to stop her from reporting him."
        },
        new CaseZeroCase {
            id = "apartamento-302", title = "Blind Spot", location = "Apartamento",
            locations = new[] { "Apartamento", "Bar", "Delegacia" },
            culprit = 0, weaponSolution = 0, placeSolution = 0, analysisCount = 2,
            introduction = "A person was found dead in their apartment. There are no signs of forced entry, and the building cameras were offline for part of the night. The body may not have been found where the murder happened.",
            evidenceKeys = new[] {
                "c2_apt_tapete", "c2_apt_agenda", "c2_apt_chaveiro",
                "c2_bar_sangue", "c2_bar_botao", "c2_bar_terminal",
                "c2_police_laudo", "c2_police_uniforme", "c2_police_faca"
            },
            evidenceNames = new[] {
                "Rug beneath the body", "Victim's appointment book", "Caretaker's key ring",
                "Blood beneath the bench", "Security uniform button", "Camera terminal",
                "Medical examiner's report", "Seized uniform", "Knife from the security locker"
            },
            evidenceDescriptions = new[] {
                "There is surprisingly little blood beneath the body despite the severity of the wounds. The scene may have been moved after the attack.",
                "The victim noted a meeting at the closed bar after closing time. The appointment connects the victim to a second scene.",
                "A caretaker's key ring was left behind during an earlier repair. It explains access to the apartment, but does not place its owner there that night.",
                "Blood beneath a bench matches the victim. The pattern is consistent with a violent attack in the closed bar.",
                "A dark button bears the logo of the security company. It was found beside the bloodstain and may have come off during a struggle.",
                "The terminal records a manual camera shutdown under the security guard's account, contradicting the claim that the system simply failed.",
                "The medical examiner confirms 28 stab wounds and finds signs that the body was moved after death. The estimated time matches the camera outage.",
                "One button is missing from the security guard's uniform. Its design and remaining threads match the button from the bar.",
                "A knife recovered from the security guard's locker carries the victim's blood on its blade."
            },
            evidenceLocations = new[] {
                "Apartamento", "Apartamento", "Apartamento", "Bar", "Bar", "Bar",
                "Delegacia", "Delegacia", "Delegacia"
            },
            evidenceUnlockStages = new[] { 0, 0, 0, 0, 0, 0, 1, 1, 2 },
            testimonyEvidenceName = "Security guard's revised statement",
            testimonyEvidenceDescription = "Confronted with the uniform button, the camera log, and the matching uniform, the security guard changes his story: he went to the bar, but claims the victim was alive when he left.",
            suspects = new[] { "Security guard", "Caretaker", "Baker" },
            objects = new[] { "Knife", "Pistol", "Poison" },
            places = new[] { "Closed bar", "Apartment", "Street" },
            objectQuestion = "WHAT WAS THE WEAPON?", placeQuestion = "WHERE DID THE ATTACK HAPPEN?",
            interviewNames = new[] { "Security guard", "Caretaker", "Baker", "Nurse • witness" },
            statements = new[] {
                "The cameras had a problem. I stayed in the surveillance room all night.",
                "I went in earlier to make a repair. I have a spare key, but I did not enter that night.",
                "I had no reason to visit the apartment or the closed bar. I have nothing to do with the camera system.",
                "I live in the building. I noticed movement in the corridor after the victim was found, but I could not identify who was there."
            },
            explanations = new[] {
                "All right. I went to the bar, but the victim was alive when I left.",
                "The key ring was left during the earlier repair. I did not kill the victim or move the body.",
                "I did not go to the closed bar that night. The evidence from the scene and the security room should be checked against the guard's account.",
                "I saw someone moving through the corridor, but I could not identify them. I can confirm the body was in the apartment when it was discovered."
            },
            analysisPrerequisites = new[] {
                new[] { "c2_bar_sangue", "c2_bar_botao", "c2_bar_terminal" },
                new[] { "c2_bar_botao", "c2_bar_terminal", "depoimento" }
            },
            analysisMessages = new[] {
                "The blood beneath the bar bench matches the victim, confirming the attack happened there, not in the apartment. The button matches the security uniform, and the camera terminal records a manual shutdown under the guard's account. The medical examiner's report and uniform comparison are ready at the police station.",
                "The search of the security locker found a knife. The lab identified the victim's blood on its blade. The seized knife and final report are ready at the police station."
            },
            ending = "The cameras went blind, but the traces remained. The button and uniform placed the security guard in the closed bar, while the terminal exposed the manual shutdown. The knife linked him to the 28 wounds. The body had been moved to the apartment to mislead the investigation."
        },
        new CaseZeroCase {
            id = "arquivo-vertice", title = "The Last Break", location = "Bar",
            locations = new[] { "Bar", "Padaria", "Delegacia" },
            culprit = 0, weaponSolution = 0, placeSolution = 0, analysisCount = 2,
            introduction = "A person was found dead in the closed bar. A receipt and coffee-stained fabric suggest the body may have been brought from somewhere else. The cause of death is not yet known.",
            evidenceKeys = new[] {
                "c3_bar_recibo", "c3_bar_pano", "c3_bar_manutencao",
                "c3_padaria_xicara", "c3_padaria_camera", "c3_padaria_retalho",
                "c3_police_tox", "c3_police_uniforme", "c3_police_carta"
            },
            evidenceNames = new[] {
                "Receipt in the victim's pocket", "Cloth caught on the victim", "Bar maintenance log",
                "Cup behind the bakery counter", "Recovered bakery footage", "Fabric snagged in the cup cabinet",
                "Toxicology report", "Caretaker's uniform", "Dismissal letter"
            },
            evidenceDescriptions = new[] {
                "The receipt records a delivery received at the bakery that morning. It points to the victim's last known appointment.",
                "The cloth bears the bakery's name and coffee stains. It links the victim's clothing to a different location.",
                "The maintenance log shows that the caretaker had a key to the closed bar that day, providing a way to move the body.",
                "The victim's cup contains leftover coffee. It was found behind the counter beside a spill and can be submitted for toxicology.",
                "Recovered footage shows the victim drinking coffee and collapsing behind the counter. The image does not show who handled the cup.",
                "A piece of gray fabric with yellow stitching is caught on the cup-cabinet hinge. It can be compared with the caretaker's uniform.",
                "Toxicology identifies poison in the victim's cup. The sample from the coffee pot is clean, showing that only one drink was contaminated.",
                "The tear and yellow seam in the caretaker's uniform match the fabric caught in the cup cabinet.",
                "The victim blamed the caretaker for a workplace accident. On the back is a handwritten note: 'You knew it wasn't my fault.'"
            },
            evidenceLocations = new[] {
                "Bar", "Bar", "Bar", "Padaria", "Padaria", "Padaria",
                "Delegacia", "Delegacia", "Delegacia"
            },
            evidenceUnlockStages = new[] { 0, 0, 0, 0, 0, 0, 1, 2, 1 },
            testimonyEvidenceName = "Caretaker's changed statement",
            testimonyEvidenceDescription = "Confronted with the fabric match, the caretaker admits entering the bakery's kitchen despite denying it. The admission and the victim-only poison result must be considered with the motive in the letter.",
            suspects = new[] { "Caretaker", "Baker" },
            objects = new[] { "Poison", "Knife", "Pistol" },
            places = new[] { "Bakery", "Closed bar", "Apartment" },
            objectQuestion = "WHAT CAUSED THE DEATH?", placeQuestion = "WHERE WAS THE VICTIM POISONED?",
            interviewNames = new[] { "Caretaker", "Baker", "Nurse • witness", "Police officer" },
            statements = new[] {
                "I only came back to collect my belongings. I never entered the kitchen.",
                "I prepared the coffee as usual and drank from the same pot. I had argued with the victim about delayed wages, but I did not touch their cup.",
                "I saw the victim at the bakery that morning. They drank coffee and became ill shortly afterward. I tried to help.",
                "The body was found in the closed bar. We will collect the items you identify and compare them with the witness accounts."
            },
            explanations = new[] {
                "I... did enter the bakery kitchen, just for a moment. I only wanted to retrieve something. I did not poison the coffee.",
                "The victim always used that cup. Anyone who worked there knew where it was kept, but I did not poison it.",
                "I saw the victim take a few sips and collapse. I cannot say who prepared or handled that cup.",
                "The evidence shows where the victim collapsed and how the body was moved. The lab reports and witness accounts should be compared before making an accusation."
            },
            analysisPrerequisites = new[] {
                new[] { "c3_padaria_xicara" },
                new[] { "c3_padaria_retalho" }
            },
            analysisMessages = new[] {
                "Toxicology found a poisonous substance in the victim's cup. The coffee-pot sample is clean. The toxicology report and the victim's personnel records are ready at the police station.",
                "The yellow seam and tear on the caretaker's uniform match the fabric caught in the cup cabinet. The comparison report is ready at the police station."
            },
            ending = "The coffee pot was clean, but the victim's cup was poisoned. The recovered footage fixed the collapse at the bakery, and the fabric match placed the caretaker in the cup cabinet he denied entering. The dismissal letter revealed the motive. The body was later moved to the closed bar."
        }
    };

    public static int Index(string id) => System.Array.FindIndex(All, c => c.id == id);
    public static CaseZeroCase Get(string id) => All[System.Math.Max(0, Index(id))];
}

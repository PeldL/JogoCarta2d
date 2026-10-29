// Case content is separate from scene objects. Evidence keys are local to a case;
// the five legacy keys preserve the existing scene button bindings and old saves.
public sealed class CaseZeroCase
{
    public string id, title, location, introduction, call, ending;
    public string[] evidenceNames, evidenceDescriptions, suspects, statements, explanations;
    public string[] objects, places;
    public string objectQuestion, placeQuestion;
    public int culprit;
}

public static class CaseZeroCases
{
    public static readonly string[] EvidenceKeys = { "registro", "pistola", "capsula", "sangue", "depoimento" };
    public static readonly CaseZeroCase[] All = {
        new CaseZeroCase {
            id = "ultima-testemunha", title = "The Last Witness", location = "Bar", culprit = 0,
            introduction = "A woman entered the bar and was shot dead. Gather evidence, question the man behind the counter, and establish the culprit, weapon, and crime scene.",
            suspects = new[] { "Man behind the counter", "Bar employee", "Unknown customer" },
            objects = new[] { "Pistol", "Knife", "Poison" }, places = new[] { "Bar", "Street", "Victim's home" },
            objectQuestion = "WHICH WEAPON?", placeQuestion = "WHERE?",
            ending = "The man behind the counter was a robber. He killed the woman with the pistol in the bar to stop her from reporting him.\n\nNEW INCIDENT: Helena Duarte was found unconscious in Apartment 302. A USB drive is missing."
        },
        new CaseZeroCase {
            id = "apartamento-302", title = "Blind Spot", location = "Apartamento", culprit = 1,
            introduction = "A victim was found in Apartment 302 with 28 stab wounds. The apartment is not the murder scene: the evidence points to a bar that had already closed. Reconstruct the security-camera blind spot and identify who moved the body.",
            evidenceNames = new[] { "Forensic report", "Fibers from the service entrance", "Security access log", "Camera blind spot", "Contradictory testimony" },
            evidenceDescriptions = new[] {
                "The autopsy records 28 stab wounds. The pattern and the lack of a struggle in the apartment indicate that the attack happened somewhere else.",
                "Fibers on the victim's clothing match the service entrance of the closed bar. They are not found in the apartment's common areas.",
                "The bar's access log records a security credential after closing time. The credential belongs to the security guard, despite his claim that he had left.",
                "The cameras stopped recording for eleven minutes. A manual override was used from the security station, creating a blind spot during the time the victim disappeared.",
                "The security guard says he never entered the bar after closing. The access log and the camera override contradict that account; a radio message also places him near the service entrance."
            },
            suspects = new[] { "Building caretaker", "Security guard", "Night delivery worker", "Patrol officer" },
            statements = new[] {
                "I found out about the body only after the police arrived. I did not take anyone to the apartment.",
                "I completed my shift before closing and never went back inside. The camera interruption was a routine technical fault.",
                "I delivered a package to the building earlier that evening. I was gone long before the body was discovered.",
                "I answered a call from the apartment building later that night. I did not enter the bar and did not handle the victim."
            },
            explanations = new[] {
                "My service key only opens the building's utility rooms. The access log does not show me entering the bar.",
                "All right. I used the manual override and entered the closed bar. The argument escalated, and I stabbed the victim. I moved the body to the apartment to make the crime look like it happened there.",
                "The delivery record and the lobby camera confirm that I left before the bar closed. I had no access to the security station.",
                "My report records the time I received the call. The camera log places the guard at the bar before I arrived."
            },
            objects = new[] { "Knife — 28 stab wounds", "Poisoning", "Accidental fall" },
            places = new[] { "Closed bar (crime scene)", "Apartment 302 (body found)", "Bakery" },
            objectQuestion = "WHAT WAS THE ATTACK METHOD?", placeQuestion = "WHERE DID THE ATTACK HAPPEN?",
            call = "Forensics confirms 28 stab wounds and finds no evidence that the attack began in Apartment 302. Compare the bar's access log, the camera outage, and each suspect's timeline.",
            ending = "The security guard killed the victim with a knife in the closed bar, then moved the body to Apartment 302. The apartment was where the body was found, not where the murder happened. The camera outage was deliberate."
        },
        new CaseZeroCase {
            id = "arquivo-vertice", title = "The Last Break", location = "Bar", culprit = 2,
            introduction = "A person was found dead in the bar after closing. The toxicology report points to poisoning, but the evidence suggests the fatal drink was prepared at a bakery. Investigate both locations without confusing where the body was found with where the crime happened.",
            evidenceNames = new[] { "Toxicology report", "Bakery order slip", "After-hours entry log", "Drink container", "Contradictory testimony" },
            evidenceDescriptions = new[] {
                "The toxicology report confirms poisoning. There are no stab wounds or signs of a violent struggle, ruling out the method from the previous case.",
                "A bakery order slip records a drink prepared shortly before closing. The handwriting matches the note used by the janitor to request supplies.",
                "The bakery alarm was disarmed after closing with a maintenance code assigned to the janitor. The baker's own code was not used.",
                "Residue in the recovered drink container matches the substance identified by the laboratory. The container was moved to the bar after the victim became ill at the bakery.",
                "The janitor says the bakery was already empty when he left. The alarm log and the order slip place him inside after closing, and his timeline does not fit the laboratory estimate."
            },
            suspects = new[] { "Baker", "Nurse", "Janitor", "Police officer" },
            statements = new[] {
                "I closed the bakery and checked the counter before leaving. I did not prepare a special drink for the victim.",
                "I treated the victim earlier that day, but I left before evening. I did not visit the bakery or the bar that night.",
                "I only went to the bakery to finish maintenance. I did not prepare anything to drink, and I left before closing.",
                "I was called to the bar after the body was found. My report records the scene as I found it; I did not see the victim at the bakery."
            },
            explanations = new[] {
                "The closing checklist confirms that I left before the maintenance code was used. I did not prepare the drink on the slip.",
                "My work record confirms that I was off duty before the victim went to the bakery. I had no access to the drink container.",
                "I used the maintenance code after closing and prepared the poisoned drink. When the victim became ill, I moved the body to the bar to hide where it happened.",
                "My report begins after the body was found at the bar. The alarm log places the janitor at the bakery earlier that evening."
            },
            objects = new[] { "Poisoning", "Knife — 28 stab wounds", "Accidental overdose" },
            places = new[] { "Bakery (crime scene)", "Bar (body found)", "Hospital" },
            objectQuestion = "WHAT CAUSED THE DEATH?", placeQuestion = "WHERE DID THE POISONING HAPPEN?",
            call = "The laboratory confirms poisoning. The victim was found at the bar, but the toxicology estimate and the bakery records point to an earlier event. Check the maintenance code and the drink container.",
            ending = "The janitor poisoned the victim at the bakery, then moved the body to the bar. The toxicology report, order slip, and after-hours entry log contradict the janitor's statement."
        },
    };
    public static int Index(string id) => System.Array.FindIndex(All, c => c.id == id);
    public static CaseZeroCase Get(string id) => All[System.Math.Max(0, Index(id))];
}

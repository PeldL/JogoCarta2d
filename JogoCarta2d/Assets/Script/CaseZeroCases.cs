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
            id = "apartamento-302", title = "Apartment 302", location = "Apartamento", culprit = 2,
            introduction = "Helena Duarte is in hospital after an assault. Her USB drive is missing. Find out who stole it; responsibility for the assault is still under investigation.",
            evidenceNames = new[] { "Elevator log", "Shoe print", "Messages and visits", "Front desk log", "Challenged testimony" },
            evidenceDescriptions = new[] {
                "At 22:07, Carlos used his service key to access the third floor. Footage shows him leaving at 22:12 carrying a small device. He denied going upstairs.",
                "The print by the laptop desk matches Carlos's work shoes. A building inspection photo allows the soles to be compared. The print confirms his presence, but does not prove he committed the assault.",
                "At 21:40, Helena wrote: 'Marina has left; I saved the evidence about Vértice on the USB drive.' Footage confirms Marina left at 21:30. The laptop remained in the apartment, but the USB drive disappeared.",
                "André finished the repair at 16:00 and returned his access pass. Lucas was with the doorman from 22:00 to 22:20; their earlier argument concerned noise. At 22:15, the doorman found Helena on the floor. Carlos was the only one of the four to enter the apartment during that interval.",
                "Carlos admits using the spare key and taking the USB drive. Helena was already on the floor when he entered. His phone reads: 'Get the device. Nothing else.' The sender was unknown."
            },
            suspects = new[] { "Marina • friend", "Lucas • neighbor", "Carlos • building manager", "André • technician" },
            statements = new[] {
                "I visited Helena, but I left at 21:30. She was worried about some documents from work. I did not take anything.",
                "We argued about the noise that afternoon. That evening I stayed with the doorman. I did not enter the apartment.",
                "I have a spare key, but I did not go to the third floor that night. I was taking care of the building.",
                "I repaired the laptop that afternoon. I finished at 16:00 and left. The USB drive was there when I left."
            },
            explanations = new[] {
                "The footage and Helena's message confirm it: I left before the USB drive disappeared. She still had it after my visit.",
                "The front desk log confirms my whereabouts. The argument about noise had nothing to do with the theft.",
                "All right. The elevator entry and the shoe print are mine. I took the USB drive for money, but Helena was already on the floor. The instructions were: 'Get the device. Nothing else.'",
                "The work order confirms I left at 16:00. Helena's record shows she still had the USB drive that night."
            },
            objects = new[] { "USB drive", "Laptop", "Cash" }, places = new[] { "Apartment 302", "Front desk", "Parking lot" },
            objectQuestion = "WHAT WAS STOLEN?", placeQuestion = "WHERE DID THE THEFT HAPPEN?",
            call = "Helena is awake. She confirms that she stored evidence about the company on a USB drive and says: 'Look into Vértice.' Identify the thief; do not confuse the theft with the assault.",
            ending = "Carlos stole the USB drive in Apartment 302. The evidence proves the theft, not responsibility for the assault.\n\nHelena is awake: the device held evidence against Vértice. The next investigation will take place at the company office."
        },
        new CaseZeroCase {
            id = "arquivo-vertice", title = "The Vértice File", location = "Escritorio", culprit = 1,
            introduction = "Helena discovered suspicious payments. Examine the office, interview the four people involved, and identify who ran the scheme, what the crime was, and which company served as a front.",
            evidenceNames = new[] { "Transfer spreadsheet", "Vértice company records", "Access logs", "Instructions and report", "Challenged testimony" },
            evidenceDescriptions = new[] {
                "Payments with no corresponding services were authorized using Augusto Mendes's digital signature. Beatriz noted the irregularities and reported them before Helena was attacked.",
                "Vértice Services has no employees or recorded deliveries. Its bank account is controlled by Augusto. Horizonte and Atlas, named in the spreadsheet, have verified contracts and services.",
                "Files were deleted using Fábio's account, but from Augusto's computer while Fábio was away. The support report confirms the technician was absent.",
                "Authenticated messages from Augusto order the recovery of the USB drive and the intimidation of Helena. Carlos's instructions were limited to theft. A report identifies an outside enforcer as the attacker, acting on Augusto's orders. Renato was at the security post; his footage and logs help establish the timeline.",
                "Confronted with the digital signature, the Vértice account, and the messages, Augusto admits directing the embezzlement and the operation against Helena."
            },
            suspects = new[] { "Beatriz • accountant", "Augusto • director", "Renato • security guard", "Fábio • technician" },
            statements = new[] {
                "I prepared the spreadsheets, but I did not authorize those payments. I warned them that no services had been delivered.",
                "Vértice was just a supplier. I do not control its account, and I do not know who deleted the files.",
                "I was seen near Helena during my patrol. My shift records and the cameras show where I was.",
                "Someone used my account to delete files. I was working for another company at the time."
            },
            explanations = new[] {
                "My report was submitted before the assault. The signature authorizing the transfers is Augusto's, not mine.",
                "The records are genuine. I controlled Vértice and authorized the embezzlement. I also ordered the evidence recovered and Helena intimidated. Carlos was only supposed to take the USB drive.",
                "I handed over the patrol logs. They confirm my shift and the director's movements; I was not running the scheme.",
                "The support report confirms I was away. The access came from the director's computer. My account was used without my permission."
            },
            objects = new[] { "Embezzlement", "Equipment theft", "Product counterfeiting" },
            places = new[] { "Vértice Services", "Horizonte", "Atlas" },
            objectQuestion = "WHAT WAS THE CRIME?", placeQuestion = "WHICH SHELL COMPANY?",
            call = "Forensics confirmed that the spreadsheet and Vértice records are authentic. There are payments with no matching services. Compare the authorizations with the statements and preserve the other office records to clarify the connection to Helena.",
            ending = "Augusto ran an embezzlement scheme through Vértice Services. The documents also link his orders to the assault on Helena.\n\nCarlos is accountable for the theft; the enforcer identified in the report, for the assault. Helena provides a copy of the evidence. All three cases are closed."
        }
    };
    public static int Index(string id) => System.Array.FindIndex(All, c => c.id == id);
    public static CaseZeroCase Get(string id) => All[System.Math.Max(0, Index(id))];
}

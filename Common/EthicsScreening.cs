namespace ResearchPublicationManagementSystem.Common;

/// <summary>
/// The twenty questions a student works through before declaring whether their research needs
/// ethics approval.
///
/// Here rather than in the page, because two parties need the same list: the screen that asks
/// them, and the controller that has to say what each answer was an answer to when it records
/// them. Written out in the page and read back by position, a question reworded on one side and
/// not the other would file an answer against the wrong sentence.
///
/// The text is stored with each answer at the moment it is given, so a decision made last year
/// still reads as the questions were worded then. This list is what is asked today.
/// </summary>
public static class EthicsScreening
{
    public static readonly string[] Questions =
    [
        "Situations where the researcher may be at risk of harm",
        "Use of a questionnaire or interview, whether or not it is anonymous, which might reasonably be expected to cause discomfort, embarrassment or psychological or spiritual harm to the participants.",
        "Processes that are potentially disadvantageous to a person or group, such as the collection of information which may expose a person / group to discrimination.",
        "Collection of information of illegal behaviour(s) gained during the research which could place the participants at risk of criminal or civil liability or be damaging to their financial standing, employability, professional or personal relationships.",
        "Collection of blood, body fluid, tissue samples or other samples.",
        "Any form of exercise regime, or deprivation (e.g. sleep or dietary).",
        "Any form of physical examination (e.g. physical, radiation, ultrasound).",
        "The administration of any form of drug, medicine (other than in the course of standard medical procedure), or placebo.",
        "Physical pain, beyond mild discomfort.",
        "Participants whose identities are known to the researcher giving oral consent rather than written consent, other than for cultural reasons.",
        "Participants who are unable to give informed consent.",
        "The participation of children (seven (7) years old or younger).",
        "The participation of children under sixteen (16) years old where active parental consent is not being sought.",
        "Participants who are in a dependant situation, such as nursing home or prison, or patients highly dependent on medical care.",
        "Participants who are vulnerable.",
        "The use of previously collected identifiable personal information or research data for which there was no explicit consent for this research.",
        "The use of previously collected biological samples for which there was no explicit consent for this research.",
        "Any evaluation of organisational services or practices where information of a personal nature may be collected and where participants or the organisation may be identified.",
        "Deception of the participants, including concealment or covert observations.",
        "Payments or other financial inducements (other than reasonable reimbursement of travel expenses or time) to participants."
    ];

    /// <summary>Yes, No or Unsure, or null where a question was left unanswered.</summary>
    public static string? Normalise(string? answer) => (answer ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "yes" => "Yes",
        "no" => "No",
        "unsure" => "Unsure",
        _ => null
    };
}

namespace ResearchPublicationManagementSystem.Common
{
    /// <summary>
    /// The names of the decisions the API asks for a comment on, so a view can name the one its
    /// button makes. The list itself lives on the API, which decides what each one requires; these
    /// are the keys that address it.
    ///
    /// Kept as constants rather than typed into markup: a key with a typo in it silently reads as
    /// a decision nothing configures, and the screen would then quietly stop asking for a reason
    /// an administrator had asked for.
    /// </summary>
    public static class DecisionPoints
    {
        public const string ProposalSendToSupervisors = "proposal.send-to-supervisors";
        public const string ProposalSupervisorSelection = "proposal.supervisor-selection";
        public const string ProposalCoordinatorAssign = "proposal.coordinator-assign";
        public const string ProposalCoordinatorDiscard = "proposal.coordinator-discard";
        public const string ProposalRequestNewRound = "proposal.request-new-round";

        public const string EthicsSupervisorRuling = "ethics.supervisor-ruling";
        public const string EthicsSupervisorDocumentsAccept = "ethics.supervisor-documents-accept";
        public const string EthicsSupervisorDocumentsReturn = "ethics.supervisor-documents-return";
        public const string EthicsCoordinatorConfirmNotRequired = "ethics.coordinator-confirm-not-required";
        public const string EthicsCoordinatorOverturnNotRequired = "ethics.coordinator-overturn-not-required";
        public const string EthicsCoordinatorDocumentsApprove = "ethics.coordinator-documents-approve";
        public const string EthicsCoordinatorDocumentsReturn = "ethics.coordinator-documents-return";
        public const string EthicsHeadOfDepartmentReview = "ethics.head-of-department-review";
        public const string EthicsCoordinatorFinalApprove = "ethics.coordinator-final-approve";
        public const string EthicsCoordinatorFinalReturn = "ethics.coordinator-final-return";

        public const string PaperSupervisorAccept = "paper.supervisor-accept";
        public const string PaperSupervisorReturn = "paper.supervisor-return";
        public const string PaperCommitteeApprove = "paper.committee-approve";
        public const string PaperCommitteeReject = "paper.committee-reject";
        public const string PaperCoordinatorAccept = "paper.coordinator-accept";
        public const string PaperCoordinatorReturn = "paper.coordinator-return";
        public const string PaperCommitteeAssign = "paper.committee-assign";
        public const string PaperCommitteeAssignOverride = "paper.committee-assign-override";
        public const string PaperPublishOnBehalf = "paper.publish-on-behalf";
        public const string PaperWithdrawFromCatalogue = "paper.withdraw-from-catalogue";
    }
}

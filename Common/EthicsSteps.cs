namespace ResearchPublicationManagementSystem.Common;

/// <summary>Mirrors the backend's Common/EthicsSteps.cs: which ethics decision is outstanding.</summary>
public static class EthicsSteps
{
    public const string SupervisorDecision = "SupervisorDecision";
    public const string CoordinatorConfirmation = "CoordinatorConfirmation";
    public const string StudentUpload = "StudentUpload";
    public const string SupervisorDocumentReview = "SupervisorDocumentReview";
    public const string CoordinatorDocumentReview = "CoordinatorDocumentReview";
    public const string HeadOfDepartmentReview = "HeadOfDepartmentReview";
    public const string CoordinatorFinalDecision = "CoordinatorFinalDecision";

    /// <summary>
    /// The coordinator's first ethics screen. Two decisions arrive at the same moment: confirming
    /// that no documentation is needed, and reviewing documents a supervisor has accepted, so the
    /// screen asks for both and the API returns one queue.
    /// </summary>
    public const string CoordinatorFirstReview = $"{CoordinatorConfirmation},{CoordinatorDocumentReview}";
}

using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models.Messages
{
    /// <summary>
    /// One publication's conversations, from the point of view of whoever is reading.
    /// </summary>
    public class PublicationMessagesViewModel
    {
        public Guid ContainerId { get; set; }

        /// <summary>Shown above the conversation so nobody has to remember which publication this is.</summary>
        public string? PublicationTitle { get; set; }

        public string? StudentName { get; set; }

        /// <summary>False when an administrator has switched this off. What was written stays readable.</summary>
        public bool Enabled { get; set; }

        public IReadOnlyList<MessageCounterpartDto> Counterparts { get; set; } = [];

        /// <summary>
        /// Whose conversation is open. Null means the lot, which is what somebody with one
        /// correspondent sees anyway.
        /// </summary>
        public Guid? With { get; set; }

        public IReadOnlyList<ContainerMessageDto> Messages { get; set; } = [];

        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }

        /// <summary>Comma-separated, as an administrator configured it, for the file picker and the note under it.</summary>
        public string AllowedExtensions { get; set; } = string.Empty;

        public int MaximumLength { get; set; }
        public int MaximumAttachments { get; set; }

        public bool LoadFailed { get; set; }

        /// <summary>Where to go back to, which differs by role and is worked out by the controller.</summary>
        public string? BackUrl { get; set; }

        public MessageCounterpartDto? OpenWith =>
            With is { } id ? Counterparts.FirstOrDefault(c => c.UserId == id) : null;

        /// <summary>
        /// What the file picker offers. Passed through as configured, so an administrator who adds
        /// a type sees the picker offer it without anything being redeployed.
        /// </summary>
        public string AcceptAttribute => AllowedExtensions;

        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?> { ["id"] = ContainerId.ToString() };
            if (With is { } id) values["with"] = id.ToString();
            return values;
        }

        public PagerViewModel Pager() => new()
        {
            Controller = "Messages",
            Action = "Publication",
            Page = Page,
            TotalPages = TotalPages,
            RouteValues = RouteValues()
        };
    }
}

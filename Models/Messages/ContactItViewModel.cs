namespace ResearchPublicationManagementSystem.Models.Messages
{
    /// <summary>Writing to the institution's IT desk.</summary>
    public class ContactItViewModel
    {
        /// <summary>
        /// Whether there is a mail server to send through. False means the form would take a
        /// message and lose it, so the address below is offered instead.
        /// </summary>
        public bool ThroughTheSite { get; set; }

        /// <summary>The desk's address. Null when the institution has not set one, in which case there is nothing to offer.</summary>
        public string? EmailAddress { get; set; }

        public int MaximumLength { get; set; }
        public int MaximumAttachments { get; set; }
        public int MaximumAttachmentMegabytes { get; set; }

        public bool LoadFailed { get; set; }

        /// <summary>
        /// Where the reader was when they pressed Contact IT. Validated by the controller before
        /// it is used, since a return address in a query string is somewhere anybody can point.
        /// </summary>
        public string? ReturnUrl { get; set; }
    }
}

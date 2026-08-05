using System.ComponentModel.DataAnnotations;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>The invitations an administrator has sent, and the form for sending another.</summary>
    /// <summary>
    /// The invitations screen: what is outstanding and what has been dealt with.
    ///
    /// Two listings rather than one list sorted into two, and each is a page of its own from the
    /// API. Split here, the second block was whatever part of the first page happened to have been
    /// replied to, so an institution with a run of accepted invitations showed an empty
    /// "Waiting for a reply" while people were genuinely waiting.
    /// </summary>
    public class InvitationsViewModel
    {
        public IReadOnlyList<UserInvitationDto> Pending { get; set; } = [];
        public IReadOnlyList<UserInvitationDto> Settled { get; set; } = [];

        public int PendingTotal { get; set; }
        public int SettledTotal { get; set; }

        public PagerViewModel? PendingPager { get; set; }
        public PagerViewModel? SettledPager { get; set; }

        /// <summary>
        /// Needed for the roles that belong to one. External committee members are the exception,
        /// which is the whole reason they cannot register themselves.
        /// </summary>
        public IReadOnlyList<DepartmentDto> Departments { get; set; } = [];

        public bool LoadFailed { get; set; }

        /// <summary>
        /// One search box and one ordering, applied to both listings. Two of each would be two
        /// sets of controls for one screen, and nobody looking for an address knows in advance
        /// whether the person replied.
        /// </summary>
        public string? Search { get; set; }
        public string? Sort { get; set; }
        public bool Descending { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (HasSearch) values["search"] = Search;
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            return values;
        }

        public Dictionary<string, string?> ClearSearchRoute() =>
            RouteValues().Where(v => v.Key != "search").ToDictionary(v => v.Key, v => v.Value);

        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = "Invitations",
            Action = "Index",
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst,
            RouteValues = HasSearch ? new Dictionary<string, string?> { ["search"] = Search } : []
        };
    }

    /// <summary>
    /// Accepting an invitation. The role is not here on purpose. It comes from the invitation, so
    /// accepting one can never be a way to award yourself a role nobody offered.
    /// </summary>
    public class AcceptInvitationViewModel
    {
        public string Token { get; set; } = string.Empty;

        /// <summary>Null when the token is bad, expired or already used; the view says why.</summary>
        public InvitationPreviewDto? Invitation { get; set; }

        public string? Problem { get; set; }

        [Required(ErrorMessage = "Choose a password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type your password again.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

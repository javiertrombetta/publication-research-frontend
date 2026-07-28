using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class PublicationsController : Controller
    {
        public IActionResult Index()
        {
            var publications = new List<PublicationListItemViewModel>
{
    new()
    {
        Id = 1,
        PublicationId = "PUB-001",
        Title = "AI-Based Research Publication Management System",
        Student = "John Smith",
        Supervisor = "Dr. Brown",
        CommitteeMembers = new List<string>
        {
            "Dr. Brown",
            "Prof. David Tan",
            "Dr. Emily Wong"
        },
        ResearchArea = "Computer Science",
        Status = "Under Review",
        SubmittedDate = "22 Jul 2026",
        LastUpdated = "23 Jul 2026"
    },

    new()
    {
        Id = 2,
        PublicationId = "PUB-002",
        Title = "Machine Learning for Healthcare",
        Student = "Jane Wilson",
        Supervisor = "Dr. Wilson",
        CommitteeMembers = new List<string>
        {
            "Dr. Wilson",
            "Dr. Michael Lim"
        },
        ResearchArea = "Artificial Intelligence",
        Status = "Approved",
        SubmittedDate = "18 Jul 2026",
        LastUpdated = "21 Jul 2026"
    },

    new()
    {
        Id = 3,
        PublicationId = "PUB-003",
        Title = "Cloud Security Framework",
        Student = "Michael Lee",
        Supervisor = "Dr. Johnson",
        CommitteeMembers = new(),
        ResearchArea = "Cyber Security",
        Status = "Pending Committee Assignment",
        SubmittedDate = "15 Jul 2026",
        LastUpdated = "20 Jul 2026"
    }
};

            var model = new PublicationListViewModel
            {
                Toolbar = new SearchFilterToolbarViewModel
                {
                    SearchPlaceholder = "Search publications...",
                    PersonLabel = "Committee",

                    StatusOptions = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "All Statuses", Value = "" },
                        new SelectListItem { Text = "Pending Committee Assignment", Value = "Pending Committee Assignment" },
                        new SelectListItem { Text = "Under Review", Value = "Under Review" },
                        new SelectListItem { Text = "Approved", Value = "Approved" }
                    },

                    CategoryOptions = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "All Categories", Value = "" },
                        new SelectListItem { Text = "Computer Science", Value = "Computer Science" },
                        new SelectListItem { Text = "Artificial Intelligence", Value = "Artificial Intelligence" },
                        new SelectListItem { Text = "Cyber Security", Value = "Cyber Security" }
                    },

                    PersonOptions = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "All Committees", Value = "" },
                        new SelectListItem { Text = "Committee A", Value = "Committee A" },
                        new SelectListItem { Text = "Committee B", Value = "Committee B" },
                        new SelectListItem { Text = "Committee C", Value = "Committee C" }
                    }
                },

                TotalPublications = 86,
                PendingCommitteeAssignment = 37,
                UnderReview = 5,
                Approved = 44,

                Publications = publications
            };

            return View(model);
        }
        public IActionResult Details(int id)
        {
            var model = new PublicationDetailsViewModel
            {
                Id = id,

                PublicationId = "PB-2026-001",

                Title = "Artificial Intelligence Based Healthcare Diagnosis System",

                Status = "Approved",

                Abstract = "This publication presents an artificial intelligence framework that assists healthcare professionals in diagnosing chronic diseases through machine learning and predictive analytics. The study demonstrates improved diagnosis accuracy while reducing processing time and supporting clinical decision-making.",

                Version = "Version 2",

                SubmittedDate = new DateTime(2026, 7, 25),

                LastUpdated = new DateTime(2026, 8, 5),

                RelatedProposalId = "RP-2026-001",

                StudentId = "ST20260015",

                StudentName = "John Doe",

                StudentEmail = "john.doe@student.ais.ac.nz",

                SupervisorName = "Dr Sarah Chen",

                ResearchCategory = "Artificial Intelligence",

                CommitteeName = "AI Research Committee",
                CommitteeMembers = new List<CommitteeMemberViewModel>
{
    new()
    {
        Id = 1,
        MemberName = "Dr Sarah Chen",
        CommitteeRole = "Committee Chair",
        ReviewStatus = "Completed",
        Recommendation = "Approve"
    },

    new()
    {
        Id = 2,
        MemberName = "Dr Michael Brown",
        CommitteeRole = "Committee Member",
        ReviewStatus = "Completed",
        Recommendation = "Minor Revision"
    },

    new()
    {
        Id = 3,
        MemberName = "Dr Alice Lim",
        CommitteeRole = "Committee Member",
        ReviewStatus = "Pending",
        Recommendation = "-"
    }
},

                CommitteeReviews = new List<CommitteeReviewViewModel>
{
    new()
    {
        Id = 1,
        ReviewerName = "Dr Sarah Chen",
        CommitteeRole = "Committee Chair",
        ReviewDate = new DateTime(2026, 8, 2),
        Recommendation = "Approve",
        Comments = "The publication demonstrates strong methodology and significant contribution."
    },

    new()
    {
        Id = 2,
        ReviewerName = "Dr Michael Brown",
        CommitteeRole = "Committee Member",
        ReviewDate = new DateTime(2026, 8, 3),
        Recommendation = "Minor Revision",
        Comments = "Minor formatting corrections required."
    }
},

                Workflow = new List<PublicationWorkflowItemViewModel>
        {
            new(){ Order=1, StepName="Publication Submitted", IsCompleted=true },

            new(){ Order=2, StepName="Committee Review", IsCompleted=true },

            new(){ Order=3, StepName="Revision Submitted", IsCompleted=true },

            new(){ Order=4, StepName="Committee Review Completed", IsCompleted=true },

            new(){ Order=5, StepName="Coordinator Review Completed", IsCompleted=true },

            new(){ Order=6, StepName="Publication Approved", IsCompleted=true },

            new(){ Order=7, StepName="Published", IsCompleted=true, IsCurrentStep=true }
        },

                History = new List<PublicationHistoryItemViewModel>
        {
            new()
            {
                Version="Version 1",
                Date=new DateTime(2026,7,25),
                UpdatedBy="John Doe",
                Description="Initial Submission"
            },

            new()
            {
                Version="Version 2",
                Date=new DateTime(2026,8,5),
                UpdatedBy="John Doe",
                Description="Minor Revision"
            }
        },

                ActivityLogs = new List<ActivityLogItemViewModel>
        {
            new()
            {
                ActivityDate=new DateTime(2026,7,25),
                Description="Publication submitted"
            },

            new()
            {
                ActivityDate=new DateTime(2026,7,26),
                Description="Assigned to AI Research Committee"
            },

            new()
            {
                ActivityDate=new DateTime(2026,7,30),
                Description="Committee review completed"
            },

            new()
            {
                ActivityDate=new DateTime(2026,8,2),
                Description="Minor revision requested"
            },

            new()
            {
                ActivityDate=new DateTime(2026,8,5),
                Description="Revised publication submitted"
            },

            new()
            {
                ActivityDate=new DateTime(2026,8,5),
                Description="Coordinator review completed"
            },

            new()
            {
                ActivityDate=new DateTime(2026,8,7),
                Description="Publication approved"
            },

            new()
            {
                ActivityDate=new DateTime(2026,8,8),
                Description="Publication published"
            }
        }
            };

            return View(model);
        }
    }
}

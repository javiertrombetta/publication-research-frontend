 # Research Publication Management System (RPMS)

This branch contains the frontend implementation that I developed for the Research Publication Management System (RPMS).

## Running the Project

1. Clone the repository.
2. Open the **ResearchPublicationManagementSystem** project folder in **Visual Studio 2022**.
3. Build the solution.
4. Press **F5** or click **Run**.

Alternatively, you can run the project using the .NET CLI:

```bash
dotnet run
```

After the application starts, open your web browser and navigate to the URL shown in the terminal, for example:

```
https://localhost:5001
```

or

```
http://localhost:5000
```

Append the following routes to the URL to access each screen.

---

# Available Screens

## User Management

| Screen | URL |
|---------|-----|
| User List | `/Users` |
| Create User | `/Users/Create` |
| Edit User | `/Users/Edit` |

---

## Category Management

| Screen | URL |
|---------|-----|
| Category List | `/Categories` |
| Create Category | `/Categories/Create` |
| Edit Category | `/Categories/Edit` |

---

## System Settings

| Screen | URL |
|---------|-----|
| System Settings | `/SystemSettings` |

---

## Audit Log

| Screen | URL |
|---------|-----|
| Audit Log | `/AuditLogs` |

---

## Authentication

| Screen | URL |
|---------|-----|
| Log In | `/Auth/home` |
| Password Recovery | `/Auth/passwordrecovery` |
| Password Reset | `/Auth/passwordreset` |
| Sign Up | `/Auth/signup` |

---

## Coordinator

| Screen | URL |
|---------|-----|
| Assigning proposal for supervisor | `/Coordinator/assigning_proposal_forsupervisor` |
| Committee Review | `/Coordinator/committee_review` |
| Coordinator Dashboard | `/Coordinator/Coordinator_dashboard` |
| Edit Staff Profile | `/Coordinator/Edit_staff_profile` |
| Ethic review after supervisor | `/Coordinator/Ethic_review_aftersupervisor` |
| Evaluation after committee | `/Coordinator/Evaluation_after_committee` |
| Ethic review after head of department | `/Coordinator/Ethic_review_afters_headofdepartment` |
| Select a proposal for student | `/Coordinator/select_a_proposal_forstudent` |
| Staff Profile | `/Coordinator/staff_profile` |
| Assign Committee Members | `/Coordinator/assigning_committee_members` |


---

## ExternalSupervisor

| Screen | URL |
|---------|-----|
| Committee Review | `/ExternalSupervisor/committee_review` |
| Edit Staff Profile | `/ExternalSupervisor/Edit_staff_profile` |
| External Supervisor Dashboard | `/ExternalSupervisor/External_Supervisor_Dashboard` |
| Staff Profile | `/ExternalSupervisor/staff_profile` |

---

## HeadOfDepartment

| Screen | URL |
|---------|-----|
| All proposals from student | `/HeadOfDepartment/all_proposals_fromstudent` |
| Committee Review | `/HeadOfDepartment/committee_review` |
| Edit Staff Profile | `/HeadOfDepartment/Edit_staff_profile` |
| Head Of Department Dashboard | `/HeadOfDepartment/Head_of_Department_dashboard` |
| Head of Deaprtment Feedback | `/HeadOfDepartment/Headofdepartment_feedback` |
| Staff Profile | `/HeadOfDepartment/staff_profile` |

---

## Public

| Screen | URL |
|---------|-----|
| Public Catalogue | `/Public/public_catalogue` |
| Published Detail | `/Public/published_detail` |

---
## Student

| Screen | URL |
|---------|-----|
| Create Proposal | `/Student/Create_proposals` |
| Create Publication | `/Student/Create_Publication` |
| Edit Student Profile | `/Student/Edit_studentprofile` |
| Ethic risk assessment | `/Student/Ethic_risk_assessment` |
| Student Dashboard | `/Student/student_dashboard` |
| Student Profile | `/Student/studentprofile` |
| Upload ethic file | `/Student/Upload_Ethic_file` |

---
## Supervisor

| Screen | URL |
|---------|-----|
| Committee Review | `/Supervisor/committee_review` |
| Edit Staff Profile | `/Supervisor/Edit_staff_profile` |
| Ethic document review | `/Supervisor/Ethic_document_review` |
| Proposal Review | `/Supervisor/proposal_review` |
| Publication Review | `/Supervisor/publication_review` |
| Review ethic assessment checklist | `/Supervisor/Review_Ethic_assessmentchecklist` |
| Staff Profile | `/Supervisor/staff_profile` |
| Supervisor Dashboard | `/Supervisor/SupervisorDashboard` |

---

## Admin

| Screen | URL |
|---------|-----|
| Admin Dashboard | `/Admin/dashboard` |
| Admin Check Proposal Details | `/Admin/Admin_check_proposaldetail` |
| Assign Committee Members | `/Admin/assigning_committee_members` |

---

## Proposal Management

| Screen | URL |
|---------|-----|
| Proposal Management | `/Proposals` |

---

## Publication Management

| Screen | URL |
|---------|-----|
| Publication Management | `/Publications` |
| Publication Details | `/Publications/Details` |


## Current Progress
The frontend implementation has been completed using **ASP.NET Core MVC**, **Bootstrap 5**, and **Tabler UI**.

### ✅ Completed Modules

- Dashboard
- Authentication
- User Management
- Category Management
- System Settings
- Audit Log
- Proposal Management
- Publication Management
- Publication Details

### 🔄 Next Phase

- Backend API integration
- Frontend and backend integration testing
- Bug fixing and UI refinements

---

## Technology Stack

- ASP.NET Core MVC (.NET 8)
- Razor Views
- Entity Framework Core
- ASP.NET Identity
- SQL Server
- Bootstrap 5
- Tabler UI

---

Lei Yee Wynn Thaung


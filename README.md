<<<<<<< HEAD
<<<<<<< HEAD
﻿ # Research Publication Management System (RPMS)

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

## Dashboard

| Screen | URL |
|---------|-----|
| Admin Dashboard | `/Admin` |

---

## Authentication

| Screen | URL |
|---------|-----|
| Category List | `/Auth/home` |
| Create Category | `/Auth/passwordrecovery` |
| Edit Category | `/Auth/passwordreset` |
| Category List | `/Auth/signup` |

---

## Coordinator

| Screen | URL |
|---------|-----|
| Category List | `/Coordinator/assigning_proposal_forsupervisor` |
| Create Category | `/Coordinator/committee_review` |
| Edit Category | `/Coordinator/Coordinator_dashboard` |
| Create Category | `/Coordinator/Edit_staff_profile` |
| Edit Category | `/Coordinator/Ethic_review_aftersupervisor` |
| Category List | `/Coordinator/Evaluation_after_committee` |
| Create Category | `/Coordinator/Ethic_review_afters_headofdepartment` |
| Edit Category | `/Coordinator/select_a_proposal_forstudent` |
| Create Category | `/Coordinator/staff_profile` |


---

## ExternalSupervisor

| Screen | URL |
|---------|-----|
| Category List | `/ExternalSupervisor/committee_review` |
| Create Category | `/ExternalSupervisor/Edit_staff_profile` |
| Edit Category | `/ExternalSupervisor/External_Supervisor_Dashboard` |
| Create Category | `/ExternalSupervisor/staff_profile` |

---

## HeadOfDepartment

| Screen | URL |
|---------|-----|
| Category List | `/HeadOfDepartment/all_proposals_fromstudent` |
| Create Category | `/HeadOfDepartment/committee_review` |
| Edit Category | `/HeadOfDepartment/Edit_staff_profile` |
| Create Category | `/HeadOfDepartment/Head_of_Department_dashboard` |
| Edit Category | `/HeadOfDepartment/Headofdepartment_feedback` |
| Category List | `/HeadOfDepartment/staff_profile` |

---

## Public

| Screen | URL |
|---------|-----|
| Category List | `/Public/public_catalogue` |
| Create Category | `/Public/published_detail` |

---
## Student

| Screen | URL |
|---------|-----|
| Category List | `/Student/Create_proposals` |
| Create Category | `/Student/Create_Publication` |
| Edit Category | `/Student/Edit_studentprofile` |
| Create Category | `/Student/Ethic_risk_assessment` |
| Edit Category | `/Student/student_dashboard` |
| Category List | `/Student/studentprofile` |
| Create Category | `/Student/Upload_Ethic_file` |

---
## Supervisor

| Screen | URL |
|---------|-----|
| Category List | `/Supervisor/committee_review` |
| Create Category | `/Supervisor/Edit_staff_profile` |
| Edit Category | `/Supervisor/Ethic_document_review` |
| Create Category | `/Supervisor/proposal_review` |
| Edit Category | `/Supervisor/publication_review` |
| Category List | `/Supervisor/Review_Ethic_assessmentchecklist` |
| Create Category | `/Supervisor/staff_profile` |
| Edit Category | `/Supervisor/SupervisorDashboard` |


## Current Progress

Completed modules:

- ✅ Dashboard
- ✅ User Management
- ✅ Category Management
- ✅ System Settings
- ✅ Audit Log

Modules currently under development:

- ⏳ Role Assignment
- ⏳ Proposal Management
- ⏳ Publication Management
- ⏳ Workflow
- ⏳ Reports

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

=======
﻿ # Research Publication Management System (RPMS)

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

## Dashboard

| Screen | URL |
|---------|-----|
| Admin Dashboard | `/Admin/dashboard` |

---

## Authentication

| Screen | URL |
|---------|-----|
| Category List | `/Auth/home` |
| Create Category | `/Auth/passwordrecovery` |
| Edit Category | `/Auth/passwordreset` |
| Category List | `/Auth/signup` |

---

## Coordinator

| Screen | URL |
|---------|-----|
| Category List | `/Coordinator/assigning_proposal_forsupervisor` |
| Create Category | `/Coordinator/committee_review` |
| Edit Category | `/Coordinator/Coordinator_dashboard` |
| Create Category | `/Coordinator/Edit_staff_profile` |
| Edit Category | `/Coordinator/Ethic_review_aftersupervisor` |
| Category List | `/Coordinator/Evaluation_after_committee` |
| Create Category | `/Coordinator/Ethic_review_afters_headofdepartment` |
| Edit Category | `/Coordinator/select_a_proposal_forstudent` |
| Create Category | `/Coordinator/staff_profile` |


---

## ExternalSupervisor

| Screen | URL |
|---------|-----|
| Category List | `/ExternalSupervisor/committee_review` |
| Create Category | `/ExternalSupervisor/Edit_staff_profile` |
| Edit Category | `/ExternalSupervisor/External_Supervisor_Dashboard` |
| Create Category | `/ExternalSupervisor/staff_profile` |

---

## HeadOfDepartment

| Screen | URL |
|---------|-----|
| Category List | `/HeadOfDepartment/all_proposals_fromstudent` |
| Create Category | `/HeadOfDepartment/committee_review` |
| Edit Category | `/HeadOfDepartment/Edit_staff_profile` |
| Create Category | `/HeadOfDepartment/Head_of_Department_dashboard` |
| Edit Category | `/HeadOfDepartment/Headofdepartment_feedback` |
| Category List | `/HeadOfDepartment/staff_profile` |

---

## Public

| Screen | URL |
|---------|-----|
| Category List | `/Public/public_catalogue` |
| Create Category | `/Public/published_detail` |

---
## Student

| Screen | URL |
|---------|-----|
| Category List | `/Student/Create_proposals` |
| Create Category | `/Student/Create_Publication` |
| Edit Category | `/Student/Edit_studentprofile` |
| Create Category | `/Student/Ethic_risk_assessment` |
| Edit Category | `/Student/student_dashboard` |
| Category List | `/Student/studentprofile` |
| Create Category | `/Student/Upload_Ethic_file` |

---
## Supervisor

| Screen | URL |
|---------|-----|
| Category List | `/Supervisor/committee_review` |
| Create Category | `/Supervisor/Edit_staff_profile` |
| Edit Category | `/Supervisor/Ethic_document_review` |
| Create Category | `/Supervisor/proposal_review` |
| Edit Category | `/Supervisor/publication_review` |
| Category List | `/Supervisor/Review_Ethic_assessmentchecklist` |
| Create Category | `/Supervisor/staff_profile` |
| Edit Category | `/Supervisor/SupervisorDashboard` |


## Current Progress

Completed modules:

- ✅ Dashboard
- ✅ User Management
- ✅ Category Management
- ✅ System Settings
- ✅ Audit Log

Modules currently under development:

- ⏳ Role Assignment
- ⏳ Proposal Management
- ⏳ Publication Management
- ⏳ Workflow
- ⏳ Reports

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

>>>>>>> 732aab9 (adjust readme)
=======
﻿ # Research Publication Management System (RPMS)

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

## Dashboard

| Screen | URL |
|---------|-----|
| Admin Dashboard | `/Admin/dashboard` |

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


## Current Progress

Completed modules:

- ✅ Dashboard
- ✅ User Management
- ✅ Category Management
- ✅ System Settings
- ✅ Audit Log

Modules currently under development:

- ⏳ Role Assignment
- ⏳ Proposal Management
- ⏳ Publication Management
- ⏳ Workflow
- ⏳ Reports

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

>>>>>>> b3be616 (adjust readme)

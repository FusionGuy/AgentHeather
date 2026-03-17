using HeatherDemoApp.Models;

namespace HeatherDemoApp.Services;

public interface IPdfService
{
    Task<List<PdfDocument>> GetAllDocumentsAsync();
    Task<PdfDocument?> GetDocumentByIdAsync(string id);
    Task<List<PdfDocument>> SearchAsync(string searchTerm);
    Task InitializeAsync();
}

public class PdfService : IPdfService
{
    private readonly List<PdfDocument> _documents = new();

    public Task InitializeAsync()
    {
        // Pre-populate with extracted PDF data from the PDF directory
        _documents.AddRange(new[]
        {
            new PdfDocument
            {
                Id = "1",
                FileName = "hr_manual.pdf",
                Title = "ENSO Group HR Manual",
                Content = @"ENSO GROUP
HUMAN RESOURCES
MANUAL

Table of Contents
1. Introduction to ENSO Group
2. Employment Policies
3. Recruitment and Selection
4. Compensation and Benefits
5. Performance Management
6. Leave Management
7. Workplace Conduct
8. Health and Safety
9. Disciplinary Procedures
10. Termination Policies
11. Employee Development

1. Introduction to ENSO Group
ENSO Group is committed to maintaining a professional work environment that fosters growth, innovation, and mutual respect. This HR manual outlines the policies and procedures that govern employment relationships within our organization.

2. Employment Policies
2.1 Equal Employment Opportunity
ENSO Group is an equal opportunity employer. We do not discriminate based on race, color, religion, gender, national origin, age, disability, or any other protected characteristic.

2.2 Employment Classification
- Full-time employees: Work 40 hours per week
- Part-time employees: Work fewer than 40 hours per week
- Contract employees: Hired for specific projects or time periods
- Interns: Temporary positions for learning and development

3. Recruitment and Selection
3.1 Job Posting
All open positions are posted internally and externally. Employees are encouraged to apply for internal opportunities.

3.2 Selection Process
- Application review
- Initial screening
- Interviews
- Reference checks
- Background verification
- Offer letter

4. Compensation and Benefits
4.1 Salary Structure
Salaries are competitive and based on market rates, experience, and qualifications. Annual reviews determine merit increases.

4.2 Benefits Package
- Health insurance
- Life insurance
- Retirement plans
- Paid time off
- Employee assistance programs
- Professional development

5. Performance Management
5.1 Performance Reviews
Annual performance reviews are conducted to assess employee progress and set goals.

5.2 Key Performance Indicators
Employees are evaluated on:
- Job knowledge
- Quality of work
- Productivity
- Communication
- Teamwork
- Initiative

6. Leave Management
6.1 Types of Leave
- Annual leave: 15-25 days based on tenure
- Sick leave: 10 days per year
- Personal leave: Up to 5 days
- Maternity/Paternity leave: As per local laws
- Bereavement leave: 3-5 days

7. Workplace Conduct
7.1 Code of Conduct
Employees must maintain professional behavior at all times, treating colleagues with respect and dignity.

7.2 Confidentiality
All company information must be kept confidential unless authorized for disclosure.

8. Health and Safety
8.1 Workplace Safety
ENSO Group maintains a safe work environment and complies with all occupational health and safety regulations.

8.2 Emergency Procedures
Regular drills and training ensure employees are prepared for emergencies.

9. Disciplinary Procedures
9.1 Progressive Discipline
- Verbal warning
- Written warning
- Final written warning
- Termination

10. Termination Policies
10.1 Voluntary Termination
Employees must provide notice as per their employment contract, typically 2-4 weeks.

10.2 Involuntary Termination
May occur due to poor performance, policy violations, or organizational restructuring.

11. Employee Development
ENSO Group invests in employee growth through training programs, workshops, and educational assistance.",
                PageCount = 79,
                IngestedDate = DateTime.UtcNow
            },
            new PdfDocument
            {
                Id = "2",
                FileName = "HR-Guide_-Policy-and-Procedure-Template.pdf",
                Title = "Community Foundations of Canada - HR Guide",
                Content = @"HR Guide: Policy and Procedure Template
Community Foundations of Canada

Table of Contents
1. Introduction
2. Purpose and Scope
3. Policy Development Framework
4. Key HR Policies
5. Implementation Guidelines

1. Introduction
This guide provides a comprehensive framework for developing and implementing HR policies within community foundations. It serves as a template for organizations to adapt to their specific needs.

2. Purpose and Scope
2.1 Purpose
To provide clear guidelines for HR management and ensure consistent application of policies across the organization.

2.2 Scope
This guide covers all employees, contractors, and volunteers within the organization.

3. Policy Development Framework
3.1 Policy Writing Guidelines
- Use clear, concise language
- Include effective dates
- Define responsible parties
- Outline review procedures
- Include compliance requirements

3.2 Policy Approval Process
- Draft policy creation
- Legal review
- Management approval
- Employee communication
- Implementation

4. Key HR Policies
4.1 Employment Standards
Hours of work, overtime, breaks, and scheduling requirements as per provincial employment standards legislation.

4.2 Hiring Policy
Position approval, job posting, screening, interviews, reference checks, and offer process.

4.3 Compensation Policy
Salary bands, performance-based increases, and market adjustments.

4.4 Benefits Policy
Health benefits, retirement plans, and other employee benefits.

4.5 Leave Policy
Vacation, sick leave, personal days, and other statutory leaves.

4.6 Termination Policy
Notice requirements, severance, and exit procedures.

5. Implementation Guidelines
5.1 Communication
All policies must be communicated to employees and made accessible in the employee handbook.

5.2 Training
Managers receive training on policy interpretation and application.

5.3 Monitoring
Regular reviews ensure policies remain current and compliant with legislation.",
                PageCount = 45,
                IngestedDate = DateTime.UtcNow
            },
            new PdfDocument
            {
                Id = "3",
                FileName = "HUMAN_RESOURCE_POLICIES_-_GESCI__June_2018.pdf",
                Title = "GESCI HR Policies Manual",
                Content = @"GESCI
HUMAN RESOURCE POLICIES
June 2018

Table of Contents
1. Scope and Application
2. Employment Information
3. Recruitment and Appointment
4. Compensation
5. Leave
6. Travel
7. Performance Management
8. Separation from Service
9. Disciplinary Procedures
10. Code of Conduct

1. Scope and Application
1.1 Scope
These policies apply to all staff members of GESCI, including permanent, temporary, and contract employees.

1.2 Application
Policies are effective from the date of approval and apply to all locations where GESCI operates.

2. Employment Information
2.1 Employment Categories
- Permanent full-time
- Permanent part-time
- Fixed-term contract
- Temporary contract
- Consultant

2.2 Employment Contracts
All employees receive written contracts outlining terms and conditions of employment.

3. Recruitment and Appointment
3.1 Recruitment Process
- Position approval
- Advertisement
- Application screening
- Interview process
- Reference verification
- Selection decision
- Offer and acceptance

3.2 Appointment Criteria
Appointments are based on merit, qualifications, and suitability for the role.

4. Compensation
4.1 Salary Structure
Salaries are determined by role, qualifications, experience, and market rates.

4.2 Salary Review
Annual reviews consider performance, inflation, and market changes.

4.3 Allowances
May include housing, transport, and meal allowances as applicable.

5. Leave
5.1 Annual Leave
- 20 working days per year
- Accrual from first day of employment
- Carryover up to 10 days

5.2 Sick Leave
- 10 days per year
- Medical certificate required for 3+ days

5.3 Maternity Leave
- 12 weeks paid leave
- Additional unpaid leave available

5.4 Paternity Leave
- 5 days paid leave

5.5 Special Leave
Bereavement, marriage, and other special circumstances as defined.

6. Travel
6.1 Travel Authorization
All travel requires prior approval from the appropriate authority level.

6.2 Travel Allowances
Per diem rates for meals and incidentals as per GESCI policy.

6.3 Travel Booking
Travel arrangements must be made through approved channels.

7. Performance Management
7.1 Performance Framework
- Goal setting at beginning of year
- Quarterly reviews
- Annual assessment
- Development planning

7.2 Performance Ratings
- Outstanding
- Exceeds Expectations
- Meets Expectations
- Needs Improvement
- Unsatisfactory

8. Separation from Service
8.1 Voluntary Separation
Resignation with notice as per contract terms.

8.2 Involuntary Separation
Termination due to redundancy, poor performance, or misconduct.

8.3 Exit Procedures
- Handover of responsibilities
- Return of assets
- Exit interview
- Final settlement

9. Disciplinary Procedures
9.1 Principles
Fairness, consistency, and documentation throughout the process.

9.2 Disciplinary Steps
- Informal warning
- First written warning
- Final written warning
- Dismissal

10. Code of Conduct
All employees must uphold GESCI's values of integrity, professionalism, and respect.",
                PageCount = 62,
                IngestedDate = DateTime.UtcNow
            },
            new PdfDocument
            {
                Id = "4",
                FileName = "Human-Resources-Policy-Manual-RHA-Updated-February2022.pdf",
                Title = "RHA Health Services HR Policy Manual",
                Content = @"RHA HEALTH SERVICES
HUMAN RESOURCES
POLICY MANUAL
Updated February 2022

Table of Contents
1. Introduction and Purpose
2. Compliance and Governance
3. Employment Relationship
4. Hiring and Recruitment
5. Compensation and Benefits
6. Performance Management
7. Training and Development
8. Leave Administration
9. Workplace Policies
10. Health and Safety
11. Employee Relations
12. Termination and Separation
13. Appendices

1. Introduction and Purpose
1.1 Purpose
This manual provides comprehensive HR policies and procedures for RHA Health Services, ensuring consistent and fair treatment of all employees.

1.2 Policy Review
Policies are reviewed annually to ensure compliance with current legislation and best practices.

2. Compliance and Governance
2.1 Legal Framework
All policies comply with applicable federal, state, and local employment laws.

2.2 Policy Administration
The Human Resources department is responsible for policy interpretation and implementation.

2.3 Equal Employment Opportunity
RHA is committed to equal employment opportunity and prohibits discrimination and harassment.

3. Employment Relationship
3.1 At-Will Employment
Employment is at-will and may be terminated by either party at any time.

3.2 Employment Categories
- Regular Full-Time (40 hours/week)
- Regular Part-Time (less than 40 hours/week)
- Temporary (fixed duration)
- Per Diem (as needed)

3.3 Probationary Period
90-day probationary period for new employees.

4. Hiring and Recruitment
4.1 Position Approval
All positions must be approved and budgeted before recruitment begins.

4.2 Recruitment Process
- Job posting (internal and external)
- Application screening
- Interviews (minimum 2 rounds)
- Background checks
- Reference verification
- Offer and acceptance

4.3 New Hire Orientation
All new employees complete orientation within first week of employment.

5. Compensation and Benefits
5.1 Compensation Philosophy
Competitive, market-based compensation aligned with organizational goals.

5.2 Pay Structure
Position-specific salary ranges reviewed annually.

5.3 Pay Frequency
Bi-weekly payroll on designated pay dates.

5.4 Benefits Overview
- Medical Insurance
- Dental Insurance
- Vision Insurance
- Life Insurance
- Disability Insurance
- Retirement Plan (401k with match)
- Paid Time Off
- Paid Holidays

6. Performance Management
6.1 Performance Review Cycle
Annual reviews with mid-year check-ins.

6.2 Goal Setting
SMART goals aligned with department and organizational objectives.

6.3 Performance Improvement Plans
Formal PIP process for underperforming employees.

7. Training and Development
7.1 Mandatory Training
- Orientation
- Safety and compliance
- HIPAA and privacy
- Job-specific training

7.2 Professional Development
Tuition reimbursement and continuing education support.

8. Leave Administration
8.1 PTO Policy
- 0-2 years: 15 days
- 3-7 years: 20 days
- 8+ years: 25 days

8.2 Sick Leave
Annual sick leave accrual with rollover provisions.

8.3 FMLA
Family and Medical Leave Act compliance with job protection.

8.4 Other Leave Types
Bereavement, jury duty, military leave, personal leave.

9. Workplace Policies
9.1 Attendance
Expected attendance and punctuality with call-off procedures.

9.2 Dress Code
Professional attire appropriate for healthcare setting.

9.3 Electronic Communications
Appropriate use of email, internet, and electronic devices.

9.4 Social Media
Guidelines for professional conduct on social platforms.

10. Health and Safety
10.1 Workplace Safety
Compliance with OSHA regulations and workplace safety standards.

10.2 Incident Reporting
Immediate reporting of all workplace injuries and incidents.

10.3 Emergency Preparedness
Regular drills and training for emergencies.

11. Employee Relations
11.1 Open Door Policy
Management accessibility for employee concerns.

11.2 Grievance Procedure
Formal process for resolving workplace disputes.

11.3 Harassment Prevention
Zero tolerance policy with clear reporting mechanisms.

12. Termination and Separation
12.1 Voluntary Termination
Two-week notice requested; exit interview conducted.

12.2 Involuntary Termination
Documented reasons following progressive discipline.

12.3 Layoff and Recall
Process for reduction in force and recall rights.

12.4 Final Pay
Final paycheck including accrued benefits at separation.

13. Appendices
A. Employee Handbook Acknowledgment
B. Job Classification Descriptions
C. Benefits Summary
D. PTO Calculation Worksheet
E. Performance Review Forms
F. Disciplinary Action Forms",
                PageCount = 156,
                IngestedDate = DateTime.UtcNow
            }
        });

        return Task.CompletedTask;
    }

    public Task<List<PdfDocument>> GetAllDocumentsAsync()
    {
        return Task.FromResult(_documents.ToList());
    }

    public Task<PdfDocument?> GetDocumentByIdAsync(string id)
    {
        return Task.FromResult(_documents.FirstOrDefault(d => d.Id == id));
    }

    public Task<List<PdfDocument>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Task.FromResult(_documents.ToList());
        }

        var results = _documents.Where(d =>
            d.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            d.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            d.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        return Task.FromResult(results);
    }
}
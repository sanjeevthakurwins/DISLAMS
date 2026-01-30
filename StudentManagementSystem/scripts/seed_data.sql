-- DISLAMS Phase-2: Seed Data Script
-- Purpose: Insert test data for all tables (Actors, Students, Courses, AttendanceRecords, AuditLogs, AttendanceExceptions, ReopenRequests)
-- Usage: sqlcmd -S Ditsdev346 -d DISLAMS_StudentManagement -i .\scripts\seed_data.sql
-- Note: IDs are fixed GUIDs to make re-runs idempotent for tests (use DELETE/RECREATE DB if you re-run)

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- Actors (Teachers, AcademicCoordinator, Leadership)
    INSERT INTO Actors (Id, ExternalUserId, FullName, Email, Role, IsActive, LastActiveAt, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
    VALUES
      ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'teacher1', 'Alice Teacher', 'alice.teacher@example.local', 1, 1, '2026-01-28T08:00:00Z', '2026-01-28T08:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-28T08:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),
      ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'ac1', 'Bob Coordinator', 'bob.coordinator@example.local', 2, 1, '2026-01-28T08:05:00Z', '2026-01-28T08:05:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-28T08:05:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),
      ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'leader1', 'Carol Leadership', 'carol.lead@example.local', 3, 1, '2026-01-28T08:10:00Z', '2026-01-28T08:10:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-28T08:10:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');

    -- Students
    INSERT INTO Students (Id, StudentId, FullName, ClassGrade, Email, IsActive, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
    VALUES
      ('11111111-1111-1111-1111-111111111111', 'S001', 'John Doe', '10A', 'john.doe@example.local', 1, '2026-01-27T07:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-27T07:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),
      ('22222222-2222-2222-2222-222222222222', 'S002', 'Jane Roe', '10A', 'jane.roe@example.local', 1, '2026-01-27T07:05:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-27T07:05:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),
      ('33333333-3333-3333-3333-333333333333', 'S003', 'Sam Student', '9B', 'sam.student@example.local', 1, '2026-01-27T07:10:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-27T07:10:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');

    -- Courses
    INSERT INTO Courses (Id, CourseCode, CourseName, TeacherId, Description, IsActive, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
    VALUES
      ('44444444-4444-4444-4444-444444444444', 'MATH101', 'Mathematics 101', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Basic Mathematics', 1, '2026-01-20T08:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-20T08:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),
      ('55555555-5555-5555-5555-555555555555', 'ENG201', 'English 201', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Intermediate English', 1, '2026-01-20T08:10:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-20T08:10:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');

    -- AttendanceRecords
    -- Draft (not submitted)
    INSERT INTO AttendanceRecords (Id, StudentId, CourseId, AttendanceDate, Status, IsPresent, Remarks, Version, ParentVersionId, SubmittedAt, SubmittedBy, ApprovedAt, ApprovedBy, PublishedAt, PublishedBy, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
    VALUES
      ('66666666-6666-6666-6666-666666666666', '11111111-1111-1111-1111-111111111111', '44444444-4444-4444-4444-444444444444', '2026-01-27T00:00:00Z', 1, 1, 'Draft entry', 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '2026-01-27T08:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-27T08:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),

    -- Submitted (within 24 hours)
      ('77777777-7777-7777-7777-777777777777', '11111111-1111-1111-1111-111111111111', '44444444-4444-4444-4444-444444444444', '2026-01-27T00:00:00Z', 2, 1, 'Submitted by teacher', 0, NULL, '2026-01-27T09:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', NULL, NULL, NULL, NULL, '2026-01-27T09:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-27T09:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'),

    -- Approved
      ('88888888-8888-8888-8888-888888888888', '11111111-1111-1111-1111-111111111111', '44444444-4444-4444-4444-444444444444', '2026-01-27T00:00:00Z', 3, 1, 'Approved by AC', 0, NULL, '2026-01-27T09:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-27T11:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NULL, NULL, '2026-01-27T11:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-27T11:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),

    -- Published
      ('99999999-9999-9999-9999-999999999998', '22222222-2222-2222-2222-222222222222', '55555555-5555-5555-5555-555555555555', '2026-01-26T00:00:00Z', 4, 0, 'Published (student absent)', 0, NULL, '2026-01-26T08:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-26T10:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-26T12:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-26T12:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-26T12:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),

    -- Locked
      ('aaaaaaaa-0000-0000-0000-000000000000', '33333333-3333-3333-3333-333333333333', '44444444-4444-4444-4444-444444444444', '2026-01-25T00:00:00Z', 5, 1, 'Locked record', 0, NULL, '2026-01-25T08:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-25T11:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-25T12:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-25T12:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-25T12:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),

    -- Corrected example: original (now Corrected)
      ('99999999-9999-9999-9999-999999999999', '22222222-2222-2222-2222-222222222222', '44444444-4444-4444-4444-444444444444', '2026-01-20T00:00:00Z', 7, 0, 'Original - corrected', 0, NULL, '2026-01-20T09:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-20T10:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NULL, NULL, '2026-01-20T10:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-20T10:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),

    -- New correction draft referencing the original
      ('aaaaaaaa-0000-0000-0000-000000000001', '22222222-2222-2222-2222-222222222222', '44444444-4444-4444-4444-444444444444', '2026-01-20T00:00:00Z', 1, 1, 'Correction draft', 1, '99999999-9999-9999-9999-999999999999', NULL, NULL, NULL, NULL, NULL, NULL, '2026-01-20T10:05:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-20T10:05:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');

    -- AuditLogs (sample entries)
    INSERT INTO AuditLogs (Id, AttendanceRecordId, Action, PreviousStatus, NewStatus, ActorId, ActorRole, Reason, PreviousValue, NewValue, ActionTimestamp, ContextInfo, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
    VALUES
      ('aaaaaaaa-1111-1111-1111-aaaaaaaa1111', '77777777-7777-7777-7777-777777777777', 'Submitted', 1, 2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 'Submitted within window', '', '', '2026-01-27T09:00:00Z', 'Submission by teacher1', '2026-01-27T09:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-27T09:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'),
      ('aaaaaaaa-2222-2222-2222-aaaaaaaa2222', '88888888-8888-8888-8888-888888888888', 'Approved', 2, 3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 2, 'Approved by AC', '', '', '2026-01-27T11:00:00Z', 'Approval by AC', '2026-01-27T11:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-27T11:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');

    -- AttendanceExceptions
    INSERT INTO AttendanceExceptions (Id, AttendanceRecordId, ExceptionType, Description, ReportedAt, ReportedBy, ResolutionStatus, ResolutionNotes, ResolvedAt, ResolvedBy, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
    VALUES
      ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', '66666666-6666-6666-6666-666666666666', 1, 'Teacher absent; could not mark attendance for class', '2026-01-27T08:30:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Pending', '', NULL, NULL, '2026-01-27T08:30:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-27T08:30:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'),
      ('ffffffff-ffff-ffff-ffff-ffffffffffff', '77777777-7777-7777-7777-777777777777', 2, 'Submitted after submission window', '2026-01-28T09:00:00Z', '11111111-1111-1111-1111-111111111111', 'Pending', 'Requested reopen', NULL, NULL, '2026-01-28T09:00:00Z', '11111111-1111-1111-1111-111111111111', '2026-01-28T09:00:00Z', '11111111-1111-1111-1111-111111111111'),
      ('dddddddd-dddd-dddd-dddd-dddddddddddd', '88888888-8888-8888-8888-888888888888', 3, 'Parent disputes that student was absent', '2026-01-27T12:00:00Z', 'cccccccc-cccc-cccc-cccc-cccccccccccc', 'Pending', '', NULL, NULL, '2026-01-27T12:00:00Z', 'cccccccc-cccc-cccc-cccc-cccccccccccc', '2026-01-27T12:00:00Z', 'cccccccc-cccc-cccc-cccc-cccccccccccc'),
      ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb9999', '99999999-9999-9999-9999-999999999998', 4, 'System failure - delayed marking due to outage', '2026-01-26T12:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Resolved', 'System incident resolved', '2026-01-26T13:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-26T12:00:00Z', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-26T13:00:00Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');

    -- ReopenRequests
    INSERT INTO ReopenRequests (Id, AttendanceRecordId, Reason, RequestedBy, RequestedAt, Status, ApprovedBy, ApprovedAt, ApprovalComments, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
    VALUES
      ('11111111-aaaa-2222-cccc-111122223333', '77777777-7777-7777-7777-777777777777', 'Late submission; requesting reopen', '11111111-1111-1111-1111-111111111111', '2026-01-28T10:00:00Z', 'Pending', NULL, NULL, '', '2026-01-28T10:00:00Z', '11111111-1111-1111-1111-111111111111', '2026-01-28T10:00:00Z', '11111111-1111-1111-1111-111111111111');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT 'ERROR: ' + @ErrorMessage;
    THROW;
END CATCH
GO

-- End of seed script

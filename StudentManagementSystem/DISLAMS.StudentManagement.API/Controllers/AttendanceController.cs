using Microsoft.AspNetCore.Mvc;
using MediatR;
using DISLAMS.StudentManagement.Application.Commands;
using DISLAMS.StudentManagement.Application.DTOs;
using DISLAMS.StudentManagement.Application.Queries;
using DISLAMS.StudentManagement.Infrastructure.Data;

namespace DISLAMS.StudentManagement.API.Controllers
{
    /// <summary>
    /// Attendance API Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AttendanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AttendanceController(IMediator mediator, ApplicationDbContext dbContext)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Create/Mark Attendance
        /// Marks attendance for a student in a course (Draft state)
        /// 
        /// State: Draft
        /// Roles: Teacher
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateAttendance([FromBody] CreateAttendanceCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAttendance), new { attendanceId = result.Id }, result);
        }

        /// <summary>
        /// Submit Attendance
        /// Transitions attendance from Draft to Submitted
        /// 
        /// State: Draft → Submitted
        /// Roles: Teacher
        /// </summary>
        [HttpPost("{attendanceId}/submit")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitAttendance(
            Guid attendanceId,
            [FromBody] SubmitAttendanceCommand command)
        {
            command.AttendanceRecordId = attendanceId;

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Approve Attendance
        /// Transitions attendance from Submitted to Approved
        /// 
        /// State: Submitted → Approved
        /// Roles: AcademicCoordinator
        /// </summary>
        [HttpPost("{attendanceId}/approve")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveAttendance(
            Guid attendanceId,
            [FromBody] ApproveAttendanceCommand command)
        {
            command.AttendanceRecordId = attendanceId;

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Publish Attendance
        /// Transitions attendance from Approved to Published (locked for normal editing)
        /// 
        /// State: Approved → Published
        /// Roles: AcademicCoordinator
        /// </summary>
        [HttpPost("{attendanceId}/publish")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PublishAttendance(
            Guid attendanceId,
            [FromBody] PublishAttendanceCommand command)
        {
            command.AttendanceRecordId = attendanceId;

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Request Reopen
        /// Request to reopen a submitted/approved attendance for correction
        /// 
        /// State: Submitted|Approved → ReopenRequested
        /// Roles: Teacher, AcademicCoordinator
        /// </summary>
        [HttpPost("{attendanceId}/request-reopen")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestReopen(
            Guid attendanceId,
            [FromBody] RequestReopenCommand command)
        {
            command.AttendanceRecordId = attendanceId;

            var result = await _mediator.Send(command);
            return Ok(new { success = result, message = "Reopen request submitted" });
        }

        /// <summary>
        /// Approve Reopen
        /// Approve a reopen request and transition back to Draft
        /// 
        /// State: ReopenRequested → Draft
        /// Roles: AcademicCoordinator
        /// </summary>
        [HttpPost("reopen-request/{reopenRequestId}/approve")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveReopen(
            Guid reopenRequestId,
            [FromBody] ApproveReopenCommand command)
        {
            command.ReopenRequestId = reopenRequestId;

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Apply Correction
        /// Apply a correction to a published/approved attendance (creates new version)
        /// Original record marked as "Corrected", new version created as Draft
        /// 
        /// State: Published|Approved → Corrected (original), Draft (new version)
        /// Roles: AcademicCoordinator
        /// </summary>
        [HttpPost("{attendanceId}/apply-correction")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApplyCorrection(
            Guid attendanceId,
            [FromBody] ApplyCorrectionCommand command)
        {
            command.AttendanceRecordId = attendanceId;

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAttendance), new { attendanceId = result.Id }, result);
        }

        /// <summary>
        /// Lock Attendance
        /// Transition from Published to Locked (final immutable state)
        /// 
        /// State: Published → Locked
        /// Roles: AcademicCoordinator
        /// </summary>
        [HttpPost("{attendanceId}/lock")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LockAttendance(
            Guid attendanceId,
            [FromBody] LockAttendanceCommand command)
        {
            command.AttendanceRecordId = attendanceId;

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Get Attendance Record
        /// Retrieve a single attendance record with all details
        /// </summary>
        [HttpGet("{attendanceId}")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAttendance(Guid attendanceId)
        {
            var result = await _mediator.Send(new GetAttendanceQuery { AttendanceRecordId = attendanceId });
            return Ok(result);
        }

        /// <summary>
        /// Get Attendance by Student and Date
        /// Retrieve attendance for a specific student on a specific date for a course
        /// </summary>
        [HttpGet("student/{studentId}/date/{date}/course/{courseId}")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByStudentAndDate(Guid studentId, string date, Guid courseId)
        {
            if (!DateTime.TryParse(date, out var attendanceDate))
                return BadRequest(new { error = "Invalid date format" });

            var result = await _mediator.Send(new GetAttendanceByStudentDateQuery
            {
                StudentId = studentId,
                AttendanceDate = attendanceDate,
                CourseId = courseId
            });
            return Ok(result);
        }

        /// <summary>
        /// Get Student Attendance Range
        /// Retrieve all attendance for a student in a date range
        /// </summary>
        [HttpGet("student/{studentId}/range")]
        [ProducesResponseType(typeof(IEnumerable<AttendanceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentRange(
            Guid studentId,
            [FromQuery] string startDate,
            [FromQuery] string endDate)
        {
            if (!DateTime.TryParse(startDate, out var start) || !DateTime.TryParse(endDate, out var end))
                return BadRequest(new { error = "Invalid date format" });

            var result = await _mediator.Send(new GetStudentAttendanceRangeQuery
            {
                StudentId = studentId,
                StartDate = start,
                EndDate = end
            });
            return Ok(result);
        }

        /// <summary>
        /// Get Course Attendance for Date
        /// Retrieve all attendance for a course on a specific date
        /// </summary>
        [HttpGet("course/{courseId}/date/{date}")]
        [ProducesResponseType(typeof(IEnumerable<AttendanceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseForDate(Guid courseId, string date)
        {
            if (!DateTime.TryParse(date, out var attendanceDate))
                return BadRequest(new { error = "Invalid date format" });

            var result = await _mediator.Send(new GetCourseAttendanceForDateQuery
            {
                CourseId = courseId,
                AttendanceDate = attendanceDate
            });
            return Ok(result);
        }

        /// <summary>
        /// Get Attendance by Status
        /// Retrieve all attendance records in a specific state
        /// </summary>
        [HttpGet("status/{status}")]
        [ProducesResponseType(typeof(IEnumerable<AttendanceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByStatus(string status)
        {
            var result = await _mediator.Send(new GetAttendanceByStatusQuery { Status = status });
            return Ok(result);
        }

        /// <summary>
        /// Get Attendance Versions
        /// Retrieve all versions (including corrections) of an attendance record
        /// </summary>
        [HttpGet("versions/student/{studentId}/date/{date}/course/{courseId}")]
        [ProducesResponseType(typeof(IEnumerable<AttendanceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVersions(Guid studentId, string date, Guid courseId)
        {
            if (!DateTime.TryParse(date, out var attendanceDate))
                return BadRequest(new { error = "Invalid date format" });

            var result = await _mediator.Send(new GetAttendanceVersionsQuery
            {
                StudentId = studentId,
                AttendanceDate = attendanceDate,
                CourseId = courseId
            });
            return Ok(result);
        }

        /// <summary>
        /// Get Audit Trail
        /// Retrieve complete audit log for an attendance record
        /// Shows all state transitions and who performed them
        /// </summary>
        [HttpGet("{attendanceId}/audit-trail")]
        [ProducesResponseType(typeof(IEnumerable<AuditLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuditTrail(Guid attendanceId)
        {
            var result = await _mediator.Send(new GetAuditTrailQuery { AttendanceRecordId = attendanceId });
            return Ok(result);
        }
    }
}

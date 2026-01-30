using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DISLAMS.StudentManagement.Application.DTOs;
using DISLAMS.StudentManagement.Application.Queries;
using DISLAMS.StudentManagement.Domain.Enums;
using DISLAMS.StudentManagement.Domain.Repositories;
using DISLAMS.StudentManagement.Infrastructure.Data;

namespace DISLAMS.StudentManagement.Application.Handlers
{
    /// <summary>
    /// Get Attendance Query Handler
    /// </summary>
    public class GetAttendanceQueryHandler : IRequestHandler<GetAttendanceQuery, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IMapper _mapper;

        public GetAttendanceQueryHandler(IAttendanceRepository attendanceRepo, IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(GetAttendanceQuery request, CancellationToken cancellationToken)
        {
            var attendance = await _attendanceRepo.GetByIdAsync(request.AttendanceRecordId);
            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            return _mapper.Map<AttendanceDto>(attendance);
        }
    }

    /// <summary>
    /// Get Attendance by Student and Date Query Handler
    /// </summary>
    public class GetAttendanceByStudentDateQueryHandler : IRequestHandler<GetAttendanceByStudentDateQuery, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IMapper _mapper;

        public GetAttendanceByStudentDateQueryHandler(IAttendanceRepository attendanceRepo, IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(GetAttendanceByStudentDateQuery request, CancellationToken cancellationToken)
        {
            var attendance = await _attendanceRepo.GetByStudentDateCourseAsync(
                request.StudentId, request.AttendanceDate, request.CourseId);

            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            return _mapper.Map<AttendanceDto>(attendance);
        }
    }

    /// <summary>
    /// Get Student Attendance Range Query Handler
    /// </summary>
    public class GetStudentAttendanceRangeQueryHandler : IRequestHandler<GetStudentAttendanceRangeQuery, IEnumerable<AttendanceDto>>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IMapper _mapper;

        public GetStudentAttendanceRangeQueryHandler(IAttendanceRepository attendanceRepo, IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AttendanceDto>> Handle(GetStudentAttendanceRangeQuery request, CancellationToken cancellationToken)
        {
            var records = await _attendanceRepo.GetByStudentAndDateRangeAsync(
                request.StudentId, request.StartDate, request.EndDate);

            return _mapper.Map<IEnumerable<AttendanceDto>>(records);
        }
    }

    /// <summary>
    /// Get Course Attendance for Date Query Handler
    /// </summary>
    public class GetCourseAttendanceForDateQueryHandler : IRequestHandler<GetCourseAttendanceForDateQuery, IEnumerable<AttendanceDto>>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IMapper _mapper;

        public GetCourseAttendanceForDateQueryHandler(IAttendanceRepository attendanceRepo, IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AttendanceDto>> Handle(GetCourseAttendanceForDateQuery request, CancellationToken cancellationToken)
        {
            var records = await _attendanceRepo.GetByCourseAndDateAsync(request.CourseId, request.AttendanceDate);

            return _mapper.Map<IEnumerable<AttendanceDto>>(records);
        }
    }

    /// <summary>
    /// Get Attendance by Status Query Handler
    /// </summary>
    public class GetAttendanceByStatusQueryHandler : IRequestHandler<GetAttendanceByStatusQuery, IEnumerable<AttendanceDto>>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IMapper _mapper;

        public GetAttendanceByStatusQueryHandler(IAttendanceRepository attendanceRepo, IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AttendanceDto>> Handle(GetAttendanceByStatusQuery request, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<AttendanceStatus>(request.Status, out var status))
                throw new ArgumentException($"Invalid status: {request.Status}");

            var records = await _attendanceRepo.GetByStatusAsync(status);

            return _mapper.Map<IEnumerable<AttendanceDto>>(records);
        }
    }

    /// <summary>
    /// Get Attendance Versions Query Handler
    /// </summary>
    public class GetAttendanceVersionsQueryHandler : IRequestHandler<GetAttendanceVersionsQuery, IEnumerable<AttendanceDto>>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IMapper _mapper;

        public GetAttendanceVersionsQueryHandler(IAttendanceRepository attendanceRepo, IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AttendanceDto>> Handle(GetAttendanceVersionsQuery request, CancellationToken cancellationToken)
        {
            var versions = await _attendanceRepo.GetAllVersionsAsync(
                request.StudentId, request.AttendanceDate, request.CourseId);

            return _mapper.Map<IEnumerable<AttendanceDto>>(versions);
        }
    }

    /// <summary>
    /// Get Audit Trail Query Handler
    /// </summary>
    public class GetAuditTrailQueryHandler : IRequestHandler<GetAuditTrailQuery, IEnumerable<DTOs.AuditLogDto>>
    {
        private readonly IAuditLogRepository _auditRepo;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetAuditTrailQueryHandler(IAuditLogRepository auditRepo, ApplicationDbContext dbContext, IMapper mapper)
        {
            _auditRepo = auditRepo;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DTOs.AuditLogDto>> Handle(GetAuditTrailQuery request, CancellationToken cancellationToken)
        {
            var logs = await _auditRepo.GetByAttendanceRecordIdAsync(request.AttendanceRecordId);

            var dtos = new List<DTOs.AuditLogDto>();
            foreach (var log in logs)
            {
                var actor = await _dbContext.Actors.FindAsync(new object[] { log.ActorId }, cancellationToken: cancellationToken);
                
                dtos.Add(new DTOs.AuditLogDto
                {
                    Id = log.Id,
                    Action = log.Action,
                    PreviousStatus = log.PreviousStatus?.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    ActorName = actor?.FullName ?? "Unknown",
                    ActorRole = log.ActorRole.ToString(),
                    Reason = log.Reason,
                    ActionTimestamp = log.ActionTimestamp
                });
            }

            return dtos;
        }
    }
}

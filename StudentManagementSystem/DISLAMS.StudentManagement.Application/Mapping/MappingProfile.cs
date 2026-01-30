using AutoMapper;
using DISLAMS.StudentManagement.Application.Commands;
using DISLAMS.StudentManagement.Application.DTOs;
using DISLAMS.StudentManagement.Application.Queries;
using DISLAMS.StudentManagement.Domain.Entities;
using DISLAMS.StudentManagement.Infrastructure.Data;

namespace DISLAMS.StudentManagement.Application.Mapping
{
    /// <summary>
    /// AutoMapper Profile for DISLAMS Student Management
    /// </summary>
    public class MappingProfile : Profile
    {
        private readonly ApplicationDbContext _dbContext;

        public MappingProfile()
        {
            // Student mappings
            CreateMap<Student, StudentDto>().ReverseMap();

            // Course mappings
            CreateMap<Course, CourseDto>().ReverseMap();

            // Attendance mappings
            CreateMap<AttendanceRecord, AttendanceDto>()
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : ""))
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course != null ? src.Course.CourseName : ""))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<AttendanceDto, AttendanceRecord>();

            // Audit Log mappings
            CreateMap<AuditLog, AuditLogDto>()
                .ForMember(dest => dest.ActorRole, opt => opt.MapFrom(src => src.ActorRole.ToString()))
                .ForMember(dest => dest.PreviousStatus, opt => opt.MapFrom(src => src.PreviousStatus.ToString()))
                .ForMember(dest => dest.NewStatus, opt => opt.MapFrom(src => src.NewStatus.ToString()));

            // Command DTOs
            CreateMap<CreateAttendanceCommand, AttendanceRecord>();
        }
    }

    /// <summary>
    /// DTO for Student
    /// </summary>
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string ClassGrade { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for Course
    /// </summary>
    public class CourseDto
    {
        public Guid Id { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public Guid TeacherId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}

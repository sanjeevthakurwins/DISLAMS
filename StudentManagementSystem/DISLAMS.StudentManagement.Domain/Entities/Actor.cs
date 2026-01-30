using DISLAMS.StudentManagement.Domain.Enums;

namespace DISLAMS.StudentManagement.Domain.Entities
{
    /// <summary>
    /// Represents an Actor/User in the system with their role
    /// Tracks user identity and role for audit purposes
    /// </summary>
    public class Actor : Entity
    {
        /// <summary>
        /// External user ID (from authentication system)
        /// </summary>
        public string ExternalUserId { get; set; }

        /// <summary>
        /// User's full name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Role(s) assigned to this user
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// Whether the user is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// When the user was last active
        /// </summary>
        public DateTime? LastActiveAt { get; set; }

        public Actor()
        {
            ExternalUserId = string.Empty;
            FullName = string.Empty;
            Email = string.Empty;
            IsActive = true;
        }

        public Actor(string externalUserId, string fullName, string email, UserRole role) : this()
        {
            ExternalUserId = externalUserId;
            FullName = fullName;
            Email = email;
            Role = role;
        }
    }
}

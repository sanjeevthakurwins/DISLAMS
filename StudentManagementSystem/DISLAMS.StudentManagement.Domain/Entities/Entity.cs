namespace DISLAMS.StudentManagement.Domain.Entities
{
    /// <summary>
    /// Immutable base entity for domain objects
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// When this entity was created (immutable)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Who created this entity (immutable)
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// When this entity was last modified
        /// Note: This is NOT modified for attendance records - new versions are created instead
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Who last modified this entity
        /// </summary>
        public Guid ModifiedBy { get; set; }

        public Entity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }
    }
}

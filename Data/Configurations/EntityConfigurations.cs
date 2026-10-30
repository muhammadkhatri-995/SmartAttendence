using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAttendance.Models;
using SmartAttendance.Web.Models;

namespace SmartAttendance.Web.Data.Configurations;

internal static class ConfigHelpers
{
    /// <summary>Builds: "Column" IN ('A','B',...) from an enum's names.</summary>
    public static string EnumIn<TEnum>(string column) where TEnum : struct, Enum =>
        $"\"{column}\" IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})";
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
    }
}

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> b)
    {
        b.ToTable("Teachers", t =>
        {
            t.HasCheckConstraint("CK_Teachers_PhoneNumber_E164", @"""PhoneNumber"" ~ '^\+[1-9][0-9]{7,14}$'");
            t.HasCheckConstraint("CK_Teachers_Email_Lower", @"""Email"" = LOWER(""Email"")");
        });

        b.HasKey(x => x.TeacherId);
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();

        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.FullName);
        b.HasIndex(x => x.ApplicationUserId).IsUnique(); // multiple NULLs are allowed in PostgreSQL

        // One-to-one (optional). Deleting a user never deletes the teacher profile.
        b.HasOne(x => x.ApplicationUser)
         .WithOne(u => u.TeacherProfile)
         .HasForeignKey<Teacher>(x => x.ApplicationUserId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subjects", t =>
            t.HasCheckConstraint("CK_Subjects_Code_Upper", @"""SubjectCode"" = UPPER(""SubjectCode"")"));

        b.HasKey(x => x.SubjectId);
        b.Property(x => x.SubjectCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.SubjectName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);

        b.HasIndex(x => x.SubjectCode).IsUnique();
        b.HasIndex(x => x.SubjectName);
    }
}

public class ClassRoomConfiguration : IEntityTypeConfiguration<ClassRoom>
{
    public void Configure(EntityTypeBuilder<ClassRoom> b)
    {
        b.ToTable("ClassRooms", t =>
            t.HasCheckConstraint("CK_ClassRooms_Semester", @"""Semester"" BETWEEN 1 AND 12"));

        b.HasKey(x => x.ClassRoomId);
        b.Property(x => x.ClassName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Section).HasMaxLength(20).IsRequired();
        b.Property(x => x.AcademicYear).HasMaxLength(20).IsRequired();

        b.HasIndex(x => new { x.ClassName, x.Section, x.AcademicYear }).IsUnique();
    }
}

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.HasKey(x => x.StudentId);
        b.Property(x => x.RollNumber).HasMaxLength(30).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256);

        b.HasIndex(x => new { x.ClassRoomId, x.RollNumber }).IsUnique();
        b.HasIndex(x => x.RollNumber);
        b.HasIndex(x => new { x.ClassRoomId, x.IsActive });

        b.HasOne(x => x.ClassRoom).WithMany(c => c.Students)
         .HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> b)
    {
        b.HasKey(x => x.TeacherAssignmentId);
        b.Property(x => x.AcademicYear).HasMaxLength(20).IsRequired();

        b.HasIndex(x => new { x.TeacherId, x.SubjectId, x.ClassRoomId, x.AcademicYear }).IsUnique();
        b.HasIndex(x => x.SubjectId);
        b.HasIndex(x => x.ClassRoomId);

        b.HasOne(x => x.Teacher).WithMany(t => t.TeacherAssignments)
         .HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subject).WithMany(s => s.TeacherAssignments)
         .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ClassRoom).WithMany(c => c.TeacherAssignments)
         .HasForeignKey(x => x.ClassRoomId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OperatorAssignmentConfiguration : IEntityTypeConfiguration<OperatorAssignment>
{
    public void Configure(EntityTypeBuilder<OperatorAssignment> b)
    {
        b.HasKey(x => x.OperatorAssignmentId);

        b.HasIndex(x => new { x.UserId, x.TeacherAssignmentId }).IsUnique();
        b.HasIndex(x => x.TeacherAssignmentId);

        b.HasOne(x => x.User).WithMany()
         .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TeacherAssignment).WithMany(a => a.OperatorAssignments)
         .HasForeignKey(x => x.TeacherAssignmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> b)
    {
        b.ToTable("AttendanceSessions", t =>
        {
            t.HasCheckConstraint("CK_AttendanceSessions_Status", ConfigHelpers.EnumIn<AttendanceStatus>("Status"));
            t.HasCheckConstraint("CK_AttendanceSessions_SubmittedAt",
                @"""Status"" <> 'Submitted' OR ""SubmittedAt"" IS NOT NULL");
        });

        b.HasKey(x => x.AttendanceSessionId);
        b.Property(x => x.AttendanceDate).HasColumnType("date");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        // Maps to PostgreSQL's xmin system column (optimistic concurrency).
        b.Property(x => x.Version).IsRowVersion();

        b.HasIndex(x => new { x.TeacherAssignmentId, x.AttendanceDate }).IsUnique();
        b.HasIndex(x => x.AttendanceDate);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedByUserId);

        b.HasOne(x => x.TeacherAssignment).WithMany(a => a.AttendanceSessions)
         .HasForeignKey(x => x.TeacherAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany()
         .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.HasKey(x => x.AttendanceRecordId);

        b.HasIndex(x => new { x.AttendanceSessionId, x.StudentId }).IsUnique();
        b.HasIndex(x => x.StudentId);

        b.HasOne(x => x.AttendanceSession).WithMany(s => s.Records)
         .HasForeignKey(x => x.AttendanceSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Student).WithMany(s => s.AttendanceRecords)
         .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceCorrectionConfiguration : IEntityTypeConfiguration<AttendanceCorrection>
{
    public void Configure(EntityTypeBuilder<AttendanceCorrection> b)
    {
        b.HasKey(x => x.AttendanceCorrectionId);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();

        b.HasIndex(x => x.AttendanceSessionId);
        b.HasIndex(x => x.StudentId);
        b.HasIndex(x => x.CorrectedAt);

        b.HasOne(x => x.AttendanceSession).WithMany(s => s.Corrections)
         .HasForeignKey(x => x.AttendanceSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Student).WithMany()
         .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CorrectedByUser).WithMany()
         .HasForeignKey(x => x.CorrectedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> b)
    {
        b.ToTable("ImportBatches", t =>
        {
            t.HasCheckConstraint("CK_ImportBatches_Type", ConfigHelpers.EnumIn<ImportType>("ImportType"));
            t.HasCheckConstraint("CK_ImportBatches_Status", ConfigHelpers.EnumIn<ImportStatus>("Status"));
            t.HasCheckConstraint("CK_ImportBatches_Counts",
                @"""TotalRows"" >= 0 AND ""SuccessfulRows"" >= 0 AND ""FailedRows"" >= 0 " +
                @"AND ""SuccessfulRows"" + ""FailedRows"" <= ""TotalRows""");
        });

        b.HasKey(x => x.ImportBatchId);
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ImportType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => x.ImportedAt);
        b.HasIndex(x => x.ImportedByUserId);

        b.HasOne(x => x.ImportedByUser).WithMany()
         .HasForeignKey(x => x.ImportedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> b)
    {
        b.ToTable("NotificationLogs", t =>
        {
            t.HasCheckConstraint("CK_NotificationLogs_Status", ConfigHelpers.EnumIn<NotificationStatus>("Status"));
            t.HasCheckConstraint("CK_NotificationLogs_MessageType",
                ConfigHelpers.EnumIn<NotificationMessageType>("MessageType"));
        });

        b.HasKey(x => x.NotificationLogId);
        b.Property(x => x.RecipientPhone).HasMaxLength(20).IsRequired();
        b.Property(x => x.MessageType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ProviderMessageId).HasMaxLength(128);
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);

        // At most one non-failed notification per session/recipient/type: prevents duplicate sends,
        // while a Failed row can still be retried.
        b.HasIndex(x => new { x.AttendanceSessionId, x.RecipientPhone, x.MessageType })
         .IsUnique()
         .HasFilter(@"""Status"" <> 'Failed'");

        b.HasIndex(x => x.ProviderMessageId);          // webhook lookups
        b.HasIndex(x => new { x.Status, x.NextRetryAt });
        b.HasIndex(x => x.CreatedAt);

        b.HasOne(x => x.AttendanceSession).WithMany(s => s.Notifications)
         .HasForeignKey(x => x.AttendanceSessionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> b)
    {
        b.HasKey(x => x.Key);
        b.Property(x => x.Key).HasMaxLength(100);
        b.Property(x => x.Value).HasMaxLength(1000).IsRequired();
    }
}
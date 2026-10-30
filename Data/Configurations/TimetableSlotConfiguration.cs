using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAttendance.Web.Models;

namespace SmartAttendance.Web.Data.Configurations;

public class TimetableSlotConfiguration : IEntityTypeConfiguration<TimetableSlot>
{
    public void Configure(EntityTypeBuilder<TimetableSlot> b)
    {
        b.ToTable("TimetableSlots", t =>
            t.HasCheckConstraint("CK_TimetableSlots_Day", @"""DayOfWeek"" BETWEEN 0 AND 6"));

        b.HasKey(x => x.TimetableSlotId);
        b.HasIndex(x => new { x.DayOfWeek, x.StartTime });
        b.HasIndex(x => x.TeacherAssignmentId);

        b.HasOne(x => x.TeacherAssignment).WithMany()
         .HasForeignKey(x => x.TeacherAssignmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
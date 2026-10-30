namespace SmartAttendance.Web.Models
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Teacher = "Teacher";
        public const string AttendanceOperator = "AttendanceOperator";


        public static readonly string[] All = { Admin, Teacher, AttendanceOperator };
    }
}

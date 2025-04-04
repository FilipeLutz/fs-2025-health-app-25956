namespace HealthApp.Domain
{
    public class Patient
    {
        public string Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public object Name { get; internal set; }
        public string UserId { get; internal set; }
    }
}
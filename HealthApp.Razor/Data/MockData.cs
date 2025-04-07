using Bogus;
using HealthApp.Domain.Entities;

namespace HealthApp.Razor.Data
{

    public static class MockData
    {
        public static List<Patient> Patients()
        {
            List<Patient> patients = new();

            var faker = new Faker();

            for (int i = 0; i < 100; i++)
            {
                patients.Add(new Patient
                {
                    Id = i,
                    FirstName = faker.Name.FirstName(),
                    LastName = faker.Name.LastName(),
                    Email = faker.Internet.Email(),
                    Address = faker.Address.FullAddress(),
                    DateOfBirth = faker.Date.Past(30, DateTime.Now),
                    BloodType = faker.PickRandom(new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" }),
                    PhoneNumber = faker.Phone.PhoneNumber(),
                    Appointments = new List<Appointment>(),
                    InsuranceInfo = faker.Internet.UserName(),
                    MedicalHistory = faker.Lorem.Paragraph(),
                });
            }

            return patients;
        }
    }
}

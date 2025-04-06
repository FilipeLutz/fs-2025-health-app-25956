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
                    Id = i + 1,
                    UserId = faker.Internet.UserName(),
                    Name = faker.Name.FirstName(),
                    Email = faker.Internet.Email().ToLower()
                });
            }

            return patients;
        }
    }
}

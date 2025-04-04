using Bogus;
using HealthApp.Domain;

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
                    Id = Guid.NewGuid().ToString(),
                    FirstName = faker.Name.FirstName(),
                    LastName = faker.Name.LastName(),
                    Email = faker.Internet.Email().ToLower()
                });
            }

            return patients;

            //return new List<Patient>
            //{
            //    new Patient
            //    {
            //        Id = Guid.NewGuid().ToString(),
            //        FirstName = "John",
            //        LastName = "Doe",
            //      //  DateOfBirth = new DateTime(1980, 1, 1)
            //    }
            //};
        }
    }
}

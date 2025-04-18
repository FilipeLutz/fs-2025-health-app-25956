using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using HealthApp.Domain.Services;
using Moq;
using Microsoft.EntityFrameworkCore;

namespace HealthApp.Tests
{
    public class AppointmentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly IAppointmentRepository _appointmentService;
        private readonly DateTime _referenceDate = new(2023, 1, 1, 9, 0, 0);

        public AppointmentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockNotificationService = new Mock<INotificationService>();
            _appointmentService = new AppointmentService(_context, _mockNotificationService.Object);

            SeedTestDataAsync().Wait();
        }

        private async Task SeedTestDataAsync()
        {
            if (!await _context.Doctors.AnyAsync())
            {
                var doctors = new List<Doctor>
                {
                    CreateTestDoctor(1, "Test Doctor 1", "1", "Cardiology", "doctor1@test.com", "John", "Doe", "1234567890"),
                    CreateTestDoctor(2, "Test Doctor 2", "2", "Neurology", "doctor2@test.com", "Jane", "Smith", "0987654321")
                };
                await _context.Doctors.AddRangeAsync(doctors);

                var patients = new List<Patient>
                {
                    CreateTestPatient(1, "Test Patient 1", "3", "patient1@test.com", "Mike", "Johnson", "1112223333", "123 Main St", "None", "O+", "Blue Cross", "No significant history"),
                    CreateTestPatient(2, "Test Patient 2", "4", "patient2@test.com", "Sarah", "Williams", "4445556666", "456 Oak Ave", "Penicillin", "A-", "Aetna", "Diabetes type 2")
                };
                await _context.Patients.AddRangeAsync(patients);

                var appointments = new List<Appointment>
                {
                    CreateTestAppointment(1, 1, 1, _referenceDate.AddDays(1)),
                    CreateTestAppointment(2, 2, 1, _referenceDate.AddDays(2)),
                    CreateTestAppointment(3, 1, 2, _referenceDate.AddDays(1))
                };
                await _context.Appointments.AddRangeAsync(appointments);

                await _context.SaveChangesAsync();
            }
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Test Data Factories
        private Doctor CreateTestDoctor(int id, string name, string userId, string specialization, string email, string firstName, string lastName, string phoneNumber)
        {
            return new Doctor
            {
                Id = id,
                UserId = userId,
                Specialization = specialization,
                LicenseNumber = $"DOC{id}2345",
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
            };
        }

        private Patient CreateTestPatient(int id, string name, string userId, string email, string firstName, string lastName, string phoneNumber,
            string address, string allergies, string bloodType, string insuranceInfo, string medicalHistory)
        {
            return new Patient
            {
                Id = id,
                UserId = userId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Address = address,
                Allergies = allergies,
                BloodType = bloodType,
                InsuranceInfo = insuranceInfo,
                MedicalHistory = medicalHistory,
                DateOfBirth = new DateTime(1980, 1, 1).AddYears(-id)
            };
        }

        private Appointment CreateTestAppointment(int id, int patientId, int doctorId, DateTime date)
        {
            return new Appointment
            {
                Id = id,
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDateTime = date,
                EndDateTime = date.AddMinutes(30),
                Status = "Approved",
                Reason = id switch
                {
                    1 => "Annual checkup",
                    2 => "Follow-up visit",
                    3 => "Neurology consultation",
                    _ => "General consultation"
                },
                Notes = id switch
                {
                    1 => "Routine physical examination",
                    2 => "Review test results",
                    3 => "Headache evaluation",
                    _ => "General notes"
                },
                CancellationReason = ""
            };
        }
        #endregion

        #region Add Tests
        [Fact]
        public async Task AddAsync_ShouldAddAppointment_WhenTimeSlotAvailable()
        {
            var appointment = CreateTestAppointment(4, 1, 1, _referenceDate.AddDays(3));

            await _appointmentService.AddAsync(appointment);
            var result = await _appointmentService.GetByIdAsync(appointment.Id);

            Assert.NotNull(result);
            Assert.Equal(1, result.PatientId);
            Assert.Equal(1, result.DoctorId);
            _mockNotificationService.Verify(
                x => x.SendAppointmentConfirmationAsync(It.Is<Appointment>(a => a.Id == appointment.Id)),
                Times.Once);
        }

        [Fact]
        public async Task AddAsync_ShouldThrow_WhenTimeSlotNotAvailable()
        {
            var newAppointment = CreateTestAppointment(4, 2, 1, _referenceDate.AddDays(1));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _appointmentService.AddAsync(newAppointment));
            _mockNotificationService.Verify(x => x.SendAppointmentConfirmationAsync(It.IsAny<Appointment>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_ShouldThrow_WhenDoctorDoesNotExist()
        {
            var appointment = CreateTestAppointment(4, 1, 99, _referenceDate.AddDays(3));
            await Assert.ThrowsAsync<ArgumentException>(() => _appointmentService.AddAsync(appointment));
        }

        [Fact]
        public async Task AddAsync_ShouldThrow_WhenPatientDoesNotExist()
        {
            var appointment = CreateTestAppointment(4, 99, 1, _referenceDate.AddDays(3));
            await Assert.ThrowsAsync<ArgumentException>(() => _appointmentService.AddAsync(appointment));
        }
        #endregion

        #region Cancellation Tests
        [Fact]
        public async Task CancelAppointment_WhenWithin48Hours_ShouldNotAllowCancellation()
        {
            var appointment = CreateTestAppointment(4, 1, 2, DateTime.Now.AddHours(47));
            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            var result = await _appointmentService.CancelAppointmentAsync(appointment.Id, "Reason");

            Assert.False(result);
            var dbAppointment = await _context.Appointments.FindAsync(appointment.Id);
            Assert.NotEqual("Cancelled", dbAppointment?.Status);
            _mockNotificationService.Verify(x => x.SendCancellationNotificationAsync(It.IsAny<Appointment>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CancelAppointment_WhenOutside48Hours_ShouldAllowCancellation()
        {
            var appointment = CreateTestAppointment(4, 2, 1, DateTime.Now.AddHours(49));
            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            var result = await _appointmentService.CancelAppointmentAsync(appointment.Id, "Valid Reason");

            Assert.True(result);
            var dbAppointment = await _context.Appointments.FindAsync(appointment.Id);
            Assert.Equal("Cancelled", dbAppointment?.Status);
            Assert.Equal("Valid Reason", dbAppointment?.CancellationReason);
            _mockNotificationService.Verify(x => x.SendAppointmentCancellationAsync(It.Is<Appointment>(a => a.Id == appointment.Id)), Times.Once);
        }

        [Fact]
        public async Task CancelAppointment_WhenAppointmentNotFound_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _appointmentService.CancelAppointmentAsync(999, "Reason"));
        }
        #endregion

        #region Query Tests
        [Fact]
        public async Task GetByDoctorIdAsync_ShouldReturnOnlyDoctorsAppointments()
        {
            var result = await _appointmentService.GetByDoctorIdAsync(1);

            Assert.Equal(2, result.Count());
            Assert.All(result, a => Assert.Equal(1, a.DoctorId));
        }

        [Fact]
        public async Task GetByPatientIdAsync_ShouldReturnOnlyPatientsAppointments()
        {
            var result = await _appointmentService.GetByPatientIdAsync(1);

            Assert.Equal(2, result.Count());
            Assert.All(result, a => Assert.Equal(1, a.PatientId));
        }

        [Fact]
        public async Task GetByIdAsync_WhenAppointmentExists_ShouldReturnAppointment()
        {
            var result = await _appointmentService.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WhenAppointmentDoesNotExist_ShouldReturnNull()
        {
            var result = await _appointmentService.GetByIdAsync(999);

            Assert.Null(result);
        }
        #endregion

        #region Update Tests
        [Fact]
        public async Task UpdateAsync_ShouldUpdateAppointment_WhenValid()
        {
            var originalAppointment = CreateTestAppointment(4, 2, 2, _referenceDate.AddDays(4));
            await _context.Appointments.AddAsync(originalAppointment);
            await _context.SaveChangesAsync();
            _context.Entry(originalAppointment).State = EntityState.Detached;

            var updatedAppointment = CreateTestAppointment(4, 2, 2, _referenceDate.AddDays(5));
            updatedAppointment.Status = "Approved";
            updatedAppointment.Notes = "Updated notes";

            await _appointmentService.UpdateAsync(updatedAppointment);
            var result = await _appointmentService.GetByIdAsync(4);

            Assert.NotNull(result);
            Assert.Equal(_referenceDate.AddDays(5), result.AppointmentDateTime);
            Assert.Equal("Approved", result.Status);
            Assert.Equal("Updated notes", result.Notes);
        }

        [Fact]
        public async Task UpdateAsync_WhenAppointmentNotFound_ShouldThrow()
        {
            var nonExistentAppointment = CreateTestAppointment(999, 1, 1, _referenceDate.AddDays(10));

            await Assert.ThrowsAsync<ArgumentException>(() => _appointmentService.UpdateAsync(nonExistentAppointment));
        }
        #endregion
    }
}
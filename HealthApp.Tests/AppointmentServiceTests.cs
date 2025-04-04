using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using HealthApp.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HealthApp.Tests;

public class AppointmentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly IAppointmentRepository _appointmentService;
    private readonly DateTime _referenceDate = new DateTime(2023, 1, 1, 9, 0, 0);
    private List<Patient> patients;

    public AppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockNotificationService = new Mock<INotificationService>();
        _appointmentService = new AppointmentService(_context, _mockNotificationService.Object);

        SeedTestDataAsync(patients).Wait();
    }

    private async Task SeedTestDataAsync(List<Patient>? patients)
    {
        if (!await _context.Doctors.AnyAsync())
        {
            // Seed doctors
            var doctors = new List<Doctor>
        {
            CreateTestDoctor(1, "Test Doctor 1", "1", "Cardiology"),
            CreateTestDoctor(2, "Test Doctor 2", "2", "Neurology")
        };
            await _context.Doctors.AddRangeAsync(doctors);

            // Seed patients
            await _context.Patients.AddRangeAsync((IEnumerable<Domain.Patient>)new List<Patient>
        {
            CreateTestPatient(1, "Test Patient 1", "3"),
            CreateTestPatient(2, "Test Patient 2", "4")
        });

            // Seed appointments - now properly including related entities
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
    private Doctor CreateTestDoctor(int id, string name, string userId, string specialization)
    {
        return new Doctor
        {
            Id = id,
            Name = name,
            UserId = userId,
            Specialization = specialization,
            Email = $"{name.ToLower().Replace(" ", "")}@test.com",
            LicenseNumber = $"DOC{id}2345",
            HospitalAffiliation = "Test Hospital"
        };
    }

    private Patient CreateTestPatient(int id, string name, string userId)
    {
        return new Patient
        {
            Id = id,
            Name = name,
            UserId = userId,
            Email = $"{name.ToLower().Replace(" ", "")}@test.com",
            PhoneNumber = $"111222{id:0000}",
            Address = $"{id}23 Test St, Test City",
            Allergies = id == 2 ? "Penicillin" : "None",
            BloodType = id == 2 ? "A-" : "O+",
            DateOfBirth = new DateTime(1980 + (id * 10), 1, 1)
        };
    }

    private Appointment CreateTestAppointment(int id, int patientId, int doctorId, DateTime date)
    {
        // First get or create the related entities
        var patient = _context.Patients.Find(patientId) ?? CreateTestPatient(patientId, $"Test Patient {patientId}", $"{patientId}");
        var doctor = _context.Doctors.Find(doctorId) ?? CreateTestDoctor(doctorId, $"Test Doctor {doctorId}", $"{doctorId}", "General");
        if (patient == null)
        {
            patient = CreateTestPatient(patientId, $"Test Patient {patientId}", $"{patientId}");
        }

        if (doctor == null)
        {
            doctor = CreateTestDoctor(doctorId, $"Test Doctor {doctorId}", $"{doctorId}", "General");
        }

        return new Appointment
        {
            Id = id,
            PatientId = patientId,
            DoctorId = doctorId,
            Doctor = doctor,
            Patient = patient,
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

    #region CRUD Tests
    [Fact]
    public async Task AddAsync_ShouldAddAppointment_WhenTimeSlotAvailable()
    {
        // Arrange
        var appointment = CreateTestAppointment(4, 1, 1, _referenceDate.AddDays(3));

        // Act
        await _appointmentService.AddAsync(appointment);
        var result = await _appointmentService.GetByIdAsync(appointment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PatientId);
        Assert.Equal(1, result.DoctorId);
        Assert.Equal("Pending", result.Status);
        _mockNotificationService.Verify(
            x => x.SendAppointmentConfirmationAsync(It.Is<Appointment>(a => a.Id == appointment.Id)),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenTimeSlotNotAvailable()
    {
        // Arrange
        var newAppointment = CreateTestAppointment(4, 2, 1, _referenceDate.AddDays(1));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _appointmentService.AddAsync(newAppointment));
        _mockNotificationService.Verify(
            x => x.SendAppointmentConfirmationAsync(It.IsAny<Appointment>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenDoctorDoesNotExist()
    {
        // Arrange
        var appointment = CreateTestAppointment(4, 1, 99, _referenceDate.AddDays(3));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.AddAsync(appointment));
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenPatientDoesNotExist()
    {
        // Arrange
        var appointment = CreateTestAppointment(4, 99, 1, _referenceDate.AddDays(3));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.AddAsync(appointment));
    }
    #endregion

    #region Cancellation Tests
    [Fact]
    public async Task CancelAppointment_WhenWithin48Hours_ShouldNotAllowCancellation()
    {
        // Arrange
        var appointment = CreateTestAppointment(4, 1, 2, DateTime.Now.AddHours(47));
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _appointmentService.CancelAppointmentAsync(appointment.Id, "Reason");

        // Assert
        Assert.False(result);
        var dbAppointment = await _context.Appointments.FindAsync(appointment.Id);
        Assert.NotEqual("Cancelled", dbAppointment?.Status);
        _mockNotificationService.Verify(
            x => x.SendCancellationNotificationAsync(It.IsAny<Appointment>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAppointment_WhenOutside48Hours_ShouldAllowCancellation()
    {
        // Arrange
        var appointment = CreateTestAppointment(4, 2, 1, DateTime.Now.AddHours(49));
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _appointmentService.CancelAppointmentAsync(appointment.Id, "Valid Reason");

        // Assert
        Assert.True(result);
        var dbAppointment = await _context.Appointments.FindAsync(appointment.Id);
        Assert.Equal("Cancelled", dbAppointment?.Status);
        Assert.Equal("Valid Reason", dbAppointment?.CancellationReason);
        _mockNotificationService.Verify(
            x => x.SendAppointmentCancellationAsync(It.Is<Appointment>(a => a.Id == appointment.Id)),
            Times.Once);
    }

    [Fact]
    public async Task CancelAppointment_WhenAppointmentNotFound_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.CancelAppointmentAsync(999, "Reason"));
    }
    #endregion

    #region Query Tests
    [Fact]
    public async Task GetByDoctorIdAsync_ShouldReturnOnlyDoctorsAppointments()
    {
        // Act
        var result = await _appointmentService.GetByDoctorIdAsync(1);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, a => Assert.Equal(1, a.DoctorId));
    }

    [Fact]
    public async Task GetByPatientIdAsync_ShouldReturnOnlyPatientsAppointments()
    {
        // Act
        var result = await _appointmentService.GetByPatientIdAsync(1);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, a => Assert.Equal(1, a.PatientId));
    }

    [Fact]
    public async Task GetByIdAsync_WhenAppointmentExists_ShouldReturnAppointment()
    {
        // Act
        var result = await _appointmentService.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAppointmentDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _appointmentService.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }
    #endregion

    #region Update Tests
    [Fact]
    public async Task UpdateAsync_ShouldUpdateAppointment_WhenValid()
    {
        // Arrange
        var originalAppointment = CreateTestAppointment(4, 2, 2, _referenceDate.AddDays(4));
        originalAppointment.Patient = CreateTestPatient(2, "Test Patient 2", "4");
        originalAppointment.Doctor = CreateTestDoctor(2, "Test Doctor 2", "2", "Neurology");

        await _context.Appointments.AddAsync(originalAppointment);
        await _context.SaveChangesAsync();
        _context.Entry(originalAppointment).State = EntityState.Detached;

        var updatedAppointment = CreateTestAppointment(4, 2, 2, _referenceDate.AddDays(5));
        updatedAppointment.Status = "Approved";
        updatedAppointment.Notes = "Updated notes";
        updatedAppointment.Patient = originalAppointment.Patient;
        updatedAppointment.Doctor = originalAppointment.Doctor;

        // Act
        await _appointmentService.UpdateAsync(updatedAppointment);
        var result = await _appointmentService.GetByIdAsync(4);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_referenceDate.AddDays(5), result.AppointmentDateTime);
        Assert.Equal("Approved", result.Status);
        Assert.Equal("Updated notes", result.Notes);
    }

    [Fact]
    public async Task UpdateAsync_WhenAppointmentNotFound_ShouldThrow()
    {
        // Arrange
        var appointment = CreateTestAppointment(999, 1, 1, _referenceDate.AddDays(1));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _appointmentService.UpdateAsync(appointment));
    }
    #endregion
}
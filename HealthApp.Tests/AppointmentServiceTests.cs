using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using HealthApp.Domain.Services;
using HealthApp.Razor.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HealthApp.Tests;

public class AppointmentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly IAppointmentRepository _appointmentService;

    public AppointmentServiceTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _context = new ApplicationDbContext(options);
        _mockNotificationService = new Mock<INotificationService>();
        _appointmentService = new AppointmentService(_context, _mockNotificationService.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        if (!_context.Doctors.Any())
        {
            // Seed doctors and patients (same as before)
            _context.Doctors.AddRange(
                new Doctor
                {
                    Id = 1,
                    Name = "Test Doctor 1",
                    UserId = "1",
                    Specialization = "Cardiology",
                    Email = "doctor1@test.com",
                    LicenseNumber = "DOC12345",
                    HospitalAffiliation = "Test Hospital"
                },
                new Doctor
                {
                    Id = 2,
                    Name = "Test Doctor 2",
                    UserId = "2",
                    Specialization = "Neurology",
                    Email = "doctor2@test.com",
                    LicenseNumber = "DOC67890",
                    HospitalAffiliation = "Test Hospital"
                }
            );

            _context.Patients.AddRange(
                new Patient
                {
                    Id = 1,
                    Name = "Test Patient 1",
                    UserId = "3",
                    Email = "patient1@test.com",
                    PhoneNumber = "1112223333",
                    Address = "123 Test St, Test City",
                    Allergies = "None",
                    BloodType = "O+",
                    DateOfBirth = new DateTime(1980, 1, 1)
                },
                new Patient
                {
                    Id = 2,
                    Name = "Test Patient 2",
                    UserId = "4",
                    Email = "patient2@test.com",
                    PhoneNumber = "4445556666",
                    Address = "456 Test Ave, Test City",
                    Allergies = "Penicillin",
                    BloodType = "A-",
                    DateOfBirth = new DateTime(1990, 1, 1)
                }
            );

            // Seed appointments with all required fields
            _context.Appointments.AddRange(
                new Appointment
                {
                    Id = 1,
                    PatientId = 1,
                    DoctorId = 1,
                    AppointmentDateTime = DateTime.Now.AddDays(1),
                    EndDateTime = DateTime.Now.AddDays(1).AddMinutes(30),
                    Status = "Approved",
                    Reason = "Annual checkup",
                    Notes = "Routine physical examination",
                    CancellationReason = ""
                },
                new Appointment
                {
                    Id = 2,
                    PatientId = 2,
                    DoctorId = 1,
                    AppointmentDateTime = DateTime.Now.AddDays(2),
                    EndDateTime = DateTime.Now.AddDays(2).AddMinutes(30),
                    Status = "Approved",
                    Reason = "Follow-up visit",
                    Notes = "Review test results",
                    CancellationReason = ""
                },
                new Appointment
                {
                    Id = 3,
                    PatientId = 1,
                    DoctorId = 2,
                    AppointmentDateTime = DateTime.Now.AddDays(1),
                    EndDateTime = DateTime.Now.AddDays(1).AddMinutes(30),
                    Status = "Approved",
                    Reason = "Neurology consultation",
                    Notes = "Headache evaluation",
                    CancellationReason = ""
                }
            );

            _context.SaveChanges();
        }
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddAppointment_WhenTimeSlotAvailable()
    {
        // Arrange
        var appointment = new Appointment
        {
            PatientId = 1,
            DoctorId = 1,
            AppointmentDateTime = DateTime.Now.AddDays(3), // New time slot
            EndDateTime = DateTime.Now.AddDays(3).AddMinutes(30),
            Status = "Pending",
            Reason = "New consultation",
            Notes = "Initial visit",
            CancellationReason = ""
        };

        // Act
        await _appointmentService.AddAsync(appointment);
        var result = await _appointmentService.GetByIdAsync(appointment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PatientId);
        Assert.Equal(1, result.DoctorId);
        Assert.Equal("Pending", result.Status);
        _mockNotificationService.Verify(x => x.SendAppointmentConfirmationAsync(It.IsAny<Appointment>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenTimeSlotNotAvailable()
    {
        // Arrange
        var newAppointment = new Appointment
        {
            PatientId = 2,
            DoctorId = 1,
            AppointmentDateTime = DateTime.Now.AddDays(1), // Conflicts with seeded appointment
            EndDateTime = DateTime.Now.AddDays(1).AddMinutes(30),
            Status = "Pending",
            Reason = "Urgent visit",
            Notes = "Patient having symptoms",
            CancellationReason = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _appointmentService.AddAsync(newAppointment));
        _mockNotificationService.Verify(x => x.SendAppointmentConfirmationAsync(It.IsAny<Appointment>()), Times.Never);
    }

    [Fact]
    public async Task CancelAppointmentAsync_ShouldReturnFalse_WhenWithin48Hours()
    {
        // Arrange
        var appointment = new Appointment
        {
            PatientId = 1,
            DoctorId = 2,
            AppointmentDateTime = DateTime.Now.AddHours(47), // Within 48 hours
            EndDateTime = DateTime.Now.AddHours(47).AddMinutes(30),
            Status = "Approved",
            Reason = "Emergency visit",
            Notes = "Patient in pain",
            CancellationReason = ""
        };
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _appointmentService.CancelAppointmentAsync(appointment.Id, "Reason");

        // Assert
        Assert.False(result);
        _mockNotificationService.Verify(x => x.SendCancellationNotificationAsync(It.IsAny<Appointment>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelAppointmentAsync_ShouldReturnTrue_WhenOutside48Hours()
    {
        // Arrange
        var appointment = new Appointment
        {
            PatientId = 2,
            DoctorId = 1,
            AppointmentDateTime = DateTime.Now.AddHours(49), 
            EndDateTime = DateTime.Now.AddHours(49).AddMinutes(30),
            Status = "Approved",
            Reason = "Follow-up",
            Notes = "Post-surgery check",
            CancellationReason = ""
        };
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _appointmentService.CancelAppointmentAsync(appointment.Id, "Valid Reason");

        // Assert
        Assert.True(result);
        var cancelledAppointment = await _context.Appointments.FindAsync(appointment.Id);
        Assert.Equal("Cancelled", cancelledAppointment.Status);
        Assert.Equal("Valid Reason", cancelledAppointment.CancellationReason);
        _mockNotificationService.Verify(x => x.SendAppointmentCancellationAsync(It.IsAny<Appointment>()), Times.Once);
    }

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
    public async Task GetByIdAsync_ShouldReturnNull_WhenAppointmentDoesNotExist()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _appointmentService.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAppointment_WhenValid()
    {
        // Arrange
        var originalAppointment = new Appointment
        {
            PatientId = 2,
            DoctorId = 2,
            AppointmentDateTime = DateTime.Now.AddDays(4),
            EndDateTime = DateTime.Now.AddDays(4).AddMinutes(30),
            Status = "Pending",
            Reason = "Initial consultation",
            Notes = "New patient",
            CancellationReason = ""
        };
        await _context.Appointments.AddAsync(originalAppointment);
        await _context.SaveChangesAsync();

        // Detach the entity to avoid tracking conflicts
        _context.Entry(originalAppointment).State = EntityState.Detached;

        var updatedAppointment = new Appointment
        {
            Id = originalAppointment.Id,
            PatientId = 2,
            DoctorId = 2,
            AppointmentDateTime = DateTime.Now.AddDays(5), // Changed date
            EndDateTime = DateTime.Now.AddDays(5).AddMinutes(30),
            Status = "Approved",
            Reason = "Initial consultation",
            Notes = "New patient - updated notes",
            CancellationReason = ""
        };

        // Act
        await _appointmentService.UpdateAsync(updatedAppointment);
        var result = await _appointmentService.GetByIdAsync(originalAppointment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedAppointment.AppointmentDateTime, result.AppointmentDateTime);
        Assert.Equal("Approved", result.Status);
        Assert.Equal("New patient - updated notes", result.Notes);
    }
}
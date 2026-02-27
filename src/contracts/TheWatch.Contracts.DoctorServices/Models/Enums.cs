namespace TheWatch.Contracts.DoctorServices.Models;

public enum AppointmentType { InPerson, Telehealth, HomeVisit, FollowUp }
public enum AppointmentStatus { Scheduled, Confirmed, InProgress, Completed, Cancelled, NoShow }

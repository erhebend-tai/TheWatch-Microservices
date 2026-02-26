namespace TheWatch.Contracts.FamilyHealth.Models;

public enum FamilyRole { Parent, Guardian, Child, Dependent, ElderlyRelative }
public enum CheckInStatus { Safe, NeedHelp, Emergency, NoResponse }
public enum VitalType { HeartRate, BloodPressure, Temperature, SpO2, RespiratoryRate, BloodGlucose }
public enum AlertSeverity { Info, Warning, Critical, Emergency }

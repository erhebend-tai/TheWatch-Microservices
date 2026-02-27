namespace TheWatch.Contracts.VoiceEmergency.Models;

public enum EmergencyType { Wildfire, Hurricane, Tornado, Flood, Earthquake, TerroristThreat, ChemicalHazard, MedicalEmergency, ActiveShooter, Other }
public enum IncidentStatus { Reported, Dispatched, InProgress, Resolved, Archived, Cancelled }
public enum DispatchStatus { Pending, Acknowledged, EnRoute, OnScene, Completed, Escalated, TimedOut }

namespace TheWatch.Contracts.FirstResponder.Models;

public enum ResponderType { Police, Fire, EMS, SAR, HazMat, VolunteerMedic, CommunityWatch, Other }
public enum ResponderStatus { Available, Busy, EnRoute, OnScene, OffDuty }
public enum CheckInType { Arrived, Update, NeedBackup, AllClear, Departing }

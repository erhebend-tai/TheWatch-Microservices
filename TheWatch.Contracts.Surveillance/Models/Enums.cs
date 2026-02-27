// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Contracts.Surveillance.Models;

public enum CameraStatus { Pending, Verified, Active, Inactive, Flagged, Rejected }
public enum FootageStatus { Submitted, Processing, Analyzed, Verified, Rejected, Archived }
public enum DetectionType { Person, Vehicle, Weapon, LicensePlate, Face, Package, Animal, Fire, Other }
public enum SubmissionSource { PublicCamera, PrivateCamera, Doorbell, Dashcam, Bodycam, Drone, Other }
public enum MediaType { Video, Audio, Image }

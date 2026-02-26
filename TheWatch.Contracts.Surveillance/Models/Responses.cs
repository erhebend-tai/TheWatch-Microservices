// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Contracts.Surveillance.Models;

public record CameraListResponse(List<CameraRegistrationDto> Items, int TotalCount, int Page, int PageSize);
public record FootageListResponse(List<FootageSubmissionDto> Items, int TotalCount, int Page, int PageSize);
public record CrimeLocationListResponse(List<CrimeLocationDto> Items, int TotalCount, int Page, int PageSize);
public record DetectionListResponse(List<DetectionResultDto> Items, int TotalCount, int Page, int PageSize);

namespace TheWatch.Contracts.DisasterRelief.Models;

public enum DisasterType { Wildfire, Hurricane, Tornado, Flood, Earthquake, ChemicalSpill, Pandemic, Other }
public enum EventStatus { Active, Monitoring, Resolved, Archived }
public enum ShelterStatus { Open, Full, Closed, Evacuating }
public enum ResourceCategory { Water, Food, Medical, Clothing, Equipment, Shelter, Transportation, Other }
public enum ResourceStatus { Available, Reserved, InTransit, Delivered, Expired }
public enum RequestPriority { Low, Medium, High, Critical }
public enum RequestStatus { Open, Matched, Fulfilled, Cancelled }

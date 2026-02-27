using Microsoft.EntityFrameworkCore;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Seeders;

public class DisasterReliefSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(DisasterReliefDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<DisasterEvent>().AnyAsync(ct))
            return;

        // Disaster Event
        var earthquake = new DisasterEvent
        {
            Id = Guid.Parse("00000000-0000-0000-0008-000000000001"),
            Type = DisasterType.Earthquake,
            Name = "SoCal M6.2 Earthquake",
            Description = "Magnitude 6.2 earthquake centered near Palmdale, CA. Multiple structures damaged.",
            Location = new GeoPoint(34.5794, -118.1165),
            RadiusKm = 50.0,
            Severity = 4,
            Status = EventStatus.Active
        };
        context.Set<DisasterEvent>().Add(earthquake);

        // Shelters
        var shelters = new[]
        {
            new Shelter { Id = Guid.Parse("00000000-0000-0000-0008-000000000010"), Name = "Lincoln High School Shelter", Location = new GeoPoint(34.0522, -118.2437), Capacity = 500, CurrentOccupancy = 127, Status = ShelterStatus.Open, ContactPhone = "+15552001001", Amenities = ["Water", "Cots", "Medical Station", "Pet Area"], DisasterEventId = earthquake.Id },
            new Shelter { Id = Guid.Parse("00000000-0000-0000-0008-000000000011"), Name = "Community Center Relief Hub", Location = new GeoPoint(34.0610, -118.2500), Capacity = 200, CurrentOccupancy = 198, Status = ShelterStatus.Full, ContactPhone = "+15552001002", Amenities = ["Water", "Cots", "Charging Station"], DisasterEventId = earthquake.Id }
        };
        context.Set<Shelter>().AddRange(shelters);

        // Resources
        var resources = new[]
        {
            new ResourceItem { Id = Guid.Parse("00000000-0000-0000-0008-000000000020"), Category = ResourceCategory.Water, Name = "Bottled Water Cases", Quantity = 500, Unit = "cases", Location = new GeoPoint(34.0522, -118.2437), Status = ResourceStatus.Available, DisasterEventId = earthquake.Id },
            new ResourceItem { Id = Guid.Parse("00000000-0000-0000-0008-000000000021"), Category = ResourceCategory.Medical, Name = "First Aid Kits", Quantity = 50, Unit = "kits", Location = new GeoPoint(34.0610, -118.2500), Status = ResourceStatus.Available, DisasterEventId = earthquake.Id },
            new ResourceItem { Id = Guid.Parse("00000000-0000-0000-0008-000000000022"), Category = ResourceCategory.Food, Name = "MRE Meal Packs", Quantity = 1000, Unit = "meals", Location = new GeoPoint(34.0522, -118.2437), Status = ResourceStatus.Available, DisasterEventId = earthquake.Id },
            new ResourceItem { Id = Guid.Parse("00000000-0000-0000-0008-000000000023"), Category = ResourceCategory.Equipment, Name = "Emergency Blankets", Quantity = 300, Unit = "blankets", Location = new GeoPoint(34.0610, -118.2500), Status = ResourceStatus.InTransit, DisasterEventId = earthquake.Id },
            new ResourceItem { Id = Guid.Parse("00000000-0000-0000-0008-000000000024"), Category = ResourceCategory.Clothing, Name = "Winter Jackets", Quantity = 150, Unit = "jackets", Location = new GeoPoint(34.0522, -118.2437), DonorId = Guid.Parse("00000000-0000-0000-0000-000000010001"), Status = ResourceStatus.Available, DisasterEventId = earthquake.Id }
        };
        context.Set<ResourceItem>().AddRange(resources);

        await context.SaveChangesAsync(ct);
    }
}

namespace OnlineStore.Api.Contracts;

public class SaveAddressRequest
{
    public string? Title { get; set; }
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? Floor { get; set; }
    public string? ApartmentNo { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }
}

namespace Arriba.Core.Models;

public record Site(
    string Id,
    string Name,
    string? Description,
    string? TimeZone,
    List<Device> Devices
);

public record Device(
    string Id,
    string Name,
    string MacAddress,
    string Model,
    string SerialNumber,
    DeviceStatus Status,
    List<Radio> Radios
);

public record Radio(
    string Id,
    string Band,
    int Channel,
    int ChannelWidth,
    int TransmitPower,
    bool Enabled,
    RadioStatus Status
);

public enum DeviceStatus
{
    Online,
    Offline,
    Updating,
    Unknown
}

public enum RadioStatus
{
    Active,
    Inactive,
    Disabled,
    Unknown
}

public record RadioControlRequest(
    string DeviceId,
    string RadioId,
    bool? Enabled,
    int? Channel,
    int? TransmitPower
);

public record RadioControlResponse(
    bool Success,
    string? Message,
    Radio? Radio
);

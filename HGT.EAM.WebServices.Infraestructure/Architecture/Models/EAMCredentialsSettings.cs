using System.Text.Json.Serialization;

namespace HGT.EAM.WebServices.Infraestructure.Architecture.Models;

public sealed class EAMCredentialsSettings
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Organization { get; set; } = string.Empty;
}

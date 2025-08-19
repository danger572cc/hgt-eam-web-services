namespace HGT.EAM.WebServices.Infraestructure.Architecture.Exceptions;

[Serializable]
public class OCTNotFoundException : Exception
{
    public string? ResourceType { get; }
    public object? MissingResourceId { get; }

    public OCTNotFoundException()
        : base("Recurso de tipo desconocido con ID desconocido no fue encontrado.")
    {
        ResourceType = "Unknown";
        MissingResourceId = "Unknown";
    }

    public OCTNotFoundException(string resourceType, object missingResourceId)
        : base($"Recurso de tipo {resourceType} con ID {missingResourceId} no fue encontrado.")
    {
        ResourceType = resourceType;
        MissingResourceId = missingResourceId;
    }

    public OCTNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    { }

    public OCTNotFoundException(string resourceType)
        : base($"No se encontraron recursos de tipo {resourceType}.")
    {
        ResourceType = resourceType;
    }
}

namespace HGT.EAM.WebServices.Infraestructure.Architecture.Exceptions;

[Serializable]
public class OCTBadRequestException : Exception
{
    public List<ValidationError> ValidationErrors { get; private set; }

    public OCTBadRequestException()
        : base()
    {
        ValidationErrors = new List<ValidationError>();
    }

    public OCTBadRequestException(string message)
        : base(message)
    {
        ValidationErrors = new List<ValidationError>();
    }

    public OCTBadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
        ValidationErrors = new List<ValidationError>();
    }

    public OCTBadRequestException(string message, List<ValidationError> validationErrors)
        : base(message)
    {
        ValidationErrors = validationErrors ?? new List<ValidationError>();
    }
}

public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public List<string> ErrorMessages { get; set; } = new List<string>();
}

using MediatR;

namespace HGT.EAM.WebServices.Infraestructure.Architecture.Query;

public interface IQuery : IRequest;
public interface IQuery<out TResponse> : IRequest<TResponse>;

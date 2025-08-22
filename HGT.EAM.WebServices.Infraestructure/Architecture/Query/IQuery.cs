using Mediator;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Query;

public interface IQuery : IRequest;
public interface IQuery<out TResponse> : IRequest<TResponse>;

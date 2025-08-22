using Mediator;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Query;

public interface IQueryHandler<TQuery> : IRequestHandler<TQuery> where TQuery : IQuery;
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>;

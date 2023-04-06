using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OttoTheGeek.Internal.Authorization
{
    public class AuthorizationBuilder<TModel, TProp>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parent;
        private Expression<Func<TModel, TProp>> _expr;

        public AuthorizationBuilder(GraphTypeBuilder<TModel> parent, Expression<Func<TModel, TProp>> expr)
        {
            this._parent = parent;
            this._expr = expr;
        }

        public GraphTypeBuilder<TModel> Via<TAuth>(Func<TAuth, bool> authorizeCallback)
            where TAuth : class
        {
            return _parent.Clone(_parent.TypeConfig.ConfigureField(_expr, x => x with { AuthResolver = new AuthResolverStub<TAuth>(authorizeCallback) })
                );
        }

        public GraphTypeBuilder<TModel> Via<TAuth>(Func<TAuth, Task<bool>> authorizeAsyncCallback)
            where TAuth : class
        {
            return _parent.Clone(_parent.TypeConfig.ConfigureField(_expr, x => x with { AuthResolver = new AuthResolverStub<TAuth>(authorizeAsyncCallback) })
            );
        }
    }
}

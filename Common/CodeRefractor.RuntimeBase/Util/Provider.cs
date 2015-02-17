using System;

namespace CodeRefractor.Util
{
    /**
     * This is a wrapper over the Func<T> in order to be able
     * to get our transparent scopes, without having Func<T> get
     * calls everywhere.
     */
    public class Provider<T>
    {
        public T Value
        {
            get
            {
                return _getter();
            }
        }

        private readonly Func<T> _getter;

        public Provider(Func<T> getter)
        {
            _getter = getter;
        }
    }
}

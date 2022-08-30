using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace xTradeService.Core
{
    public class PublicGrouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly IGrouping<TKey, TElement> _internalGrouping;

        public PublicGrouping(IGrouping<TKey, TElement> internalGrouping)
        {
            _internalGrouping = internalGrouping;
        }

        public override bool Equals(object obj)
        {
            var that = obj as PublicGrouping<TKey, TElement>;

            return (that != null) && (Key.Equals(that.Key));
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        #region IGrouping<TKey,TElement> Members

        public TKey Key
        {
            get { return _internalGrouping.Key; }
        }

        #endregion

        #region IEnumerable<TElement> Members

        public IEnumerator<TElement> GetEnumerator()
        {
            return _internalGrouping.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalGrouping.GetEnumerator();
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;

namespace ET
{
    public sealed class DelauTriangle : IDisposable
    {
        private List<DelauSite> _sites;

        public List<DelauSite> sites
        {
            get { return this._sites; }
        }

        public DelauTriangle(DelauSite a, DelauSite b, DelauSite c)
        {
            _sites = new List<DelauSite>() { a, b, c };
        }

        public void Dispose()
        {
            _sites.Clear();
            _sites = null;
        }
    }
}
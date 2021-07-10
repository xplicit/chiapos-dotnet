using System;
using System.Collections.Generic;

namespace Chiapos.Dotnet
{
    public class NormalizedComparer : IComparer<int>
    {
        private List<double> dpdf;
        private List<short> ans;
        
        public NormalizedComparer(List<double> dbpf, List<short> ans)
        {
            this.dpdf = dbpf;
            this.ans = ans;
        }
        
        public int Compare(int i, int j)
        {
            return Math.Sign((dpdf[i] * (Math.Log2(ans[i] + 1) - Math.Log2(ans[i]))) -
                             (dpdf[j] * (Math.Log2(ans[j] + 1) - Math.Log2(ans[j]))));
        }
    }
}
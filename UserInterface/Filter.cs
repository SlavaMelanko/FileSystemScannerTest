using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ashampoo
{
    public interface IFilter
    {
        bool Matches(FileInfo file);
    }

    public class SizeFilter : IFilter
    {
        private readonly long minSize;

        public SizeFilter(long minSize)
        {
            this.minSize = minSize;
        }

        public bool Matches(FileInfo file)
        {
            return file.Length > minSize;
        }
    }

    public class CompositeFilter : IFilter
    {
        private List<IFilter> filters = new();

        public void AddFilter(IFilter filter)
        {
            filters.Add(filter);
        }

        public bool Matches(FileInfo file)
        {
            foreach (var filter in filters)
            {
                if (!filter.Matches(file))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

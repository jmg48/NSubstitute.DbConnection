namespace NSubstitute.DbConnection.Extensions
{
    using System.Collections.Generic;
    using System.Data;

    internal class DataTableComparer : EqualityComparer<DataTable>
    {
        public override bool Equals(DataTable x, DataTable y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.Rows.Count != y.Rows.Count || x.Columns.Count != y.Columns.Count)
            {
                return false;
            }

            for (var row = 0; row < x.Rows.Count; row++)
            {
                for (var column = 0; column < x.Columns.Count; column++)
                {
                    if (!Equals(x.Rows[row][column], y.Rows[row][column]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override int GetHashCode(DataTable obj) => obj.GetHashCode();
    }
}

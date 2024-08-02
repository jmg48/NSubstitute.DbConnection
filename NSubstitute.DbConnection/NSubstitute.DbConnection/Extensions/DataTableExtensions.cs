namespace NSubstitute.DbConnection.Extensions
{
    using System.Data;

    internal static class DataTableExtensions
    {
        public static bool Equivalent(this DataTable table, DataTable other)
        {
            if (table.Rows.Count != other.Rows.Count || table.Columns.Count != other.Columns.Count)
            {
                return false;
            }

            for (var row = 0; row < table.Rows.Count; row++)
            {
                for (var column = 0; column < table.Columns.Count; column++)
                {
                    if (!Equals(table.Rows[row][column], other.Rows[row][column]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

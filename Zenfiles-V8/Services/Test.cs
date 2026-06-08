using System.Data;

namespace Zenfiles_V8.Services
{
    public class Test
    {
        // Helper: calculate days difference between previous and next DateTime
        public string? GetDaysDifference(DataRow row, int prevIndex, int[] nextOffsets)
        {
            DateTime prevDate = Convert.ToDateTime(row[prevIndex]);

            foreach (int offset in nextOffsets)
            {
                int targetIndex = prevIndex + offset;

                if (targetIndex >= 0 && targetIndex < row.Table.Columns.Count)
                {
                    if (row[targetIndex] != DBNull.Value)
                    {
                        DateTime nextDate = Convert.ToDateTime(row[targetIndex]);
                        return (nextDate - prevDate).Days.ToString()+" Days "+ (nextDate - prevDate).Hours.ToString()+" Hours "+ (nextDate - prevDate).Minutes.ToString()+" Minutes";
                    }
                }
            }
            return (DateTime.Now - prevDate).Days.ToString()+" Days "+ (DateTime.Now - prevDate).Hours.ToString()+" Hours "+ (DateTime.Now - prevDate).Days.ToString()+" Minutes" ;
        }
    }
}

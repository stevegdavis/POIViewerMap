using POIViewerMap.DataClasses;

namespace POIViewerMap.Helpers;

/// <summary>
/// Class <c>FilenameComparer</c>
/// </summary>
internal class FilenameComparer
{
    [Flags]
    public enum SortOrder
    {
        asc = 0,
        desc = 1,
    }
    private static SortOrder m_filenameSortOrder = SortOrder.asc;
    /// <summary>
    /// Sort order
    /// </summary>
    public static SortOrder filenameSortOrder
    {
        get { return m_filenameSortOrder; }
        set { m_filenameSortOrder = value; }
    }
    /// <summary>
    /// <c>NameArray</c>
    /// </summary>
    /// <param name="ffA"></param>
    /// <param name="ffB"></param>
    /// <returns>0 if equal, 1 if greater than, -1 if lesser than</returns>
    public static int NameArray(string ffA, string ffB)
    {
        int RC = 0;
        try
        {
            if (ffA.Equals(ffB))
                return 0;
            if (ffA.CompareTo(ffB) > 0)
                return 1;
            else
                return -1;
        }
        catch
        {
            RC = 0;
        }
        return RC;
    }    
}

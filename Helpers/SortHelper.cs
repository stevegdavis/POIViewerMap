﻿using POIViewerMap.DataClasses;

namespace POIViewerMap.Helpers;

internal class FilenameComparer
{
    [Flags]
    public enum SortOrder
    {
        asc = 0,
        desc = 1,
    }
    private static SortOrder m_filenameSortOrder = SortOrder.asc;

    public static SortOrder filenameSortOrder
    {
        get { return m_filenameSortOrder; }
        set { m_filenameSortOrder = value; }
    }
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
    public static int Name(string strA, string strB)
    {
        int RC = 0;
        try
        {
            if (strA.Equals(strB))
                return 0;
            if (strA.CompareTo(strB) > 0)
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
